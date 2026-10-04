using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>GPUI's IndexPath: a row of a section.</summary>
/// <param name="Section">The section, counting the empty ones.</param>
/// <param name="Row">The row within the section.</param>
public readonly record struct IndexPath(int Section, int Row);

/// <summary>
/// A section of a <see cref="ListView"/>: its items, with a header above and a
/// footer below (GPUI's sections_count, items_count, render_section_header and
/// render_section_footer). A section without items is not shown.
/// </summary>
public class ListSection
{
    /// <summary>The header's content, or null for none.</summary>
    public object? Header { get; set; }

    /// <summary>The footer's content, or null for none.</summary>
    public object? Footer { get; set; }

    /// <summary>The section's items.</summary>
    [Content]
    public IList Items { get; set; } = new AvaloniaList<object?>();
}

/// <summary>The row a <see cref="ListView"/> event is about (GPUI's ListEvent).</summary>
public class ListEventArgs(RoutedEvent routedEvent, IndexPath? index, object? item, bool isSecondary = false) : RoutedEventArgs(routedEvent)
{
    /// <summary>The row, or null when none is selected.</summary>
    public IndexPath? Index { get; } = index;

    /// <summary>The row's item.</summary>
    public object? Item { get; } = item;

    /// <summary>Whether a confirm used the secondary modifier (Cmd on macOS, Ctrl elsewhere).</summary>
    public bool IsSecondary { get; } = isSecondary;
}

/// <summary>
/// GPUI Kit's List (list/list.rs, delegate.rs): a virtualized list of
/// <see cref="ListItem"/> rows, optionally in sections with headers and
/// footers, with a search field, the loading skeleton and an empty view.
/// <para>
/// A click or Enter selects and confirms a row (<see cref="Confirmed"/>,
/// secondary with Cmd/Ctrl), Up and Down move the selection and wrap at the
/// ends, Esc clears it and raises <see cref="Cancelled"/>. A right press marks
/// a row (<see cref="RightClickedIndex"/>) without selecting it. Scrolling near
/// the end raises <see cref="LoadMore"/> while <see cref="HasMore"/>.
/// </para>
/// <para>
/// Items come from <see cref="ItemsSource"/> (or <see cref="Items"/>). When
/// they are <see cref="ListSection"/>s, or <see cref="SectionItemsSelector"/>
/// gives each one's items, each is a section.
/// </para>
/// </summary>
[TemplatePart("PART_Rows", typeof(Control))]
[TemplatePart("PART_SearchBox", typeof(TextBox))]
[PseudoClasses(":empty", ":loading")]
public class ListView : TemplatedControl, IRowsOwner
{
    /// <summary>The items, or the sections.</summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ListView, IEnumerable?>(nameof(ItemsSource));

    /// <summary>The template for an item's content.</summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<ListView, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>
    /// A section's items, which makes every element of the source a section
    /// (GPUI's items_count per section). <see cref="ListSection"/>s need none.
    /// </summary>
    public static readonly StyledProperty<Func<object?, IEnumerable?>?> SectionItemsSelectorProperty =
        AvaloniaProperty.Register<ListView, Func<object?, IEnumerable?>?>(nameof(SectionItemsSelector));

    /// <summary>The template for a section's header, given the section (GPUI's render_section_header).</summary>
    public static readonly StyledProperty<IDataTemplate?> SectionHeaderTemplateProperty =
        AvaloniaProperty.Register<ListView, IDataTemplate?>(nameof(SectionHeaderTemplate));

    /// <summary>The template for a section's footer, given the section (GPUI's render_section_footer).</summary>
    public static readonly StyledProperty<IDataTemplate?> SectionFooterTemplateProperty =
        AvaloniaProperty.Register<ListView, IDataTemplate?>(nameof(SectionFooterTemplate));

    /// <summary>Whether an item's row is disabled (GPUI's ListItem::disabled).</summary>
    public static readonly StyledProperty<Func<object?, bool>?> DisabledSelectorProperty =
        AvaloniaProperty.Register<ListView, Func<object?, bool>?>(nameof(DisabledSelector));

