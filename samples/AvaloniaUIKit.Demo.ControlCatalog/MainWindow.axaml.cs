using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUIKit.Demo.ControlCatalog.Inspector;
using AvaloniaUIKit.Demo.ControlCatalog.Views;

namespace AvaloniaUIKit.Demo.ControlCatalog;

/// <summary>
/// The catalog's window: the components in the sidebar, the chosen component's page in
/// the middle, the property grid on the right, and the theme in the title bar.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly CatalogSettings _settings;
    private readonly Dictionary<string, SidebarMenuItem> _items = new(StringComparer.Ordinal);
    private readonly List<(SidebarGroup Group, List<(CatalogComponent Component, SidebarMenuItem Item)> Items)> _groups = [];
    private ComponentPage? _page;
    private bool _choosingTheme;

    public MainWindow()
        : this(new CatalogSettings())
    {
    }

    public MainWindow(CatalogSettings settings)
    {
        _settings = settings;
        InitializeComponent();
        Sidebar.IsCollapsed = settings.IsSidebarCollapsed;
        Sidebar.PropertyChanged += (_, e) =>
        {
            if (e.Property == AvaloniaUIKit.Sidebar.IsCollapsedProperty)
            {
                _settings.IsSidebarCollapsed = Sidebar.IsCollapsed;
            }
        };
        BuildSidebar();
        BuildThemes();

        InspectorToggle.IsChecked = settings.IsInspectorOpen;
        InspectorToggle.IsCheckedChanged += (_, _) => LayoutInspector();
        LayoutInspector();
        InspectorSplitter.DragCompleted += (_, _) => _settings.InspectorWidth = Body.ColumnDefinitions[2].ActualWidth;
        Inspector.PickingChanged += (_, _) =>
        {
            if (!Inspector.IsPicking)
            {
                Hover(null, null);
            }
        };
        Inspector.TargetChanged += (_, _) =>
        {
            foreach (var card in _page?.Cards ?? [])
            {
                card.Selected = card.Session == Inspector.Session ? Inspector.Target : null;
            }
        };

        // Picking: a press in a demo selects the element under it instead of reaching it.
        PageHost.AddHandler(PointerMovedEvent, OnPagePointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        PageHost.AddHandler(PointerPressedEvent, OnPagePointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        PageHost.PointerExited += (_, _) => Hover(null, null);

        var command = Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;
        SearchKey.Gesture = new KeyGesture(Key.K, command);
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.K, command),
            Command = new RelayCommand(() =>
            {
                Sidebar.IsCollapsed = false;
                Search.Focus();
                Search.SelectAll();
            }),
        });
        Search.TextChanged += (_, _) => Filter(Search.Text ?? "");
        Search.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter && _groups.SelectMany(g => g.Items).FirstOrDefault(i => i.Item.IsVisible) is { Component: var first })
            {
                Navigate(first);
                e.Handled = true;
            }
        };

        Navigate(Catalog.Find(settings.Component ?? "") ?? Catalog.Find("button") ?? Catalog.Components[0]);
        Closing += (_, _) => _settings.Save();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && Inspector.IsPicking)
        {
            Inspector.IsPicking = false;
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    /// <summary>Shows <paramref name="component"/>'s page.</summary>
    public void Navigate(CatalogComponent component)
    {
        if (_page is not null)
        {
            _page.ActiveCardChanged -= OnActiveCardChanged;
        }
        Inspector.Clear();
        _page = new ComponentPage(component);
        _page.ActiveCardChanged += OnActiveCardChanged;
        PageHost.Content = _page;
        PageScroller.Offset = default;
        foreach (var (slug, item) in _items)
        {
            item.IsActive = slug == component.Slug;
        }
        _settings.Component = component.Slug;
        Title = $"{component.Title} — Avalonia UIKit Control Catalog";
        // The first card is the one the grid starts with, once its demo is in the tree.
        Dispatcher.UIThread.Post(() =>
        {
            if (_page?.Component == component && _page.Cards.Count > 0)
            {
                _page.ActiveCard = _page.Cards[0];
            }
        }, DispatcherPriority.Loaded);
    }

    private void OnActiveCardChanged(object? sender, EventArgs e)
    {
        if (_page?.ActiveCard is { } card && Inspector.Session != card.Session)
        {
            Inspector.Show(card.Session, card.Session.DefaultElement());
        }
    }

    private void BuildSidebar()
    {
        foreach (var section in (CatalogSection[])[CatalogSection.Avalonia, CatalogSection.UIKit, CatalogSection.ThirdParty])
        {
            var menu = new SidebarMenu();
            var items = new List<(CatalogComponent, SidebarMenuItem)>();
            foreach (var component in Catalog.In(section))
            {
                var item = new SidebarMenuItem { Label = component.Title };
                item.Click += (_, _) => Navigate(component);
                ToolTip.SetTip(item, string.Join(", ", component.Controls));
                ToolTip.SetPlacement(item, PlacementMode.Right);
                menu.Items.Add(item);
                items.Add((component, item));
                _items[component.Slug] = item;
            }
            var group = new SidebarGroup { Label = Catalog.Heading(section), Items = { menu } };
            Sidebar.Items.Add(group);
            _groups.Add((group, items));
        }
        Counts.Text = $"{Catalog.Components.Count} components · {Catalog.Components.Sum(c => c.Demos.Count)} demos";
    }

    private void Filter(string query)
    {
        foreach (var (group, items) in _groups)
        {
            var any = false;
            foreach (var (component, item) in items)
            {
                item.IsVisible = component.Matches(query);
                any |= item.IsVisible;
            }
            group.IsVisible = any;
        }
    }

    private sealed record ThemeOption(string Name, ThemeVariant? Variant);

    private void BuildThemes()
    {
        var system = new ThemeOption("System", null);
        var light = new ThemeOption("Default Light", ThemeVariant.Light);
        var dark = new ThemeOption("Default Dark", ThemeVariant.Dark);
        var bundled = UIKitThemeVariants.All.Select(v => new ThemeOption((string)v.Key, v)).ToList();
        var defaults = new SelectGroup { Title = "Default" };
        defaults.Items.AddRange([system, light, dark]);
        var lights = new SelectGroup { Title = "Light" };
        lights.Items.AddRange(bundled.Where(o => !UIKitThemeVariants.IsDark(o.Variant)));
        var darks = new SelectGroup { Title = "Dark" };
        darks.Items.AddRange(bundled.Where(o => UIKitThemeVariants.IsDark(o.Variant)));
        ThemeSelect.Items.Add(defaults);
        ThemeSelect.Items.Add(lights);
        ThemeSelect.Items.Add(darks);
        ThemeSelect.TextSelector = item => (item as ThemeOption)?.Name ?? "";

        var options = new List<ThemeOption> { system, light, dark };
        options.AddRange(bundled);
        var chosen = options.FirstOrDefault(o => string.Equals(o.Name, _settings.Theme, StringComparison.OrdinalIgnoreCase)) ?? system;
        Apply(chosen);
        _choosingTheme = true;
        ThemeSelect.SelectedItem = chosen;
        _choosingTheme = false;
        ThemeSelect.SelectionChanged += (_, _) =>
        {
            if (!_choosingTheme && ThemeSelect.SelectedItem is ThemeOption option)
            {
                Apply(option);
            }
        };

        void Apply(ThemeOption option)
        {
            if (Application.Current is { } app)
            {
                app.RequestedThemeVariant = option.Variant ?? ThemeVariant.Default;
            }
            _settings.Theme = option.Variant is null ? null : option.Name;
        }
    }

    private void LayoutInspector()
    {
        var open = InspectorToggle.IsChecked == true;
        _settings.IsInspectorOpen = open;
        Inspector.IsVisible = open;
        InspectorSplitter.IsVisible = open;
        Body.ColumnDefinitions[2].Width = open ? new GridLength(Math.Clamp(_settings.InspectorWidth, 260, 640)) : new GridLength(0);
        if (!open)
        {
            Inspector.IsPicking = false;
        }
    }

    private (DemoCard Card, Inspector.DemoSession Session, Xaml.XamlElement Element)? PickAt(object? source)
    {
        if (_page?.CardOf(source as Visual) is not { } card)
        {
            return null;
        }
        var session = card.Session;
        var element = session.Map.ElementAt(source as Visual);
        if (element is null || element == session.Document.Root)
        {
            // The demo may have added or moved controls since they were mapped.
            session.Remap();
            element = session.Map.ElementAt(source as Visual);
        }
        return element is null || element == session.Document.Root ? null : (card, session, element);
    }

    private void OnPagePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!Inspector.IsPicking && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            Hover(null, null);
            return;
        }
        if (PickAt(e.Source) is { } pick)
        {
            Hover(pick.Card, pick.Session.Map.ControlOf(pick.Element));
        }
        else
        {
            Hover(null, null);
        }
    }

    // Fills the control the pointer would pick, on its card only.
    private void Hover(DemoCard? card, Control? control)
    {
        foreach (var other in _page?.Cards ?? [])
        {
            other.Hovered = other == card ? control : null;
        }
    }

    private void OnPagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!Inspector.IsPicking && !e.KeyModifiers.HasFlag(KeyModifiers.Alt))
        {
            return;
        }
        if (PickAt(e.Source) is not { } pick)
        {
            return;
        }
        e.Handled = true;
        _page!.ActiveCard = pick.Card;
        Inspector.Show(pick.Session, pick.Element);
        Inspector.IsPicking = false;
    }

    /// <summary>A command that runs an action.</summary>
    private sealed class RelayCommand(Action action) : System.Windows.Input.ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => action();
    }
}
