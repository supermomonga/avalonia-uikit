using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's TitleBar (title_bar.rs): a 34px bar the app puts at the top of a
/// window that extends its client area into the decorations
/// (<c>ExtendClientAreaToDecorationsHint</c>), with GPUI's gradient and bottom
/// border. Its children are spread along it, the first at the start and the
/// last at the end (GPUI's justify_between), and centered vertically.
/// </summary>
/// <remarks>
/// <para>
/// Pressing the bar's empty areas or its text and moving drags the window; a
/// double click maximizes or restores it. On Windows and macOS the bar is the
/// window's title bar area (<c>WindowDecorationProperties.ElementRole</c>
/// TitleBar), so the system does both; focusable controls in it (buttons,
/// menus, inputs) take the role User and get their input. On other systems
/// the bar drags the window itself, as GPUI does on Linux.
/// </para>
/// <para>
/// On macOS the bar leaves 80px at the start for the window's traffic lights
/// while the window extends into its decorations. On other systems it ends
/// with GPUI's minimize, maximize (restore while maximized) and close buttons
/// when <see cref="ShowsCaptionButtons"/> says so: by default while the window
/// extends into its decorations, not in a browser. Only the buttons the window
/// allows show (<c>CanMinimize</c>, <c>CanMaximize</c>).
/// </para>
/// </remarks>
[TemplatePart(PartRoot, typeof(Control))]
[TemplatePart(PartBar, typeof(Control))]
[TemplatePart(PartMinimizeButton, typeof(Button))]
[TemplatePart(PartMaximizeButton, typeof(Button))]
[TemplatePart(PartCloseButton, typeof(Button))]
[PseudoClasses(":macos", ":caption-buttons", ":maximized", ":fullscreen", ":has-minimize", ":has-maximize")]
public class TitleBar : ItemsControl
{
    /// <summary>
    /// Whether the bar ends with the window's caption buttons. Null (the
    /// default) shows them on Windows and Linux while the window extends its
    /// client area into the decorations (title_bar.rs WindowControls).
    /// </summary>
    public static readonly StyledProperty<bool?> ShowsCaptionButtonsProperty =
        AvaloniaProperty.Register<TitleBar, bool?>(nameof(ShowsCaptionButtons));