    /// <summary>The selected row.</summary>
    public static readonly DirectProperty<ListView, IndexPath?> SelectedIndexProperty =
        AvaloniaProperty.RegisterDirect<ListView, IndexPath?>(nameof(SelectedIndex), o => o.SelectedIndex, (o, v) => o.SelectedIndex = v, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The selected row's item.</summary>
    public static readonly DirectProperty<ListView, object?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<ListView, object?>(nameof(SelectedItem), o => o.SelectedItem, (o, v) => o.SelectedItem = v, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The right-clicked row (GPUI's right_clicked_index).</summary>
    public static readonly DirectProperty<ListView, IndexPath?> RightClickedIndexProperty =
        AvaloniaProperty.RegisterDirect<ListView, IndexPath?>(nameof(RightClickedIndex), o => o.RightClickedIndex);

    /// <summary>Whether rows can be selected (GPUI's selectable, true by default).</summary>
    public static readonly StyledProperty<bool> IsSelectableProperty =
        AvaloniaProperty.Register<ListView, bool>(nameof(IsSelectable), true);

    /// <summary>Whether the list shows a search field above the rows (GPUI's searchable).</summary>
    public static readonly StyledProperty<bool> IsSearchableProperty =
        AvaloniaProperty.Register<ListView, bool>(nameof(IsSearchable));

    /// <summary>The search field's text.</summary>
    public static readonly StyledProperty<string?> SearchTextProperty =
        AvaloniaProperty.Register<ListView, string?>(nameof(SearchText), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The search field's placeholder (GPUI's search_placeholder).</summary>
    public static readonly StyledProperty<string?> SearchPlaceholderProperty =
        AvaloniaProperty.Register<ListView, string?>(nameof(SearchPlaceholder), "Search...");

    /// <summary>
    /// Whether an item matches the trimmed query (GPUI's perform_search). By
    /// default, the item's text contains the query, ignoring case.
    /// </summary>
    public static readonly StyledProperty<Func<object?, string, bool>?> SearchFilterProperty =
        AvaloniaProperty.Register<ListView, Func<object?, string, bool>?>(nameof(SearchFilter));

    /// <summary>Whether the list shows skeleton rows instead of its rows (GPUI's loading).</summary>
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<ListView, bool>(nameof(IsLoading));

    /// <summary>What an empty list shows (GPUI's render_empty); the Inbox icon by default.</summary>
    public static readonly StyledProperty<object?> EmptyContentProperty =
        AvaloniaProperty.Register<ListView, object?>(nameof(EmptyContent));

    /// <summary>Whether there is more to load at the end (GPUI's has_more).</summary>
    public static readonly StyledProperty<bool> HasMoreProperty =
        AvaloniaProperty.Register<ListView, bool>(nameof(HasMore));

    /// <summary>How many rows before the end <see cref="LoadMore"/> is raised (GPUI's load_more_threshold, 20).</summary>
    public static readonly StyledProperty<int> LoadMoreThresholdProperty =
        AvaloniaProperty.Register<ListView, int>(nameof(LoadMoreThreshold), 20);

    /// <summary>
    /// Set when <see cref="LoadMore"/> is raised; the app clears it once the
    /// items are added. No <see cref="LoadMore"/> is raised while it is set.
    /// </summary>
    public static readonly StyledProperty<bool> IsLoadingMoreProperty =
        AvaloniaProperty.Register<ListView, bool>(nameof(IsLoadingMore), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Raised when the selection changes.</summary>
    public static readonly RoutedEvent<ListEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<ListView, ListEventArgs>(nameof(SelectionChanged), RoutingStrategies.Bubble);

    /// <summary>Raised on a click on a row or Enter (GPUI's ListEvent::Confirm).</summary>
    public static readonly RoutedEvent<ListEventArgs> ConfirmedEvent =
        RoutedEvent.Register<ListView, ListEventArgs>(nameof(Confirmed), RoutingStrategies.Bubble);

    /// <summary>Raised on Esc, after the selection is cleared (GPUI's ListEvent::Cancel).</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CancelledEvent =
        RoutedEvent.Register<ListView, RoutedEventArgs>(nameof(Cancelled), RoutingStrategies.Bubble);

    /// <summary>Raised when the rows in view come within <see cref="LoadMoreThreshold"/> of the end (GPUI's load_more).</summary>
    public static readonly RoutedEvent<RoutedEventArgs> LoadMoreEvent =
        RoutedEvent.Register<ListView, RoutedEventArgs>(nameof(LoadMore), RoutingStrategies.Bubble);

    private static readonly object s_itemKey = new();
    private static readonly object s_headerKey = new();
    private static readonly object s_footerKey = new();

    private readonly List<INotifyCollectionChanged> _observed = [];
    private List<ListRow> _rows = [];
    private Dictionary<IndexPath, int> _rowOfPath = [];
    private int _itemsCount;
    private ListRows? _rowsControl;
    private TextBox? _searchBox;
    private IndexPath? _selectedIndex;
    private object? _selectedItem;
    private IndexPath? _rightClickedIndex;
    private object? _pendingItem;
    private string _lastQuery = "";
    private ListItem? _pressed;
    private TopLevel? _outsidePressRoot;
    private (IndexPath Index, ScrollStrategy Strategy, RowScrollMode Mode)? _deferredScroll;
    private bool _loadMorePosted;

    static ListView()
    {
        FocusableProperty.OverrideDefaultValue<ListView>(true);
    }

    /// <summary>Creates a list.</summary>
    public ListView()
    {
        Items.CollectionChanged += (_, _) =>
        {
            if (ItemsSource is null)
            {
                Rebuild();
            }
        };
        // Tunnels so the keys also work while the search field has the focus.
        AddHandler(KeyDownEvent, OnKeyDownTunnel, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc cref="ItemsSourceProperty"/>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>The items (or sections) when <see cref="ItemsSource"/> is not set.</summary>
    [Content]
    public AvaloniaList<object?> Items { get; } = [];

    /// <inheritdoc cref="ItemTemplateProperty"/>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <inheritdoc cref="SectionItemsSelectorProperty"/>
    public Func<object?, IEnumerable?>? SectionItemsSelector
    {
        get => GetValue(SectionItemsSelectorProperty);
        set => SetValue(SectionItemsSelectorProperty, value);
    }

    /// <inheritdoc cref="SectionHeaderTemplateProperty"/>
    public IDataTemplate? SectionHeaderTemplate
    {
        get => GetValue(SectionHeaderTemplateProperty);
        set => SetValue(SectionHeaderTemplateProperty, value);
    }

    /// <inheritdoc cref="SectionFooterTemplateProperty"/>
    public IDataTemplate? SectionFooterTemplate
    {
        get => GetValue(SectionFooterTemplateProperty);
        set => SetValue(SectionFooterTemplateProperty, value);
    }

    /// <inheritdoc cref="DisabledSelectorProperty"/>
    public Func<object?, bool>? DisabledSelector
    {
        get => GetValue(DisabledSelectorProperty);
        set => SetValue(DisabledSelectorProperty, value);
    }

    /// <inheritdoc cref="SelectedIndexProperty"/>
    public IndexPath? SelectedIndex
    {
        get => _selectedIndex;
        set => Select(value, scroll: false);
    }

    /// <inheritdoc cref="SelectedItemProperty"/>
    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            var path = value is null ? null : PathOf(value);
            // An item set before the rows exist (XAML sets attributes before content) waits for them.
            _pendingItem = path is null ? value : null;
            Select(path, scroll: false);
        }
    }

    /// <inheritdoc cref="RightClickedIndexProperty"/>
    public IndexPath? RightClickedIndex
    {
        get => _rightClickedIndex;
        private set => SetAndRaise(RightClickedIndexProperty, ref _rightClickedIndex, value);
    }

    /// <inheritdoc cref="IsSelectableProperty"/>
    public bool IsSelectable
    {
        get => GetValue(IsSelectableProperty);
        set => SetValue(IsSelectableProperty, value);
    }

    /// <inheritdoc cref="IsSearchableProperty"/>
    public bool IsSearchable
    {
        get => GetValue(IsSearchableProperty);
        set => SetValue(IsSearchableProperty, value);
    }

    /// <inheritdoc cref="SearchTextProperty"/>
    public string? SearchText
    {
        get => GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <inheritdoc cref="SearchPlaceholderProperty"/>
    public string? SearchPlaceholder
    {
        get => GetValue(SearchPlaceholderProperty);
        set => SetValue(SearchPlaceholderProperty, value);
    }

    /// <inheritdoc cref="SearchFilterProperty"/>
    public Func<object?, string, bool>? SearchFilter
    {
        get => GetValue(SearchFilterProperty);
        set => SetValue(SearchFilterProperty, value);
    }

    /// <inheritdoc cref="IsLoadingProperty"/>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <inheritdoc cref="EmptyContentProperty"/>
    public object? EmptyContent
    {
        get => GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    /// <inheritdoc cref="HasMoreProperty"/>
    public bool HasMore
    {
        get => GetValue(HasMoreProperty);
        set => SetValue(HasMoreProperty, value);
    }

    /// <inheritdoc cref="LoadMoreThresholdProperty"/>
    public int LoadMoreThreshold
    {
        get => GetValue(LoadMoreThresholdProperty);
        set => SetValue(LoadMoreThresholdProperty, value);
    }

    /// <inheritdoc cref="IsLoadingMoreProperty"/>
    public bool IsLoadingMore
    {
        get => GetValue(IsLoadingMoreProperty);
        set => SetValue(IsLoadingMoreProperty, value);
    }

    /// <inheritdoc cref="SelectionChangedEvent"/>
    public event EventHandler<ListEventArgs>? SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    /// <inheritdoc cref="ConfirmedEvent"/>
    public event EventHandler<ListEventArgs>? Confirmed
    {
        add => AddHandler(ConfirmedEvent, value);
        remove => RemoveHandler(ConfirmedEvent, value);
    }

    /// <inheritdoc cref="CancelledEvent"/>
    public event EventHandler<RoutedEventArgs>? Cancelled
    {
        add => AddHandler(CancelledEvent, value);
        remove => RemoveHandler(CancelledEvent, value);
    }

    /// <inheritdoc cref="LoadMoreEvent"/>
    public event EventHandler<RoutedEventArgs>? LoadMore
    {
        add => AddHandler(LoadMoreEvent, value);
        remove => RemoveHandler(LoadMoreEvent, value);
    }

    /// <summary>The number of item rows shown (after the search).</summary>
    public int ItemCount => _itemsCount;

    /// <summary>The item of a row shown, or null.</summary>
    public object? ItemAt(IndexPath index) =>
        _rowOfPath.TryGetValue(index, out var row) ? _rows[row].Value : null;

    /// <summary>
    /// Scrolls to a row (GPUI's scroll_to_item): Center centers it, any other
    /// strategy scrolls a row out of view to the nearer edge. The first row
    /// scrolls to the top. Before the first layout, it waits for one.
    /// </summary>
    public void ScrollToItem(IndexPath index, ScrollStrategy strategy = ScrollStrategy.Top)
    {
        // list.rs: the first item scrolls to the very top, above any header.
        _deferredScroll = index == default
            ? (index, ScrollStrategy.Top, RowScrollMode.Strict)
            : (index, strategy, RowScrollMode.VirtualList);
        ApplyDeferredScroll();
    }

    /// <summary>Scrolls the selected row into view (GPUI's scroll_to_selected_item).</summary>
    public void ScrollToSelectedItem()
    {
        if (_selectedIndex is { } index)
        {
            _deferredScroll = (index, ScrollStrategy.Top, RowScrollMode.VirtualList);
            ApplyDeferredScroll();
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_rowsControl is not null)
        {
            _rowsControl.Owner = null;
            _rowsControl.RowsInViewChanged -= OnRowsInViewChanged;
            _rowsControl.ItemsSource = null;
        }
        base.OnApplyTemplate(e);
        _rowsControl = e.NameScope.Find<ListRows>("PART_Rows");
        _searchBox = e.NameScope.Find<TextBox>("PART_SearchBox");
        if (_rowsControl is not null)
        {
            _rowsControl.Owner = this;
            _rowsControl.RowsInViewChanged += OnRowsInViewChanged;
        }
        Rebuild();
        ApplyDeferredScroll();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty || change.Property == SectionItemsSelectorProperty ||
            change.Property == SectionHeaderTemplateProperty || change.Property == SectionFooterTemplateProperty ||
            change.Property == SearchFilterProperty || change.Property == IsSearchableProperty)
        {
            Rebuild();
        }
        else if (change.Property == SearchTextProperty)
        {
            OnSearchTextChanged();
        }
        else if (change.Property == ItemTemplateProperty || change.Property == DisabledSelectorProperty)
        {
            RefreshRows();
        }
        else if (change.Property == IsLoadingProperty)
        {
            UpdatePseudoClasses();
        }
    }

    /// <inheritdoc />
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        // list.rs: a searchable list's focus is its search field's.
        if (e.Source == this && IsSearchable && e.NavigationMethod != NavigationMethod.Pointer)
        {
            _searchBox?.Focus(e.NavigationMethod, e.KeyModifiers);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Source is Visual source && _searchBox is not null && (source == _searchBox || _searchBox.IsVisualAncestorOf(source)))
        {
            return;
        }
        var properties = e.GetCurrentPoint(this).Properties;
        var container = ContainerAt(e.Source);
        if (properties.IsRightButtonPressed)
        {
            // list.rs: a right press marks the row without selecting it.
            if (IsSelectable && container is not null && RowOf(container) is { Kind: ListRowKind.Item } row)
            {
                SetRightClicked(row.Path);
            }
            return;
        }
        if (properties.IsLeftButtonPressed)
        {
            _pressed = container;
            Focus(NavigationMethod.Pointer);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var pressed = _pressed;
        _pressed = null;
        if (e.InitialPressMouseButton != MouseButton.Left || pressed is null || !IsSelectable ||
            !new Rect(pressed.Bounds.Size).Contains(e.GetPosition(pressed)) || RowOf(pressed) is not { Kind: ListRowKind.Item } row)
        {
            return;
        }
        // list.rs: a click clears the right-clicked mark, selects the row and confirms it.
        SetRightClicked(null);
        Select(row.Path, scroll: false);
        RaiseConfirm(IsSecondary(e.KeyModifiers));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        WatchOutsidePresses(false);
        base.OnDetachedFromVisualTree(e);
    }

    private void OnKeyDownTunnel(object? sender, KeyEventArgs e)
    {
        // list.rs binds its actions only while it is not loading.
        if (IsLoading)
        {
            return;
        }
        switch (e.Key)
        {
            case Key.Up when e.KeyModifiers == KeyModifiers.None:
                SelectPrevious();
                e.Handled = true;
                break;
            case Key.Down when e.KeyModifiers == KeyModifiers.None:
                SelectNext();
                e.Handled = true;
                break;
            case Key.Enter when e.KeyModifiers == KeyModifiers.None || IsSecondary(e.KeyModifiers):
                Confirm(IsSecondary(e.KeyModifiers));
                e.Handled = true;
                break;
            case Key.Escape:
                // list.rs: Cancel propagates, so Esc also reaches the parents.
                Cancel();
                break;
        }
    }

    private bool IsSecondary(KeyModifiers modifiers)
    {
        // GPUI's "secondary" modifier: Cmd on macOS, Ctrl elsewhere.
        var command = Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers
                      ?? (OperatingSystem.IsMacOS() ? KeyModifiers.Meta : KeyModifiers.Control);
        return command != KeyModifiers.None && modifiers.HasFlag(command);
    }

    private void SelectPrevious()
    {
        if (_rows.Count == 0 || !IsSelectable)
        {
            return;
        }
        // cache.rs prev: the row before, skipping headers and footers; the last row before the first.
        var at = _selectedIndex is { } s && _rowOfPath.TryGetValue(s, out var r) ? r : -1;
        var previous = at >= 0 ? LastItemRow(at - 1) : -1;
        SelectByKey(previous >= 0 ? previous : LastItemRow(_rows.Count - 1));
    }

    private void SelectNext()
    {
        if (_rows.Count == 0 || !IsSelectable)
        {
            return;
        }
        // cache.rs next: the row after, skipping headers and footers; the first row after the last.
        var at = _selectedIndex is { } s && _rowOfPath.TryGetValue(s, out var r) ? r : -1;
        var next = at >= 0 ? FirstItemRow(at + 1) : -1;
        SelectByKey(next >= 0 ? next : FirstItemRow(0));
    }

    private void SelectByKey(int row)
    {
        if (row < 0)
        {
            return;
        }
        Select(_rows[row].Path, scroll: true);
    }

    private int FirstItemRow(int from)
    {
        for (var i = Math.Max(0, from); i < _rows.Count; i++)
        {
            if (_rows[i].Kind == ListRowKind.Item)
            {
                return i;
            }
        }
        return -1;
    }

    private int LastItemRow(int from)
    {
        for (var i = Math.Min(from, _rows.Count - 1); i >= 0; i--)
        {
            if (_rows[i].Kind == ListRowKind.Item)
            {
                return i;
            }
        }
        return -1;
    }

    private void Confirm(bool secondary)
    {
        // list.rs on_action_confirm: nothing without rows or a selection.
        if (_rows.Count == 0 || _selectedIndex is null)
        {
            return;
        }
        RaiseConfirm(secondary);
    }

    private void RaiseConfirm(bool secondary) =>
        RaiseEvent(new ListEventArgs(ConfirmedEvent, _selectedIndex, _selectedItem, secondary));

    private void Cancel()
    {
        // list.rs on_action_cancel: clears the selection (reset_on_cancel) and emits Cancel.
        if (IsSelectable)
        {
            _pendingItem = null;
            Select(null, scroll: false);
        }
        RaiseEvent(new RoutedEventArgs(CancelledEvent));
    }

    private void Select(IndexPath? index, bool scroll)
    {
        if (index is not null)
        {
            _pendingItem = null;
        }
        var item = index is { } path && _rowOfPath.TryGetValue(path, out var row) ? _rows[row].Value : null;
        if (index == _selectedIndex && Equals(item, _selectedItem))
        {
            if (scroll)
            {
                ScrollToSelectedItem();
            }
            return;
        }
        var oldIndex = _selectedIndex;
        var oldItem = _selectedItem;
        _selectedIndex = index;
        _selectedItem = item;
        RaisePropertyChanged(SelectedIndexProperty, oldIndex, index);
        RaisePropertyChanged(SelectedItemProperty, oldItem, item);
        UpdateRealizedRows();
        if (scroll)
        {
            ScrollToSelectedItem();
        }
        RaiseEvent(new ListEventArgs(SelectionChangedEvent, index, item));
    }

    private void SetRightClicked(IndexPath? index)
    {
        if (RightClickedIndex == index)
        {
            return;
        }
        RightClickedIndex = index;
        UpdateRealizedRows();
        // list.rs: a press outside the list clears the mark (on_mouse_down_out).
        WatchOutsidePresses(index is not null);
    }

    private void WatchOutsidePresses(bool watch)
    {
        _outsidePressRoot?.RemoveHandler(PointerPressedEvent, OnAnyPointerPressed);
        _outsidePressRoot = watch ? TopLevel.GetTopLevel(this) : null;
        _outsidePressRoot?.AddHandler(PointerPressedEvent, OnAnyPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void OnAnyPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Visual source || !this.IsVisualAncestorOf(source) && source != this)
        {
            SetRightClicked(null);
        }
    }

    private void OnSearchTextChanged()
    {
        if (!IsSearchable)
        {
            return;
        }
        var query = SearchText?.Trim() ?? "";
        if (query == _lastQuery)
        {
            return;
        }
        _lastQuery = query;
        // list.rs start_search: the first row is selected when the list had rows.
        var hadRows = _rows.Count > 0;
        Rebuild();
        if (IsSelectable)
        {
            var first = FirstItemRow(0);
            Select(hadRows && first >= 0 ? _rows[first].Path : null, scroll: false);
        }
        if (_rows.Count > 0)
        {
            _deferredScroll = (_rows[0].Path, ScrollStrategy.Top, RowScrollMode.Strict);
            ApplyDeferredScroll();
        }
    }

    private void OnRowsInViewChanged(object? sender, EventArgs e)
    {
        // list.rs load_more_if_need: the rows in view reach `threshold` rows before the end.
        if (_loadMorePosted || !HasMore || IsLoadingMore || IsLoading || _rows.Count == 0 || _rowsControl is null)
        {
            return;
        }
        if (_rowsControl.VisibleEnd() >= Math.Max(0, _rows.Count - LoadMoreThreshold))
        {
            _loadMorePosted = true;
            SetCurrentValue(IsLoadingMoreProperty, true);
            // GPUI loads more in a task, after the frame.
            Dispatcher.UIThread.Post(() =>
            {
                _loadMorePosted = false;
                RaiseEvent(new RoutedEventArgs(LoadMoreEvent));
            });
        }
    }

    private void ApplyDeferredScroll()
    {
        if (_deferredScroll is not { } request || _rowsControl is null || !_rowOfPath.TryGetValue(request.Index, out var row))
        {
            return;
        }
        _deferredScroll = null;
        _rowsControl.ScrollToRow(request.Index == default ? 0 : row, request.Strategy, request.Mode);
    }

    private IndexPath? PathOf(object item)
    {
        foreach (var row in _rows)
        {
            if (row.Kind == ListRowKind.Item && Equals(row.Value, item))
            {
                return row.Path;
            }
        }
        return null;
    }

    private ListItem? ContainerAt(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != this; visual = visual.GetVisualParent())
        {
            if (visual is ListItem item && _rowsControl?.IndexFromContainer(item) >= 0)
            {
                return item;
            }
        }
        return null;
    }

    private ListRow? RowOf(Control container) =>
        _rowsControl?.IndexFromContainer(container) is int i and >= 0 && i < _rows.Count ? _rows[i] : null;

    private void Rebuild()
    {
        foreach (var observed in _observed)
        {
            observed.CollectionChanged -= OnSourceChanged;
        }
        _observed.Clear();

        var rows = new List<ListRow>();
        var paths = new Dictionary<IndexPath, int>();
        var count = 0;
        var query = IsSearchable ? SearchText?.Trim() ?? "" : "";
        _lastQuery = query;
        var filter = SearchFilter ?? DefaultFilter;
        IEnumerable source = ItemsSource ?? Items;
        Observe(source);

        void AddSection(int section, object? owner, IEnumerable? items)
        {
            if (items is null)
            {
                return;
            }
            Observe(items);
            var matched = new List<object?>();
            foreach (var item in items)
            {
                if (query.Length == 0 || filter(item, query))
                {
                    matched.Add(item);
                }
            }
            // cache.rs: a section without rows is skipped with its header and footer.
            if (matched.Count == 0)
            {
                return;
            }
            if (SectionHeaderTemplate is not null || owner is ListSection { Header: not null })
            {
                rows.Add(new ListRow(ListRowKind.Header, new IndexPath(section, 0), owner));
            }
            for (var i = 0; i < matched.Count; i++)
            {
                paths[new IndexPath(section, i)] = rows.Count;
                rows.Add(new ListRow(ListRowKind.Item, new IndexPath(section, i), matched[i]));
            }
            if (SectionFooterTemplate is not null || owner is ListSection { Footer: not null })
            {
                rows.Add(new ListRow(ListRowKind.Footer, new IndexPath(section, 0), owner));
            }
            count += matched.Count;
        }

        var selector = SectionItemsSelector;
        var sectioned = selector is not null || source.Cast<object?>().Any(x => x is ListSection);
        if (sectioned)
        {
            var section = 0;
            foreach (var element in source)
            {
                AddSection(section++, element, selector?.Invoke(element) ?? (element as ListSection)?.Items);
            }
        }
        else
        {
            AddSection(0, null, source);
        }

        _rows = rows;
        _rowOfPath = paths;
        _itemsCount = count;
        if (_rowsControl is not null)
        {
            _rowsControl.ItemsSource = rows;
        }
        // The selection is a path (GPUI's IndexPath); it is dropped when the row is gone.
        if (_pendingItem is not null && PathOf(_pendingItem) is { } pending)
        {
            _pendingItem = null;
            Select(pending, scroll: false);
        }
        else if (_selectedIndex is { } selected)
        {
            var keep = paths.ContainsKey(selected) ? selected : (IndexPath?)null;
            _selectedIndex = null;
            Select(keep, scroll: false);
        }
        if (RightClickedIndex is { } marked && !paths.ContainsKey(marked))
        {
            SetRightClicked(null);
        }
        UpdatePseudoClasses();
        ApplyDeferredScroll();
    }

    private static bool DefaultFilter(object? item, string query) =>
        item?.ToString()?.Contains(query, StringComparison.OrdinalIgnoreCase) == true;

    private void Observe(IEnumerable source)
    {
        if (source is INotifyCollectionChanged observable && !_observed.Contains(observable))
        {
            observable.CollectionChanged += OnSourceChanged;
            _observed.Add(observable);
        }
    }

    private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":loading", IsLoading);
        PseudoClasses.Set(":empty", !IsLoading && _itemsCount == 0);
    }

    private void RefreshRows()
    {
        if (_rowsControl is null)
        {
            return;
        }
        foreach (var container in _rowsControl.GetRealizedContainers())
        {
            var index = _rowsControl.IndexFromContainer(container);
            if (index >= 0 && index < _rows.Count)
            {
                ((IRowsOwner)this).PrepareRow(container, _rows[index], index);
            }
        }
    }

    private void UpdateRealizedRows()
    {
        if (_rowsControl is null)
        {
            return;
        }
        foreach (var container in _rowsControl.GetRealizedContainers())
        {
            if (container is ListItem item && RowOf(item) is { Kind: ListRowKind.Item } row)
            {
                item.IsSelected = row.Path == _selectedIndex;
                item.IsSecondarySelected = row.Path == RightClickedIndex;
            }
        }
    }

    private ControlTheme? FindTheme(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) ? value as ControlTheme : null;

    object IRowsOwner.RecycleKey(object? row) => (row as ListRow)?.Kind switch
    {
        ListRowKind.Header => s_headerKey,
        ListRowKind.Footer => s_footerKey,
        _ => s_itemKey,
    };

    Control IRowsOwner.CreateRow(object recycleKey)
    {
        if (recycleKey == s_itemKey)
        {
            return new ListItem();
        }
        return new ContentPresenter();
    }

    void IRowsOwner.PrepareRow(Control container, object? row, int index)
    {
        if (row is not ListRow entry)
        {
            return;
        }
        switch (container)
        {
            case ListItem item:
                // The app's item, for bindings in its row styles (ItemsControl set the row record).
                item.DataContext = entry.Value;
                item.Content = entry.Value;
                item.ContentTemplate = ItemTemplate;
                item.IsSelected = entry.Path == _selectedIndex;
                item.IsSecondarySelected = entry.Path == RightClickedIndex;
                item.IsEnabled = DisabledSelector?.Invoke(entry.Value) != true;
                break;
            case ContentPresenter presenter:
                presenter.DataContext = entry.Value;
                var template = entry.Kind == ListRowKind.Header ? SectionHeaderTemplate : SectionFooterTemplate;
                // A ListSection's own header and footer take the story's look from the
                // theme; a template's are the app's, as GPUI's render_section_header.
                presenter.Theme = template is null
                    ? FindTheme(entry.Kind == ListRowKind.Header ? "UIKitListSectionHeader" : "UIKitListSectionFooter")
                    : null;
                presenter.Content = template is not null
                    ? entry.Value
                    : entry.Kind == ListRowKind.Header ? (entry.Value as ListSection)?.Header : (entry.Value as ListSection)?.Footer;
                presenter.ContentTemplate = template;
                break;
        }
    }

    void IRowsOwner.ClearRow(Control container)
    {
        switch (container)
        {
            case ListItem item:
                // The template first: the old template must not see the cleared content.
                item.ClearValue(ContentControl.ContentTemplateProperty);
                item.ClearValue(ContentControl.ContentProperty);
                break;
            case ContentPresenter presenter:
                presenter.ClearValue(ContentPresenter.ContentTemplateProperty);
                presenter.ClearValue(ContentPresenter.ContentProperty);
                break;
        }
    }
}

internal enum ListRowKind
{
    Header,
    Item,
    Footer,
}

/// <summary>A flattened row of a ListView (GPUI's RowEntry): a header, an item or a footer.</summary>
internal sealed record ListRow(ListRowKind Kind, IndexPath Path, object? Value);
