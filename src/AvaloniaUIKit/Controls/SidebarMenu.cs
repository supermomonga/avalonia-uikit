using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's SidebarHeader (sidebar/header.rs): a clickable row at the top of
/// a <see cref="Sidebar"/>, 8px in, in the theme radius, that takes the
/// sidebar accent when hovered or <see cref="IsSelected"/>. It is a Button: a
/// <see cref="Button.Flyout"/> (a MenuFlyout) is GPUI's <c>dropdown_menu</c>.
/// </summary>
[PseudoClasses(":selected", ":collapsed")]
public class SidebarHeader : Button
{
    /// <summary>Whether the row shows as selected (GPUI's Selectable).</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<SidebarHeader, bool>(nameof(IsSelected));

    static SidebarHeader()
    {
        IsSelectedProperty.Changed.AddClassHandler<SidebarHeader>((x, e) => x.PseudoClasses.Set(":selected", e.GetNewValue<bool>()));
        Sidebar.IsIconCollapsedProperty.Changed.AddClassHandler<SidebarHeader>((x, e) => x.PseudoClasses.Set(":collapsed", e.GetNewValue<bool>()));
    }

    /// <inheritdoc cref="IsSelectedProperty"/>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}

/// <summary>
/// GPUI Kit's SidebarFooter (sidebar/footer.rs): the <see cref="SidebarHeader"/>'s
/// row at the bottom of a <see cref="Sidebar"/>, with the same hover, selected
/// state and <see cref="Button.Flyout"/> (GPUI's <c>dropdown_menu</c>).
/// </summary>
[PseudoClasses(":selected", ":collapsed")]
public class SidebarFooter : Button
{
    /// <summary>Whether the row shows as selected (GPUI's Selectable).</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        SidebarHeader.IsSelectedProperty.AddOwner<SidebarFooter>();

    static SidebarFooter()
    {
        IsSelectedProperty.Changed.AddClassHandler<SidebarFooter>((x, e) => x.PseudoClasses.Set(":selected", e.GetNewValue<bool>()));
        Sidebar.IsIconCollapsedProperty.Changed.AddClassHandler<SidebarFooter>((x, e) => x.PseudoClasses.Set(":collapsed", e.GetNewValue<bool>()));
    }

    /// <inheritdoc cref="IsSelectedProperty"/>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}

/// <summary>
/// GPUI Kit's SidebarGroup (sidebar/group.rs): items under a muted 12px
/// <see cref="Label"/> in a 32px row; the label hides while the sidebar is
/// collapsed to icons.
/// </summary>
[PseudoClasses(":collapsed")]
public class SidebarGroup : ItemsControl
{
    /// <summary>The group's label (GPUI's <c>SidebarGroup::new(label)</c>).</summary>
    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<SidebarGroup, object?>(nameof(Label));

    static SidebarGroup()
    {
        Sidebar.IsIconCollapsedProperty.Changed.AddClassHandler<SidebarGroup>((x, e) => x.PseudoClasses.Set(":collapsed", e.GetNewValue<bool>()));
    }

    /// <inheritdoc cref="LabelProperty"/>
    public object? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
}

/// <summary>
/// GPUI Kit's SidebarMenu (sidebar/menu.rs): <see cref="SidebarMenuItem"/>s
/// 8px apart. Items that are not SidebarMenuItems get one as their container.
/// </summary>
public class SidebarMenu : ItemsControl
{
    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<SidebarMenuItem>(item, out recycleKey);

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SidebarMenuItem();
}

/// <summary>
/// GPUI Kit's SidebarMenuItem (sidebar/menu.rs): a row with an
/// <see cref="Icon"/>, a <see cref="Label"/> and a <see cref="Suffix"/>
/// (a Badge, a ToggleSwitch, an icon…), and nested items: a submenu under a
/// line, opened and closed by the caret at the row's end
/// (<see cref="IsOpen"/>, GPUI's <c>default_open</c> as its first value).
/// <para>
/// A hovered row takes the sidebar accent at 80%, an <see cref="IsActive"/>
/// one the sidebar accent and medium text, a disabled one muted text and no
/// hover. A click on the row (not the caret) raises <see cref="Click"/> and
/// runs <see cref="Command"/>; with <see cref="ClickToOpen"/> it also opens
/// the submenu, with <see cref="ClickToToggle"/> it opens or closes it. Set
/// <see cref="Control.ContextMenu"/> for GPUI's <c>context_menu</c>.
/// </para>
/// <para>
/// Collapsed to icons the row is the icon alone, 8px in all round, with the label
/// as a tooltip on its right; the submenu and caret hide.
/// </para>
/// </summary>
[TemplatePart("PART_Row", typeof(Control))]
[TemplatePart("PART_Caret", typeof(Button))]
[PseudoClasses(":active", ":open", ":collapsed", ":icon", ":submenu")]
public class SidebarMenuItem : ItemsControl
{
    /// <summary>The row's text (GPUI's <c>SidebarMenuItem::new(label)</c>), also its tooltip when collapsed to icons.</summary>
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<SidebarMenuItem, string?>(nameof(Label));

    /// <summary>The icon before the label (GPUI's <c>icon</c>), the text's size (14px).</summary>
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<SidebarMenuItem, Geometry?>(nameof(Icon));

    /// <summary>Content at the end of the row, before the caret (GPUI's <c>suffix</c>).</summary>
    public static readonly StyledProperty<object?> SuffixProperty =
        AvaloniaProperty.Register<SidebarMenuItem, object?>(nameof(Suffix));

