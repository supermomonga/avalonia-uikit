using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's SettingPage (crates/component/src/setting/page.rs): a page of
/// <see cref="SettingGroup"/>s in a <see cref="Settings"/>. The header has the
/// <see cref="Title"/>, a <see cref="TitleSuffix"/> after it, the reset
/// button at its end and the <see cref="Description"/> under it, above a 1px
/// line; the groups scroll under it, 16px in. In the sidebar the page is a menu
/// item with its <see cref="Icon"/>, open (<see cref="IsOpen"/>, GPUI's
/// <c>default_open</c>) or closed over its groups.
/// <para>
/// The reset button (GPUI's "Reset All") shows while the page is
/// <see cref="IsResettable"/> and an item the search keeps is
/// <see cref="SettingItem.IsModified"/>; it resets those items
/// (<see cref="ResetAll"/>). Narrower than 480px, the page lays its items out
/// vertically (settings.rs STACKED_LAYOUT_MAX_WIDTH).
/// </para>
/// </summary>
[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
[TemplatePart("PART_ResetButton", typeof(Button))]
[PseudoClasses(":unselected", ":resettable")]
public class SettingPage : ItemsControl
{
    /// <summary>The page's title (GPUI's <c>SettingPage::new(title)</c>), also its label in the sidebar.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SettingPage, string?>(nameof(Title));

    /// <summary>Content after the title in the header (GPUI's <c>title_suffix</c>), such as a help button.</summary>
    public static readonly StyledProperty<object?> TitleSuffixProperty =
        AvaloniaProperty.Register<SettingPage, object?>(nameof(TitleSuffix));

    /// <summary>The icon before the page's label in the sidebar (GPUI's <c>icon</c>).</summary>
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<SettingPage, Geometry?>(nameof(Icon));

    /// <summary>Small muted text under the title (GPUI's <c>description</c>).</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<SettingPage, string?>(nameof(Description));

    /// <summary>Whether the page's groups show under it in the sidebar (GPUI's <c>default_open</c> as its first value); binds two-way.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<SettingPage, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Whether the header shows the reset button while an item is modified (GPUI's <c>resettable</c>, true by default).</summary>
    public static readonly StyledProperty<bool> IsResettableProperty =
        AvaloniaProperty.Register<SettingPage, bool>(nameof(IsResettable), true);

    /// <summary>Whether the items lay out vertically: set on a page narrower than 480px, inherited by its items.</summary>
    internal static readonly AttachedProperty<bool> IsStackedProperty =
        AvaloniaProperty.RegisterAttached<SettingPage, Control, bool>("IsStacked", inherits: true);

    // settings.rs STACKED_LAYOUT_MAX_WIDTH.
    private const double StackedMaxWidth = 480;

    private ScrollViewer? _scrollViewer;
    private ItemsPresenter? _presenter;
    private Button? _resetButton;
    // A scroll to the top or to a group, done once the page has been laid out.
    private bool _scrollPending;
    private SettingGroup? _scrollGroup;
    private string _query = "";

    static SettingPage()
    {
        TitleProperty.Changed.AddClassHandler<SettingPage>((x, _) => x.Changed());
        IconProperty.Changed.AddClassHandler<SettingPage>((x, _) => x.Changed());
        IsResettableProperty.Changed.AddClassHandler<SettingPage>((x, _) => x.UpdateResettable());
        IsOpenProperty.Changed.AddClassHandler<SettingPage>((x, e) => Settings.Of(x)?.OnPageOpenChanged(x, e.GetNewValue<bool>()));
    }

    /// <summary>Creates a page.</summary>
    public SettingPage()
    {
        Items.CollectionChanged += (_, _) => Changed();
    }

    /// <inheritdoc cref="TitleProperty"/>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="TitleSuffixProperty"/>
    public object? TitleSuffix
    {
        get => GetValue(TitleSuffixProperty);
        set => SetValue(TitleSuffixProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="DescriptionProperty"/>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <inheritdoc cref="IsOpenProperty"/>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <inheritdoc cref="IsResettableProperty"/>
    public bool IsResettable
    {
        get => GetValue(IsResettableProperty);
        set => SetValue(IsResettableProperty, value);
    }

    /// <summary>
    /// Resets the items the search keeps (page.rs reset_all): each takes its
    /// <see cref="SettingItem.DefaultValue"/> and raises
    /// <see cref="SettingItem.Reset"/>.
    /// </summary>
    public void ResetAll()
    {
        foreach (var item in Groups().SelectMany(g => g.Items.OfType<SettingItem>()))
        {
            if (item.Matches(_query))
            {
                item.ResetValue();
            }
        }
    }

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<SettingGroup>(item, out recycleKey);

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SettingGroup();

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _resetButton?.RemoveHandler(Button.ClickEvent, OnResetClick);
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _presenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
        _resetButton = e.NameScope.Find<Button>("PART_ResetButton");
        _resetButton?.AddHandler(Button.ClickEvent, OnResetClick);
        if (_scrollPending)
        {
            ScrollTo(_scrollGroup);
        }
    }

    /// <inheritdoc />
    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        // settings.rs container_query: the items stack on a narrow page.
        SetValue(IsStackedProperty, e.NewSize.Width <= StackedMaxWidth);
    }

    /// <summary>The groups, in order.</summary>
    internal IEnumerable<SettingGroup> Groups() => Items.OfType<SettingGroup>();

    /// <summary>Marks the items, groups and the page by <paramref name="query"/> and returns the groups with matches.</summary>
    internal List<SettingGroup> Filter(string query)
    {
        _query = query;
        var visible = new List<SettingGroup>();
        foreach (var group in Groups())
        {
            if (group.Filter(query))
            {
                visible.Add(group);
            }
        }
        UpdateResettable();
        return visible;
    }

    /// <summary>Shows the page as the selected one, or hides it.</summary>
    internal void SetShown(bool shown) => PseudoClasses.Set(":unselected", !shown);

    /// <summary>Scrolls to <paramref name="group"/>'s top, or the page's top for null, once the page is laid out.</summary>
    internal void ScrollTo(SettingGroup? group)
    {
        _scrollPending = true;
        _scrollGroup = group;
        LayoutUpdated -= OnLayoutUpdated;
        LayoutUpdated += OnLayoutUpdated;
        InvalidateArrange();
    }

    /// <summary>Shows or hides the reset button by the items the search keeps.</summary>
    internal void UpdateResettable() =>
        PseudoClasses.Set(":resettable", IsResettable
            && Groups().SelectMany(g => g.Items.OfType<SettingItem>()).Any(i => i.IsModified && i.Matches(_query)));

    /// <summary>Tells the settings that what the page shows or matches has changed.</summary>
    internal void Changed()
    {
        UpdateResettable();
        Settings.Of(this)?.Refresh();
    }

    // page.rs scroll_to: the group's top (with its 16px above) at the top of the list.
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_scrollViewer is null || _presenter is null || !IsEffectivelyVisible)
        {
            return;
        }
        LayoutUpdated -= OnLayoutUpdated;
        _scrollPending = false;
        var top = 0.0;
        if (_scrollGroup is { IsEffectivelyVisible: true } group && group.TranslatePoint(default, _presenter) is { } at)
        {
            top = at.Y + _presenter.Margin.Top;
        }
        _scrollGroup = null;
        _scrollViewer.Offset = new Vector(_scrollViewer.Offset.X, top);
    }

    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        ResetAll();
    }
}
