using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace AvaloniaUIKit;

/// <summary>The surface of a <see cref="SettingGroup"/> (GPUI's GroupBoxVariant, the GroupBox theme's classes).</summary>
public enum GroupBoxVariant
{
    /// <summary>No surface: the items sit on the page.</summary>
    Normal,

    /// <summary>The group_box color, 16px in.</summary>
    Fill,

    /// <summary>A 1px border, 16px in.</summary>
    Outline,
}

/// <summary>
/// GPUI Kit's Settings (crates/component/src/setting/settings.rs): a
/// <see cref="Sidebar"/> with a search box and a menu of the pages beside the
/// selected <see cref="SettingPage"/>, the boundary between them resizable
/// (<see cref="SidebarWidth"/>, 250px, from <see cref="SidebarMinWidth"/> to
/// <see cref="SidebarMaxWidth"/>). Its items are SettingPages.
/// <para>
/// A page's menu item opens its titled groups under it when the page has more
/// than one; clicking the page selects it (<see cref="SelectedIndex"/>), and
/// clicking a group selects it too (<see cref="SelectedGroup"/>) and scrolls
/// the page to it.
/// </para>
/// <para>
/// <see cref="SearchText"/> (the search box) keeps the items whose title,
/// description or keywords contain it, ignoring case, the groups with such
/// items and the pages with such groups; an item without a title matches only
/// its keywords, and every item matches an empty search. The selected page
/// stays while it matches, otherwise the first page that matches is selected
/// (and the selected group stays only while it matches); with no match the
/// selection stays for when there are results again, and no page shows. A
/// change of search, like a change of page, puts the page back at its top, or
/// at the selected group.
/// </para>
/// <para>
/// <see cref="GroupVariantProperty"/> (inherited) is the groups' surface; a
/// group can set its own. The size classes (xsmall, small, large) go on every
/// item's content, as GPUI sizes its fields.
/// </para>
/// </summary>
[TemplatePart("PART_Menu", typeof(SidebarMenu))]
public class Settings : ItemsControl
{
    /// <summary>The index of the selected page among the items (GPUI's <c>default_selected_index</c> as its first value); binds two-way.</summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<Settings, int>(nameof(SelectedIndex), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The group selected in the sidebar, or null for the whole page; binds two-way.</summary>
    public static readonly StyledProperty<SettingGroup?> SelectedGroupProperty =
        AvaloniaProperty.Register<Settings, SettingGroup?>(nameof(SelectedGroup), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The text in the search box; binds two-way.</summary>
    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<Settings, string?>(nameof(SearchText), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The search box's placeholder (GPUI's <c>Settings.search_placeholder</c>, "Search...").</summary>
    public static readonly StyledProperty<string?> SearchPlaceholderTextProperty =
        AvaloniaProperty.Register<Settings, string?>(nameof(SearchPlaceholderText), "Search...");

    /// <summary>The sidebar's initial width (GPUI's <c>sidebar_width</c>, 250).</summary>
    public static readonly StyledProperty<double> SidebarWidthProperty =
        AvaloniaProperty.Register<Settings, double>(nameof(SidebarWidth), 250);

    /// <summary>The sidebar's smallest width (the start of GPUI's <c>sidebar_size_range</c>, 160).</summary>
    public static readonly StyledProperty<double> SidebarMinWidthProperty =
        AvaloniaProperty.Register<Settings, double>(nameof(SidebarMinWidth), 160);

    /// <summary>The sidebar's largest width (the end of GPUI's <c>sidebar_size_range</c>, 360).</summary>
    public static readonly StyledProperty<double> SidebarMaxWidthProperty =
        AvaloniaProperty.Register<Settings, double>(nameof(SidebarMaxWidth), 360);

    /// <summary>
    /// The surface of the groups inside (GPUI's <c>with_group_variant</c>,
    /// Normal by default). Inherited: set on the settings for every group, or
    /// on a group (<see cref="SettingGroup.Variant"/>) for that one.
    /// </summary>
    public static readonly AttachedProperty<GroupBoxVariant> GroupVariantProperty =
        AvaloniaProperty.RegisterAttached<Settings, Control, GroupBoxVariant>("GroupVariant", inherits: true);

    private readonly Dictionary<SettingPage, SidebarMenuItem> _pageItems = [];
    private readonly Dictionary<SettingGroup, SidebarMenuItem> _groupItems = [];
    private SidebarMenu? _menu;
    // The page shown, so a change of page can put the new one back at its top.
    private SettingPage? _shown;
    private bool _refreshing;

    static Settings()
    {
        SearchTextProperty.Changed.AddClassHandler<Settings>((x, _) => x.Refresh(searched: true));
        SelectedIndexProperty.Changed.AddClassHandler<Settings>((x, _) => x.Refresh());
        SelectedGroupProperty.Changed.AddClassHandler<Settings>((x, e) => x.OnSelectedGroupChanged(e.GetNewValue<SettingGroup?>()));
    }

    /// <summary>Creates the settings.</summary>
    public Settings()
    {
        Items.CollectionChanged += (_, _) => Refresh();
        Classes.CollectionChanged += (_, _) => PassClasses();
    }

    /// <inheritdoc cref="SelectedIndexProperty"/>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <inheritdoc cref="SelectedGroupProperty"/>
    public SettingGroup? SelectedGroup
    {
        get => GetValue(SelectedGroupProperty);
        set => SetValue(SelectedGroupProperty, value);
    }

    /// <inheritdoc cref="SearchTextProperty"/>
    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <inheritdoc cref="SearchPlaceholderTextProperty"/>
    public string? SearchPlaceholderText
    {
        get => GetValue(SearchPlaceholderTextProperty);
        set => SetValue(SearchPlaceholderTextProperty, value);
    }

    /// <inheritdoc cref="SidebarWidthProperty"/>
    public double SidebarWidth
    {
        get => GetValue(SidebarWidthProperty);
        set => SetValue(SidebarWidthProperty, value);
    }

    /// <inheritdoc cref="SidebarMinWidthProperty"/>
    public double SidebarMinWidth
    {
        get => GetValue(SidebarMinWidthProperty);
        set => SetValue(SidebarMinWidthProperty, value);
    }

    /// <inheritdoc cref="SidebarMaxWidthProperty"/>
    public double SidebarMaxWidth
    {
        get => GetValue(SidebarMaxWidthProperty);
        set => SetValue(SidebarMaxWidthProperty, value);
    }

    /// <inheritdoc cref="GroupVariantProperty"/>
    public GroupBoxVariant GroupVariant
    {
        get => GetValue(GroupVariantProperty);
        set => SetValue(GroupVariantProperty, value);
    }

    /// <summary>The pages, in order.</summary>
    public IEnumerable<SettingPage> Pages => Items.OfType<SettingPage>();

    /// <summary>Gets the surface of the groups inside a control.</summary>
    public static GroupBoxVariant GetGroupVariant(Control control) => control.GetValue(GroupVariantProperty);

    /// <summary>Sets the surface of the groups inside a control.</summary>
    public static void SetGroupVariant(Control control, GroupBoxVariant value) => control.SetValue(GroupVariantProperty, value);

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<SettingPage>(item, out recycleKey);

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new SettingPage();

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _menu?.RemoveHandler(SidebarMenuItem.ClickEvent, OnMenuClick);
        _menu?.Items.Clear();
        _menu = e.NameScope.Find<SidebarMenu>("PART_Menu");
        _menu?.AddHandler(SidebarMenuItem.ClickEvent, OnMenuClick);
        Refresh();
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Refresh();
    }

