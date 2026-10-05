using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.LogicalTree;

namespace AvaloniaUIKit;

/// <summary>A side of a <see cref="Sidebar"/>'s window (GPUI's Side).</summary>
public enum Side
{
    /// <summary>The left side.</summary>
    Left,

    /// <summary>The right side.</summary>
    Right,
}

/// <summary>How a <see cref="Sidebar"/> collapses (GPUI's SidebarCollapsible, from shadcn/ui).</summary>
public enum SidebarCollapsible
{
    /// <summary>To its icons, 48px wide.</summary>
    Icon,

    /// <summary>Out of the layout, sliding away from the content.</summary>
    Offcanvas,

    /// <summary>Never: the collapsed state is ignored.</summary>
    None,
}

/// <summary>
/// GPUI Kit's Sidebar (crates/component/src/sidebar/mod.rs): a surface on the
/// side of the window with a <see cref="Header"/>, scrolling items (usually
/// <see cref="SidebarGroup"/>s and <see cref="SidebarMenu"/>s) and a
/// <see cref="Footer"/>, in the sidebar colors with a 1px border on the
/// content side. It is <see cref="ExpandedWidth"/> wide (255px by default).
/// <para>
/// <see cref="IsCollapsed"/> collapses it the way <see cref="Collapsible"/>
/// says: to its icons (48px; group labels, item labels, suffixes and carets
/// hide, and an item with an icon shows its label as a tooltip on the right),
/// off the layout (sliding away from the content), or not at all. The width
/// moves over 200ms (ease-in-out-cubic) while the surface inside snaps to its
/// new width, as GPUI animates the clip and not the content. The collapsed
/// state reaches the parts through the inherited
/// <see cref="IsIconCollapsedProperty"/>, which an app can also set on a
/// SidebarMenu used without a Sidebar.
/// </para>
/// <para>
/// As the <see cref="SplitView.Pane"/> of a SplitView the sidebar follows it
/// instead: the SplitView draws the surface and moves the width, the sidebar
/// is collapsed while the pane is closed, to its icons in the compact display
/// modes and off the layout in the others, on the pane's side.
/// </para>
/// </summary>
[PseudoClasses(":left", ":right", ":offcanvas", ":icon-collapsed", ":offcanvas-collapsed", ":pane")]
public class Sidebar : ItemsControl
{
    /// <summary>The content above the items (GPUI's <c>header</c>), usually a <see cref="SidebarHeader"/>.</summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<Sidebar, object?>(nameof(Header));

    /// <summary>The content below the items (GPUI's <c>footer</c>), usually a <see cref="SidebarFooter"/>.</summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<Sidebar, object?>(nameof(Footer));

    /// <summary>The side of the window the sidebar is on (Left by default): its border is on the other side.</summary>
    public static readonly StyledProperty<Side> SideProperty =
        AvaloniaProperty.Register<Sidebar, Side>(nameof(Side));

    /// <summary>How the sidebar collapses (GPUI's <c>collapsible</c>, Icon by default).</summary>
    public static readonly StyledProperty<SidebarCollapsible> CollapsibleProperty =
        AvaloniaProperty.Register<Sidebar, SidebarCollapsible>(nameof(Collapsible));

    /// <summary>Whether the sidebar is collapsed (GPUI's <c>collapsed</c>); binds two-way.</summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        AvaloniaProperty.Register<Sidebar, bool>(nameof(IsCollapsed), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The width when not collapsed (GPUI's width, DEFAULT_WIDTH 255 by default).</summary>
    public static readonly StyledProperty<double> ExpandedWidthProperty =
        AvaloniaProperty.Register<Sidebar, double>(nameof(ExpandedWidth), 255);

    /// <summary>
    /// Whether the sidebar parts inside show only their icons (GPUI's
    /// <c>collapsed</c> on a SidebarGroup, SidebarMenu or SidebarMenuItem).
    /// Inherited; a Sidebar sets it on itself while it is collapsed to icons.
    /// </summary>
    public static readonly AttachedProperty<bool> IsIconCollapsedProperty =
        AvaloniaProperty.RegisterAttached<Sidebar, Control, bool>("IsIconCollapsed", inherits: true);

    private SplitView? _host;

    static Sidebar()
    {
        SideProperty.Changed.AddClassHandler<Sidebar>((sidebar, _) => sidebar.UpdateState());
        CollapsibleProperty.Changed.AddClassHandler<Sidebar>((sidebar, _) => sidebar.UpdateState());
        IsCollapsedProperty.Changed.AddClassHandler<Sidebar>((sidebar, _) => sidebar.UpdateState());
    }

    /// <summary>Creates a sidebar.</summary>
    public Sidebar() => UpdateState();

    /// <inheritdoc cref="HeaderProperty"/>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <inheritdoc cref="FooterProperty"/>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <inheritdoc cref="SideProperty"/>
    public Side Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    /// <inheritdoc cref="CollapsibleProperty"/>
    public SidebarCollapsible Collapsible
    {
        get => GetValue(CollapsibleProperty);
        set => SetValue(CollapsibleProperty, value);
    }

    /// <inheritdoc cref="IsCollapsedProperty"/>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <inheritdoc cref="ExpandedWidthProperty"/>
    public double ExpandedWidth
    {
        get => GetValue(ExpandedWidthProperty);
        set => SetValue(ExpandedWidthProperty, value);
    }

