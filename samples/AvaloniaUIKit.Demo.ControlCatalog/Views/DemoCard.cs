using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUIKit.Demo.ControlCatalog.Code;
using AvaloniaUIKit.Demo.ControlCatalog.Inspector;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Views;

/// <summary>
/// A demo on the component's page: its title and description, the live demo, and its
/// code, which shows the property grid's changes as they are made.
/// </summary>
public sealed class DemoCard : Border
{
    private readonly Decorator _preview;
    private readonly Canvas _overlay;
    private readonly ElementHighlight _selection = new(filled: false);
    private readonly ElementHighlight _hover = new(filled: true);
    private readonly Border _code;
    private readonly CodeView _codeView;
    private readonly ToggleButton _codeToggle;
    private readonly Button _reset;
    private readonly AvaloniaUIKit.Clipboard _copy;
    private readonly TabStrip? _codeTabs;
    private bool _wasEdited;

    public DemoCard(DemoSession session)
    {
        Session = session;
        Classes.Add("card");

        var title = new TextBlock { Classes = { "card-title" }, Text = session.Demo.Title ?? "Demo", VerticalAlignment = VerticalAlignment.Center };

        _codeToggle = new ToggleButton
        {
            Classes = { "small" },
            Content = Labeled("SquareTerminal", "Code"),
        };
        ToolTip.SetTip(_codeToggle, "Show the XAML");
        _codeToggle.IsCheckedChanged += (_, _) => _code!.IsVisible = _codeToggle.IsChecked == true;

        _reset = new Button
        {
            Classes = { "ghost", "small" },
            Content = Labeled("Undo2", "Reset"),
            IsVisible = false,
        };
        ToolTip.SetTip(_reset, "Undo the property grid's changes");
        _reset.Click += (_, _) => Session.Reset();

        _copy = new AvaloniaUIKit.Clipboard { Classes = { "small" }, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(_copy, "Copy the XAML");

        var tools = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Children = { _reset, _codeToggle, _copy },
        };
        DockPanel.SetDock(tools, Avalonia.Controls.Dock.Right);
        // The description has a row of its own: its width does not follow the buttons that come and go.
        var heading = new StackPanel { Spacing = 2, Children = { new DockPanel { Children = { tools, title } } } };
        if (session.Demo.Description is { } description)
        {
            heading.Children.Add(new SelectableTextBlock { Classes = { "card-description" }, Margin = new Thickness(0, 0, 6, 0) }.With(description));
        }
        var header = new Border { Classes = { "card-header" }, Child = heading };

        _preview = new Decorator { HorizontalAlignment = HorizontalAlignment.Center };
        // The frames over the demo's controls: on the card, so that the page's scrolling clips them.
        _overlay = new Canvas { IsHitTestVisible = false, Children = { _selection, _hover } };
        var preview = new Border { Classes = { "card-preview" }, Child = new Panel { Children = { _preview, _overlay } } };
        LayoutUpdated += (_, _) =>
        {
            _selection.Follow(_overlay);
            _hover.Follow(_overlay);
        };

        _codeView = new CodeView();
        var code = new DockPanel();
        if (session.CodeBehind is not null)
        {
            _codeTabs = new TabStrip
            {
                Classes = { "segmented", "xsmall" },
                HorizontalAlignment = HorizontalAlignment.Left,
                ItemsSource = new[] { "XAML", $"{session.Demo.Source.Split('/')[^1]}.axaml.cs" },
                SelectedIndex = 0,
            };
            _codeTabs.SelectionChanged += (_, _) => ShowCode();
            var tabs = new Border { Classes = { "code-tabs" }, Child = _codeTabs };
            tabs.BindBrush(BackgroundProperty, "UIKit.Background");
            DockPanel.SetDock(tabs, Avalonia.Controls.Dock.Top);
            code.Children.Add(tabs);
        }
        code.Children.Add(_codeView);
        _code = new Border { Classes = { "card-code" }, Child = code, IsVisible = false };

        Child = new StackPanel { Children = { header, preview, _code } };

        // Any press or focus in the card makes it the one the property grid works on.
        AddHandler(PointerPressedEvent, (_, _) => Activated?.Invoke(this, EventArgs.Empty), RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(GotFocusEvent, (_, _) => Activated?.Invoke(this, EventArgs.Empty), RoutingStrategies.Bubble, handledEventsToo: true);

        session.Changed += (_, _) => OnChanged();
        session.Recreated += (_, _) =>
        {
            _preview.Child = Session.Root;
            OnChanged();
        };
        _preview.Child = session.Root;
        ShowCode();
    }

    /// <summary>The demo and its XAML.</summary>
    public DemoSession Session { get; }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Border);

    /// <summary>Whether the property grid works on this card.</summary>
    public bool IsActive
    {
        get => Classes.Contains("active");
        set => Classes.Set("active", value);
    }

    /// <summary>Whether the code shows.</summary>
    public bool IsCodeVisible
    {
        get => _codeToggle.IsChecked == true;
        set => _codeToggle.IsChecked = value;
    }

    /// <summary>The control of the demo the property grid edits, framed; null for none.</summary>
    public Control? Selected
    {
        get => _selection.Target;
        set
        {
            _selection.Target = value;
            _selection.Follow(_overlay);
        }
    }

    /// <summary>The control of the demo the pointer would pick, filled; null for none.</summary>
    public Control? Hovered
    {
        get => _hover.Target;
        set
        {
            _hover.Target = value;
            _hover.Follow(_overlay);
        }
    }

    /// <summary>Raised when the card is pressed or takes the focus.</summary>
    public event EventHandler? Activated;

    /// <summary>Whether <paramref name="visual"/> is part of the live demo.</summary>
    public bool Contains(Visual? visual) => visual is not null && (visual == _preview || _preview.IsVisualAncestorOf(visual));

    private void OnChanged()
    {
        var edited = !Session.Edits.IsEmpty;
        _reset.IsVisible = edited;
        // The first change opens the code, so that the change shows.
        if (edited && !_wasEdited)
        {
            IsCodeVisible = true;
        }
        _wasEdited = edited;
        ShowCode();
    }

    private void ShowCode()
    {
        if (_codeTabs?.SelectedIndex == 1 && Session.CodeBehind is { } codeBehind)
        {
            var text = codeBehind.TrimEnd();
            _codeView.Show(text, Highlighter.CSharp(text));
            _copy.Text = text;
            return;
        }
        var inner = XamlEdits.Inner(XamlDocument.Parse(Session.Xaml));
        var changed = Session.Edits.SetAttributes.ToHashSet();
        var texts = Session.Edits.SetTexts.ToHashSet();
        // The code is the root element's content: its elements are numbered from 1 in the whole XAML.
        _codeView.Show(inner, Highlighter.Xaml(XamlDocument.Parse(inner), changed, texts, indexOffset: 1));
        _copy.Text = inner;
    }

    private static StackPanel Labeled(string icon, string text) => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 6,
        Children =
        {
            new PathIcon { Data = InlineText.Icon(icon), Width = 14, Height = 14 },
            new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center },
        },
    };
}