    /// <summary>The settings a page, group or item is in.</summary>
    internal static Settings? Of(StyledElement element)
    {
        for (var parent = element.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent is Settings settings)
            {
                return settings;
            }
        }
        return null;
    }

    /// <summary>The size classes the items' content takes.</summary>
    internal List<string> FieldClasses() => GroupClasses.Pick(this, GroupClasses.Sizes);

    /// <summary>
    /// Filters the pages by <see cref="SearchText"/>, keeps the selection on a
    /// page that matches, shows the selected page and builds the sidebar's
    /// menu (settings.rs SettingsFilter and render_sidebar). Pages, groups and
    /// items call it when what they show or match changes.
    /// </summary>
    internal void Refresh(bool searched = false)
    {
        // Until the pages are all in (XAML sets SelectedIndex before it adds them), the
        // selection must not be moved to a page that is there.
        if (_refreshing || !IsInitialized)
        {
            return;
        }
        _refreshing = true;
        try
        {
            var query = SearchText ?? "";
            var pages = Pages.ToList();
            var visible = new List<List<SettingGroup>>(pages.Count);
            foreach (var page in pages)
            {
                visible.Add(page.Filter(query));
            }

            // SettingsFilter::selected_index: the page stays while it has matches, else the
            // first page with matches; the group stays only on the same page while it matches.
            var selected = SelectedIndex;
            var group = SelectedGroup;
            var pageIndex = selected >= 0 && selected < pages.Count && visible[selected].Count > 0
                ? selected
                : visible.FindIndex(groups => groups.Count > 0);
            if (pageIndex >= 0)
            {
                if (pageIndex != selected || (group is not null && !visible[pageIndex].Contains(group)))
                {
                    group = null;
                }
                SetCurrentValue(SelectedIndexProperty, pageIndex);
                SetCurrentValue(SelectedGroupProperty, group);
            }

            SettingPage? shown = null;
            for (var i = 0; i < pages.Count; i++)
            {
                var show = i == pageIndex;
                pages[i].SetShown(show);
                if (show)
                {
                    shown = pages[i];
                }
            }
            if (shown is not null && (searched || shown != _shown))
            {
                // page.rs: a new page or search rebuilds the list at the top, or at the selected group.
                shown.ScrollTo(group);
            }
            _shown = shown;
            UpdateMenu(pages, visible, pageIndex, group);
        }
        finally
        {
            _refreshing = false;
        }
    }

    // A group chosen from code or the menu selects its page and scrolls into view; one
    // cleared leaves the page where it is.
    private void OnSelectedGroupChanged(SettingGroup? group)
    {
        if (_refreshing)
        {
            return;
        }
        if (group?.Parent is SettingPage page && Pages.ToList().IndexOf(page) is var index and >= 0)
        {
            SetCurrentValue(SelectedIndexProperty, index);
        }
        Refresh();
        if (group is not null && SelectedGroup == group)
        {
            _shown?.ScrollTo(group);
        }
    }

    // settings.rs render_sidebar: a menu item per page with matches, opened by a click, and
    // under it the titled groups with matches when the page has more than one. A page's
    // item is active while the page is selected without a group (or with only one group),
    // a group's while it is selected.
    private void UpdateMenu(List<SettingPage> pages, List<List<SettingGroup>> visible, int pageIndex, SettingGroup? group)
    {
        if (_menu is null)
        {
            return;
        }
        var entries = new List<SidebarMenuItem>();
        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            var groups = visible[i];
            if (groups.Count == 0)
            {
                continue;
            }
            var entry = PageItem(page);
            entry.Label = page.Title;
            entry.Icon = page.Icon;
            entry.IsActive = i == pageIndex && (group is null || groups.Count == 1);
            var children = new List<SidebarMenuItem>();
            if (groups.Count > 1)
            {
                foreach (var g in groups.Where(g => g.Title is not null))
                {
                    var child = GroupItem(g);
                    child.Label = g.Title;
                    child.IsActive = i == pageIndex && g == group;
                    children.Add(child);
                }
            }
            Sync(entry.Items, children);
            entries.Add(entry);
        }
        Sync(_menu.Items, entries);
        foreach (var page in _pageItems.Keys.Where(p => !pages.Contains(p)).ToList())
        {
            _pageItems.Remove(page);
        }
        foreach (var g in _groupItems.Keys.Where(g => !visible.Any(groups => groups.Contains(g))).ToList())
        {
            _groupItems.Remove(g);
        }
    }

    private SidebarMenuItem PageItem(SettingPage page)
    {
        if (_pageItems.TryGetValue(page, out var item))
        {
            return item;
        }
        // menu.rs click_to_open; the page's IsOpen is GPUI's default_open, kept in step both ways.
        item = new SidebarMenuItem { ClickToOpen = true, IsOpen = page.IsOpen };
        item.PropertyChanged += (_, e) =>
        {
            if (e.Property == SidebarMenuItem.IsOpenProperty)
            {
                page.SetCurrentValue(SettingPage.IsOpenProperty, e.GetNewValue<bool>());
            }
        };
        _pageItems[page] = item;
        return item;
    }

    private SidebarMenuItem GroupItem(SettingGroup group)
    {
        if (!_groupItems.TryGetValue(group, out var item))
        {
            item = new SidebarMenuItem();
            _groupItems[group] = item;
        }
        return item;
    }

    /// <summary>Keeps a page's menu item open or closed with the page.</summary>
    internal void OnPageOpenChanged(SettingPage page, bool open)
    {
        if (_pageItems.TryGetValue(page, out var item))
        {
            item.SetCurrentValue(SidebarMenuItem.IsOpenProperty, open);
        }
    }

    // Changes the items only where they differ, so the menu keeps its rows (and their hover).
    private static void Sync(ItemCollection items, List<SidebarMenuItem> wanted)
    {
        if (items.Count == wanted.Count && items.Cast<object?>().SequenceEqual(wanted))
        {
            return;
        }
        items.Clear();
        foreach (var item in wanted)
        {
            items.Add(item);
        }
    }

    // settings.rs: a page's item selects the page; a group's selects the group and scrolls to it.
    private void OnMenuClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not SidebarMenuItem item)
        {
            return;
        }
        var pages = Pages.ToList();
        foreach (var (page, entry) in _pageItems)
        {
            if (entry == item)
            {
                SetCurrentValue(SelectedGroupProperty, null);
                SetCurrentValue(SelectedIndexProperty, pages.IndexOf(page));
                return;
            }
        }
        foreach (var (group, entry) in _groupItems)
        {
            if (entry == item)
            {
                // A group already selected scrolls to it again (deferred_scroll_group_ix).
                SetCurrentValue(SelectedGroupProperty, group);
                _shown?.ScrollTo(group);
                return;
            }
        }
    }

    // GPUI sizes its fields with the settings' size: the size classes go on every item's content.
    private void PassClasses()
    {
        foreach (var page in Pages)
        {
            foreach (var group in page.Items.OfType<SettingGroup>())
            {
                foreach (var item in group.Items.OfType<SettingItem>())
                {
                    item.PassClasses();
                }
            }
        }
    }
}