    private const string PartRoot = "PART_Root";
    private const string PartBar = "PART_TitleBar";
    private const string PartMinimizeButton = "PART_MinimizeButton";
    private const string PartMaximizeButton = "PART_MaximizeButton";
    private const string PartCloseButton = "PART_CloseButton";

    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new TitleBarPanel());

    private Control? _bar;
    private Button? _minimize;
    private Button? _maximize;
    private Button? _close;
    private Window? _window;
    private PointerPressedEventArgs? _moveFrom;

    static TitleBar()
    {
        ItemsPanelProperty.OverrideDefaultValue<TitleBar>(DefaultPanel);
        ShowsCaptionButtonsProperty.Changed.AddClassHandler<TitleBar>((bar, _) => bar.Update());
    }

    /// <inheritdoc cref="ShowsCaptionButtonsProperty"/>
    public bool? ShowsCaptionButtons
    {
        get => GetValue(ShowsCaptionButtonsProperty);
        set => SetValue(ShowsCaptionButtonsProperty, value);
    }

    /// <summary>
    /// The role of the bar's empty areas: the system's title bar on Windows and
    /// macOS, which drags and maximizes the window itself; none elsewhere.
    /// </summary>
    internal static WindowDecorationsElementRole BarRole { get; } =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? WindowDecorationsElementRole.TitleBar : WindowDecorationsElementRole.None;

    /// <summary>Whether the window's traffic lights are at the bar's start (macOS).</summary>
    internal static bool HasTrafficLights { get; } = OperatingSystem.IsMacOS();

    /// <summary>Whether the platform has no window controls of the app's (macOS draws its own; a browser has none).</summary>
    internal static bool HasNoCaptionButtons { get; } = OperatingSystem.IsMacOS() || OperatingSystem.IsBrowser();

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Subscribe(_minimize, OnMinimize, false);
        Subscribe(_maximize, OnMaximize, false);
        Subscribe(_close, OnClose, false);
        _bar = e.NameScope.Find<Control>(PartBar);
        _minimize = e.NameScope.Find<Button>(PartMinimizeButton);
        _maximize = e.NameScope.Find<Button>(PartMaximizeButton);
        _close = e.NameScope.Find<Button>(PartCloseButton);
        // The drawn bar (macOS hit-tests the element it hits) and everything in it
        // (Windows walks up to the nearest role) are the title bar area.
        foreach (var part in new[] { e.NameScope.Find<Control>(PartRoot), _bar })
        {
            if (part is not null)
            {
                WindowDecorationProperties.SetElementRole(part, BarRole);
            }
        }
        Subscribe(_minimize, OnMinimize, true);
        Subscribe(_maximize, OnMaximize, true);
        Subscribe(_close, OnClose, true);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null)
        {
            _window.PropertyChanged += OnWindowPropertyChanged;
        }
        Update();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_window is not null)
        {
            _window.PropertyChanged -= OnWindowPropertyChanged;
        }
        _window = null;
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _moveFrom = null;
        // Presses its controls took are theirs.
        if (e.Handled || _window is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }
        // Avalonia's macOS backend drags and zooms a window from what it hits with the TitleBar role.
        if (BarRole == WindowDecorationsElementRole.TitleBar && HasTrafficLights &&
            _window.IsExtendedIntoWindowDecorations && ReferenceEquals(e.Source, _bar))
        {
            return;
        }
        if (e.ClickCount == 2)
        {
            // title_bar.rs on_double_click: zoom_window (titlebar_double_click on macOS).
            ToggleMaximized();
            e.Handled = true;
            return;
        }
        // title_bar.rs: a press arms the move, and the first move with the button down starts it.
        _moveFrom = e;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_moveFrom is { } from && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _moveFrom = null;
            _window?.BeginMoveDrag(from);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _moveFrom = null;
    }

    private static void Subscribe(Button? button, EventHandler<RoutedEventArgs> handler, bool subscribe)
    {
        if (button is null)
        {
            return;
        }
        if (subscribe)
        {
            button.Click += handler;
        }
        else
        {
            button.Click -= handler;
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.WindowStateProperty || e.Property == Window.CanMinimizeProperty ||
            e.Property == Window.CanMaximizeProperty || e.Property == Window.CanResizeProperty ||
            e.Property == Window.IsExtendedIntoWindowDecorationsProperty)
        {
            Update();
        }
    }

    private void Update()
    {
        var window = _window;
        var extended = window?.IsExtendedIntoWindowDecorations == true;
        var state = window?.WindowState ?? WindowState.Normal;
        PseudoClasses.Set(":macos", HasTrafficLights && extended);
        PseudoClasses.Set(":caption-buttons", ShowsCaptionButtons ?? (!HasNoCaptionButtons && extended));
        PseudoClasses.Set(":maximized", state == WindowState.Maximized);
        PseudoClasses.Set(":fullscreen", state == WindowState.FullScreen);
        PseudoClasses.Set(":has-minimize", window?.CanMinimize ?? true);
        PseudoClasses.Set(":has-maximize", CanToggleMaximized(window));
    }

    // WindowDrawnDecorations: a maximized window restores if it can resize; a normal one maximizes if it can.
    private static bool CanToggleMaximized(Window? window) => window is null ||
        (window.WindowState is WindowState.Maximized or WindowState.FullScreen ? window.CanResize : window.CanMaximize);

    private void ToggleMaximized()
    {
        if (_window is { } window && CanToggleMaximized(window))
        {
            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }
    }

    private void OnMinimize(object? sender, RoutedEventArgs e)
    {
        if (_window is not null)
        {
            _window.WindowState = WindowState.Minimized;
        }
        e.Handled = true;
    }

    private void OnMaximize(object? sender, RoutedEventArgs e)
    {
        ToggleMaximized();
        e.Handled = true;
    }

    private void OnClose(object? sender, RoutedEventArgs e)
    {
        _window?.Close();
        e.Handled = true;
    }
}

/// <summary>
/// The row of a <see cref="TitleBar"/>'s children (title_bar.rs: an h_flex,
/// justify_between, items_center): each at its own width, the space left
/// spread evenly between them, centered vertically.
/// </summary>
internal sealed class TitleBarPanel : Panel
{
    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0d;
        var height = 0d;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }
        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        var free = finalSize.Width - shown.Sum(c => c.DesiredSize.Width);
        var gap = shown.Count > 1 ? Math.Max(0, free) / (shown.Count - 1) : 0;
        var x = 0d;
        foreach (var child in shown)
        {
            var size = child.DesiredSize;
            child.Arrange(new Rect(x, (finalSize.Height - size.Height) / 2, size.Width, size.Height));
            x += size.Width + gap;
        }
        return finalSize;
    }
}