    /// <summary>Whether the item is the current one (GPUI's <c>active</c>).</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<SidebarMenuItem, bool>(nameof(IsActive));

    /// <summary>Whether the submenu is open; set it for GPUI's <c>default_open</c>. Binds two-way.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<SidebarMenuItem, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Whether a click on the row opens the submenu (GPUI's <c>click_to_open</c>).</summary>
    public static readonly StyledProperty<bool> ClickToOpenProperty =
        AvaloniaProperty.Register<SidebarMenuItem, bool>(nameof(ClickToOpen));

    /// <summary>Whether a click on the row opens or closes the submenu (GPUI's <c>click_to_toggle</c>; not with <see cref="ClickToOpen"/>).</summary>
    public static readonly StyledProperty<bool> ClickToToggleProperty =
        AvaloniaProperty.Register<SidebarMenuItem, bool>(nameof(ClickToToggle));

    /// <summary>The command a click on the row runs.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        Button.CommandProperty.AddOwner<SidebarMenuItem>();

    /// <summary>The parameter of <see cref="Command"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        Button.CommandParameterProperty.AddOwner<SidebarMenuItem>();

    /// <summary>Raised when the row is clicked (GPUI's <c>on_click</c>).</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<SidebarMenuItem, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private Control? _row;
    private Button? _caret;
    private bool _rowPressed;

    static SidebarMenuItem()
    {
        IsActiveProperty.Changed.AddClassHandler<SidebarMenuItem>((x, e) => x.PseudoClasses.Set(":active", e.GetNewValue<bool>()));
        IsOpenProperty.Changed.AddClassHandler<SidebarMenuItem>((x, e) => x.PseudoClasses.Set(":open", e.GetNewValue<bool>()));
        IconProperty.Changed.AddClassHandler<SidebarMenuItem>((x, _) => x.UpdateState());
        Sidebar.IsIconCollapsedProperty.Changed.AddClassHandler<SidebarMenuItem>((x, _) => x.UpdateState());
    }

    /// <summary>Creates a menu item.</summary>
    public SidebarMenuItem()
    {
        UpdateState();
    }

    /// <inheritdoc cref="LabelProperty"/>
    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="SuffixProperty"/>
    public object? Suffix
    {
        get => GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    /// <inheritdoc cref="IsActiveProperty"/>
    public bool IsActive
    {
        get => GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <inheritdoc cref="IsOpenProperty"/>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <inheritdoc cref="ClickToOpenProperty"/>
    public bool ClickToOpen
    {
        get => GetValue(ClickToOpenProperty);
        set => SetValue(ClickToOpenProperty, value);
    }

    /// <inheritdoc cref="ClickToToggleProperty"/>
    public bool ClickToToggle
    {
        get => GetValue(ClickToToggleProperty);
        set => SetValue(ClickToToggleProperty, value);
    }

    /// <inheritdoc cref="CommandProperty"/>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <inheritdoc cref="CommandParameterProperty"/>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <inheritdoc cref="ClickEvent"/>
    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<SidebarMenuItem>(item, out recycleKey);

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SidebarMenuItem();

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemCountProperty)
        {
            PseudoClasses.Set(":submenu", ItemCount > 0);
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _row?.RemoveHandler(PointerPressedEvent, OnRowPressed);
        _row?.RemoveHandler(PointerReleasedEvent, OnRowReleased);
        _caret?.RemoveHandler(Button.ClickEvent, OnCaretClick);
        _row = e.NameScope.Find<Control>("PART_Row");
        _caret = e.NameScope.Find<Button>("PART_Caret");
        _row?.AddHandler(PointerPressedEvent, OnRowPressed);
        _row?.AddHandler(PointerReleasedEvent, OnRowReleased);
        _caret?.AddHandler(Button.ClickEvent, OnCaretClick);
    }

    // menu.rs: the caret opens and closes the submenu without clicking the item.
    private void OnCaretClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        SetCurrentValue(IsOpenProperty, !IsOpen);
    }

    // A left press on the row (not on the caret, which handles its own) starts a click.
    private void OnRowPressed(object? sender, PointerPressedEventArgs e)
    {
        _rowPressed = !e.Handled && IsEffectivelyEnabled && e.GetCurrentPoint(_row).Properties.IsLeftButtonPressed;
    }

    // menu.rs on_click (not on a disabled item), on every click (a second one is
    // no double click): click_to_open opens, else click_to_toggle toggles; then
    // the handler runs.
    private void OnRowReleased(object? sender, PointerReleasedEventArgs e)
    {
        var pressed = _rowPressed;
        _rowPressed = false;
        if (!pressed || e.InitialPressMouseButton != MouseButton.Left || _row is null
            || !new Rect(_row.Bounds.Size).Contains(e.GetPosition(_row)))
        {
            return;
        }
        e.Handled = true;
        if (ItemCount > 0)
        {
            if (ClickToOpen)
            {
                SetCurrentValue(IsOpenProperty, true);
            }
            else if (ClickToToggle)
            {
                SetCurrentValue(IsOpenProperty, !IsOpen);
            }
        }
        RaiseEvent(new RoutedEventArgs(ClickEvent));
        if (Command is { } command && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }

    // menu.rs collapsed_tooltip: collapsed to icons, an item with an icon shows its
    // label as a tooltip on its right (the theme puts it on the row).
    private void UpdateState()
    {
        PseudoClasses.Set(":collapsed", Sidebar.GetIsIconCollapsed(this));
        PseudoClasses.Set(":icon", Icon is not null);
    }
}