    /// <summary>Gets whether <paramref name="control"/>'s sidebar parts show only their icons.</summary>
    public static bool GetIsIconCollapsed(Control control) => control.GetValue(IsIconCollapsedProperty);

    /// <summary>Sets whether <paramref name="control"/>'s sidebar parts show only their icons.</summary>
    public static void SetIsIconCollapsed(Control control, bool value) => control.SetValue(IsIconCollapsedProperty, value);

    /// <inheritdoc />
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (Parent is SplitView split && ReferenceEquals(split.Pane, this))
        {
            _host = split;
            split.PropertyChanged += OnHostChanged;
            FollowHost();
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        if (_host is not null)
        {
            _host.PropertyChanged -= OnHostChanged;
            _host = null;
            PseudoClasses.Remove(":pane");
        }
    }

    private void OnHostChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SplitView.IsPaneOpenProperty || e.Property == SplitView.DisplayModeProperty || e.Property == SplitView.PanePlacementProperty)
        {
            FollowHost();
        }
    }

    // The SplitView's pane: closed is collapsed, CompactInline / CompactOverlay
    // collapse to icons (the closed pane is CompactPaneLength wide), Inline and
    // Overlay off the layout.
    private void FollowHost()
    {
        if (_host is not { } split)
        {
            return;
        }
        PseudoClasses.Set(":pane", true);
        SetCurrentValue(SideProperty, split.PanePlacement == SplitViewPanePlacement.Right ? Side.Right : Side.Left);
        SetCurrentValue(CollapsibleProperty, split.DisplayMode is SplitViewDisplayMode.CompactInline or SplitViewDisplayMode.CompactOverlay
            ? SidebarCollapsible.Icon
            : SidebarCollapsible.Offcanvas);
        SetCurrentValue(IsCollapsedProperty, !split.IsPaneOpen);
    }

    // mod.rs SidebarLayout: None ignores collapsed; Icon collapses to icons; Offcanvas
    // to nothing, its surface on the content side so it slides out.
    private void UpdateState()
    {
        var collapsed = IsCollapsed && Collapsible != SidebarCollapsible.None;
        var icons = collapsed && Collapsible == SidebarCollapsible.Icon;
        PseudoClasses.Set(":left", Side == Side.Left);
        PseudoClasses.Set(":right", Side == Side.Right);
        PseudoClasses.Set(":offcanvas", Collapsible == SidebarCollapsible.Offcanvas);
        PseudoClasses.Set(":icon-collapsed", icons);
        PseudoClasses.Set(":offcanvas-collapsed", collapsed && Collapsible == SidebarCollapsible.Offcanvas);
        SetValue(IsIconCollapsedProperty, icons);
    }
}

/// <summary>
/// GPUI Kit's SidebarToggleButton (sidebar/mod.rs): a small ghost button with
/// the panel icon of the sidebar's <see cref="Side"/> that flips
/// <see cref="IsCollapsed"/> (bind it two-way to the sidebar's), showing the
/// open icon while collapsed and the close icon while not.
/// </summary>
public class SidebarToggleButton : Button
{
    /// <summary>Whether the sidebar it toggles is collapsed; a click flips it. Binds two-way.</summary>
    public static readonly StyledProperty<bool> IsCollapsedProperty =
        Sidebar.IsCollapsedProperty.AddOwner<SidebarToggleButton>();

    /// <summary>The side of the sidebar it toggles: it picks the panel icon.</summary>
    public static readonly StyledProperty<Side> SideProperty =
        Sidebar.SideProperty.AddOwner<SidebarToggleButton>();

    private readonly PathIcon _icon = new();

    static SidebarToggleButton()
    {
        IsCollapsedProperty.Changed.AddClassHandler<SidebarToggleButton>((button, _) => button.UpdateIcon());
        SideProperty.Changed.AddClassHandler<SidebarToggleButton>((button, _) => button.UpdateIcon());
    }

    /// <summary>Creates a toggle button: GPUI's Button::new("collapse").ghost().small() with the icon.</summary>
    public SidebarToggleButton()
    {
        Classes.Add("ghost");
        Classes.Add("small");
        Classes.Add("icon-only");
        Content = _icon;
        UpdateIcon();
    }

    /// <inheritdoc cref="IsCollapsedProperty"/>
    public bool IsCollapsed
    {
        get => GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
    }

    /// <inheritdoc cref="SideProperty"/>
    public Side Side
    {
        get => GetValue(SideProperty);
        set => SetValue(SideProperty, value);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(Button);

    /// <inheritdoc />
    protected override void OnClick()
    {
        SetCurrentValue(IsCollapsedProperty, !IsCollapsed);
        base.OnClick();
    }

    // The button gives its icon its own size (Button::icon), 14px when small.
    private void UpdateIcon()
    {
        var key = (IsCollapsed, Side) switch
        {
            (true, Side.Right) => "UIKit.Icon.PanelRightOpen",
            (true, _) => "UIKit.Icon.PanelLeftOpen",
            (false, Side.Right) => "UIKit.Icon.PanelRightClose",
            _ => "UIKit.Icon.PanelLeftClose",
        };
        _icon.Bind(PathIcon.DataProperty, this.GetResourceObservable(key, value => value as Avalonia.Media.Geometry));
    }
}
