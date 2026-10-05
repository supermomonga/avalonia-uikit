using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>A page the user picked on a <see cref="Pagination"/> (GPUI's on_click).</summary>
public class PageChangedEventArgs(RoutedEvent routedEvent, int oldPage, int newPage) : RoutedEventArgs(routedEvent)
{
    /// <summary>The page shown before.</summary>
    public int OldPage { get; } = oldPage;

    /// <summary>The page picked, now <see cref="Pagination.CurrentPage"/>.</summary>
    public int NewPage { get; } = newPage;
}

/// <summary>
/// One entry of a pagination row (gpui_base pagination.rs PaginationItem): a
/// page, or an ellipsis standing for the hidden pages <see cref="Start"/> to
/// <see cref="End"/> (exclusive).
/// </summary>
public readonly record struct PaginationItem(int Start, int End)
{
    /// <summary>A page button.</summary>
    public static PaginationItem Page(int page) => new(page, page);

    /// <summary>An ellipsis over the pages <paramref name="start"/> to <paramref name="end"/> (exclusive).</summary>
    public static PaginationItem Ellipsis(int start, int end) => new(start, end);

    /// <summary>Whether the entry is an ellipsis.</summary>
    public bool IsEllipsis => End != Start;
}

/// <summary>
/// GPUI Kit's Pagination (crates/component/src/pagination.rs over
/// crates/base/src/pagination.rs): Previous, the page buttons and Next. The
/// first and last pages always show; past <see cref="VisiblePages"/> the
/// pages around <see cref="CurrentPage"/> show and an ellipsis stands for
/// each run of hidden pages. An ellipsis opens a menu of its pages (at most
/// 100, the ones nearest the current page) to jump to one directly.
/// <para>
/// A click on a page, Previous, Next or a menu entry sets
/// <see cref="CurrentPage"/> and raises <see cref="PageChanged"/>. Previous
/// and Next are disabled at the ends. Classes: xsmall small large (medium is
/// the default) and compact (Previous and Next alone, as icons).
/// </para>
/// </summary>
[TemplatePart("PART_PreviousButton", typeof(Button))]
[TemplatePart("PART_NextButton", typeof(Button))]
[TemplatePart("PART_Pages", typeof(Panel))]
public class Pagination : TemplatedControl
{
    /// <summary>The current page, from 1 (GPUI's current_page), kept within 1 to <see cref="TotalPages"/>.</summary>
    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<Pagination, int>(nameof(CurrentPage), 1,
            defaultBindingMode: BindingMode.TwoWay, coerce: CoerceCurrentPage);

    /// <summary>The number of pages, at least 1 (GPUI's total_pages).</summary>
    public static readonly StyledProperty<int> TotalPagesProperty =
        AvaloniaProperty.Register<Pagination, int>(nameof(TotalPages), 1, coerce: (_, value) => Math.Max(1, value));

    /// <summary>How many buttons the pages may take before ellipses replace some, at least 5 (GPUI's visible_pages).</summary>
    public static readonly StyledProperty<int> VisiblePagesProperty =
        AvaloniaProperty.Register<Pagination, int>(nameof(VisiblePages), 5);

    /// <summary>Raised when the user picks a page (GPUI's on_click).</summary>
    public static readonly RoutedEvent<PageChangedEventArgs> PageChangedEvent =
        RoutedEvent.Register<Pagination, PageChangedEventArgs>(nameof(PageChanged), RoutingStrategies.Bubble);

    /// <summary>pagination.rs MAX_ELLIPSIS_MENU_PAGES: the most pages an ellipsis menu lists.</summary>
    internal const int MaxEllipsisMenuPages = 100;

    private static readonly string[] SizeClasses = ["xsmall", "small", "large"];

    private Button? _previous;
    private Button? _next;
    private Panel? _pages;

    static Pagination()
    {
        TotalPagesProperty.Changed.AddClassHandler<Pagination>((p, _) =>
        {
            p.CoerceValue(CurrentPageProperty);
            p.Update();
        });
        CurrentPageProperty.Changed.AddClassHandler<Pagination>((p, _) => p.Update());
        VisiblePagesProperty.Changed.AddClassHandler<Pagination>((p, _) => p.Update());
    }

    /// <summary>Creates the pagination; its size and compact classes reach the buttons it builds.</summary>
    public Pagination()
    {
        Classes.CollectionChanged += (_, _) => Update();
    }

    /// <inheritdoc cref="CurrentPageProperty"/>
    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    /// <inheritdoc cref="TotalPagesProperty"/>
    public int TotalPages
    {
        get => GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    /// <inheritdoc cref="VisiblePagesProperty"/>
    public int VisiblePages
    {
        get => GetValue(VisiblePagesProperty);
        set => SetValue(VisiblePagesProperty, value);
    }

    /// <inheritdoc cref="PageChangedEvent"/>
    public event EventHandler<PageChangedEventArgs>? PageChanged
    {
        add => AddHandler(PageChangedEvent, value);
        remove => RemoveHandler(PageChangedEvent, value);
    }

    /// <summary>The page buttons and ellipses the row shows now (empty when compact).</summary>
    public IReadOnlyList<PaginationItem> Items =>
        Classes.Contains("compact") ? [] : CalculateItems(CurrentPage, TotalPages, VisiblePages);

    /// <summary>
    /// Goes to <paramref name="page"/> as a click would: nothing happens when
    /// the pagination is disabled, the page is current or out of range
    /// (PaginationState::request_page); otherwise <see cref="CurrentPage"/>
    /// changes and <see cref="PageChanged"/> is raised.
    /// </summary>
    public bool RequestPage(int page)
    {
        var current = CurrentPage;
        if (!IsEffectivelyEnabled || page == current || page < 1 || page > TotalPages)
        {
            return false;
        }
        SetCurrentValue(CurrentPageProperty, page);
        RaiseEvent(new PageChangedEventArgs(PageChangedEvent, current, page));
        return true;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_previous is not null)
        {
            _previous.Click -= OnPreviousClick;
        }
        if (_next is not null)
        {
            _next.Click -= OnNextClick;
        }
        _pages?.Children.Clear();
        _previous = e.NameScope.Find<Button>("PART_PreviousButton");
        _next = e.NameScope.Find<Button>("PART_NextButton");
        _pages = e.NameScope.Find<Panel>("PART_Pages");
        if (_previous is not null)
        {
            _previous.Click += OnPreviousClick;
        }
        if (_next is not null)
        {
            _next.Click += OnNextClick;
        }
        Update();
    }

    private static int CoerceCurrentPage(AvaloniaObject owner, int value) =>
        Math.Clamp(value, 1, Math.Max(1, owner.GetValue(TotalPagesProperty)));

    private void OnPreviousClick(object? sender, RoutedEventArgs e) => RequestPage(CurrentPage - 1);

    private void OnNextClick(object? sender, RoutedEventArgs e) => RequestPage(CurrentPage + 1);

    private void Update()
    {
        var current = CurrentPage;
        if (_previous is not null)
        {
            _previous.IsEnabled = current > 1;
        }
        if (_next is not null)
        {
            _next.IsEnabled = current < TotalPages;
        }
        if (_pages is null)
        {
            return;
        }
        var items = Items;
        var size = SizeClasses.FirstOrDefault(Classes.Contains);
        while (_pages.Children.Count > items.Count)
        {
            _pages.Children.RemoveAt(_pages.Children.Count - 1);
        }
        while (_pages.Children.Count < items.Count)
        {
            _pages.Children.Add(new PageButton(this));
        }
        for (var i = 0; i < items.Count; i++)
        {
            ((PageButton)_pages.Children[i]).Show(items[i], current, size);
        }
        _pages.IsVisible = items.Count > 0;
    }

    /// <summary>
    /// The row for <paramref name="current"/> of <paramref name="total"/> pages
    /// (gpui_base pagination.rs calculate_items): every page up to
    /// <paramref name="maxVisible"/> (at least 5), else the first and last
    /// pages, (max − 3) / 2 pages on each side of the current one, and an
    /// ellipsis for each hidden run.
    /// </summary>
    internal static IReadOnlyList<PaginationItem> CalculateItems(int current, int total, int maxVisible)
    {
        if (total <= 1)
        {
            return [];
        }
        maxVisible = Math.Max(5, maxVisible);
        current = Math.Clamp(current, 1, total);
        if (total <= maxVisible)
        {
            return [.. Enumerable.Range(1, total).Select(PaginationItem.Page)];
        }
        var pages = new List<PaginationItem> { PaginationItem.Page(1) };
        var side = (maxVisible - 3) / 2;
        var start = current <= side + 1 ? 2 : current > total - side - 1 ? total - side - 1 : current - side;
        if (start > 2)
        {
            pages.Add(PaginationItem.Ellipsis(2, start));
        }
        var end = current >= total - side ? total - 1 : current <= side + 1 ? side + 2 : current + side;
        for (var page = start; page <= end; page++)
        {
            pages.Add(PaginationItem.Page(page));
        }
        if (end < total - 1)
        {
            pages.Add(PaginationItem.Ellipsis(end + 1, total));
        }
        pages.Add(PaginationItem.Page(total));
        return pages;
    }

    /// <summary>
    /// The pages an ellipsis menu lists (pagination.rs ellipsis_menu_pages):
    /// all of <paramref name="start"/> to <paramref name="end"/> (exclusive),
    /// or the 100 nearest <paramref name="current"/>.
    /// </summary>
    internal static (int Start, int End) EllipsisMenuPages(int start, int end, int current)
    {
        if (end - start <= MaxEllipsisMenuPages)
        {
            return (start, end);
        }
        return start > current ? (start, start + MaxEllipsisMenuPages) : (end - MaxEllipsisMenuPages, end);
    }

    /// <summary>
    /// A page button (ghost, outline for the current page) or an ellipsis
    /// button (ghost, the Ellipsis icon) whose menu lists its hidden pages.
    /// Both are compact Buttons and take the Button theme.
    /// </summary>
    private sealed class PageButton : Button
    {
        private readonly Pagination _owner;
        private readonly MenuFlyout _menu = new() { Placement = PlacementMode.BottomEdgeAlignedLeft };
        private PaginationItem _item;
        private int _current;

        public PageButton(Pagination owner)
        {
            _owner = owner;
        }

        protected override Type StyleKeyOverride => typeof(Button);

        public void Show(PaginationItem item, int current, string? size)
        {
            _item = item;
            _current = current;
            var selected = !item.IsEllipsis && item.Start == current;
            Classes.Set("ghost", !selected);
            Classes.Set("outline", selected);
            Classes.Set("compact", true);
            Classes.Set("icon-only", item.IsEllipsis);
            foreach (var name in SizeClasses)
            {
                Classes.Set(name, name == size);
            }
            if (item.IsEllipsis)
            {
                if (Content is not PathIcon)
                {
                    var icon = new PathIcon();
                    icon.Bind(PathIcon.DataProperty, this.GetResourceObservable("UIKit.Icon.Ellipsis"));
                    Content = icon;
                }
                Flyout = _menu;
            }
            else
            {
                Content = item.Start.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Flyout = null;
            }
        }

        protected override void OnClick()
        {
            if (_item.IsEllipsis)
            {
                // Before the menu opens: the items it is measured with.
                FillMenu();
            }
            else
            {
                _owner.RequestPage(_item.Start);
            }
            base.OnClick();
        }

        private void FillMenu()
        {
            // pagination.rs: menu.min_w(55).max_h(240).scrollable(true).
            if (_menu.FlyoutPresenterTheme is null && this.TryFindResource("UIKitPaginationMenu", ActualThemeVariant, out var theme))
            {
                _menu.FlyoutPresenterTheme = theme as ControlTheme;
            }
            _menu.Items.Clear();
            var (start, end) = EllipsisMenuPages(_item.Start, _item.End, _current);
            for (var page = start; page < end; page++)
            {
                var target = page;
                var entry = new MenuItem
                {
                    Header = page.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ToggleType = MenuItemToggleType.CheckBox,
                    IsChecked = page == _current,
                };
                entry.Click += (_, _) => _owner.RequestPage(target);
                _menu.Items.Add(entry);
            }
        }
    }
}
