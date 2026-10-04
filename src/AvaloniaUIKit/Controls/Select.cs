using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Select (select.rs) and Combobox (combobox.rs), both built on the
/// searchable list (searchable_list/): a trigger in the input frame and a
/// dropdown of rows with an optional search field, sections, a footer and an
/// empty view. The trigger shows the selection's text (several joined by
/// ", "), the row under the cursor while the list is open (Select; the
/// <c>combobox</c> class keeps the committed selection, as Combobox does), a
/// <see cref="TriggerTemplate"/>, or the placeholder.
/// </summary>
/// <remarks>
/// Items are data: strings or objects whose text comes from
/// <see cref="TextSelector"/> (ToString by default); a <see cref="SelectGroup"/>
/// among them is a section. The rows show <see cref="ItemsControl.ItemTemplate"/>
/// or the text. A click or Enter on a row raises <see cref="SelectionChanging"/>
/// (GPUI's on_will_change), where the app may change or cancel the proposed
/// selection, then commits it; a single selection closes the popup, a multiple
/// one toggles the row and stays open. Every close drops the query and puts
/// the cursor back on the selection (select.rs clear_query_and_restore_cursor).
/// </remarks>
[TemplatePart("PART_Popup", typeof(Popup))]
[TemplatePart("PART_List", typeof(SelectList))]
[TemplatePart("PART_SearchBox", typeof(TextBox))]
[TemplatePart("PART_ClearButton", typeof(Button))]
[TemplatePart("PART_ClearSearchButton", typeof(Button))]
[PseudoClasses(":dropdownopen", ":pressed", ":clearable", ":has-icon", ":custom-trigger", ":query", ":searching", ":empty-view")]
public class Select : ItemsControl
{
    /// <summary>Single (the default) or multiple selection (GPUI's Combobox multiple(true)).</summary>
    public static readonly StyledProperty<SelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<Select, SelectionMode>(nameof(SelectionMode), SelectionMode.Single);

    /// <summary>The first selected item. A binding's validation error shows the invalid frame (<c>:error</c>).</summary>
    public static readonly DirectProperty<Select, object?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<Select, object?>(nameof(SelectedItem), o => o.SelectedItem, (o, v) => o.SelectedItem = v,
            defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

    /// <summary>The position of <see cref="SelectedItem"/> among the items, sections flattened; -1 for none.</summary>
    public static readonly DirectProperty<Select, int> SelectedIndexProperty =
        AvaloniaProperty.RegisterDirect<Select, int>(nameof(SelectedIndex), o => o.SelectedIndex, (o, v) => o.SelectedIndex = v,
            unsetValue: -1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The selected items, in the order they were selected.</summary>
    public static readonly DirectProperty<Select, IList?> SelectedItemsProperty =
        AvaloniaProperty.RegisterDirect<Select, IList?>(nameof(SelectedItems), o => o.SelectedItems, (o, v) => o.SelectedItems = v);

    /// <summary>Whether the dropdown is open.</summary>
    public static readonly DirectProperty<Select, bool> IsDropDownOpenProperty =
        AvaloniaProperty.RegisterDirect<Select, bool>(nameof(IsDropDownOpen), o => o.IsDropDownOpen, (o, v) => o.IsDropDownOpen = v,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Whether the dropdown has a search field above the rows (GPUI's searchable(true)).</summary>
    public static readonly StyledProperty<bool> IsSearchableProperty =
        AvaloniaProperty.Register<Select, bool>(nameof(IsSearchable));

    /// <summary>Whether a clear button replaces the caret while there is a selection (GPUI's cleanable(true)).</summary>
    public static readonly StyledProperty<bool> IsCleanableProperty =
        AvaloniaProperty.Register<Select, bool>(nameof(IsCleanable));

    /// <summary>The trigger's text while nothing is selected (GPUI's "Please select").</summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<Select, string?>(nameof(PlaceholderText), "Please select");

    /// <summary>The placeholder's color.</summary>
    public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.Register<Select, IBrush?>(nameof(PlaceholderForeground));

    /// <summary>The search field's placeholder (GPUI's "Search...").</summary>
    public static readonly StyledProperty<string?> SearchPlaceholderTextProperty =
        AvaloniaProperty.Register<Select, string?>(nameof(SearchPlaceholderText), "Search...");

    /// <summary>The query in the search field.</summary>
    public static readonly DirectProperty<Select, string> SearchTextProperty =
        AvaloniaProperty.RegisterDirect<Select, string>(nameof(SearchText), o => o.SearchText, (o, v) => o.SearchText = v,
            unsetValue: string.Empty, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Text before the selection's text in the trigger (GPUI's title_prefix).</summary>
    public static readonly StyledProperty<string?> TitlePrefixProperty =
        AvaloniaProperty.Register<Select, string?>(nameof(TitlePrefix));

    /// <summary>An icon in place of the caret, 12px (GPUI's icon()).</summary>
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<Select, Geometry?>(nameof(Icon));

    /// <summary>Content below the rows, under a separator (GPUI's footer()).</summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<Select, object?>(nameof(Footer));

    /// <summary>The template of <see cref="Footer"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> FooterTemplateProperty =
        AvaloniaProperty.Register<Select, IDataTemplate?>(nameof(FooterTemplate));

    /// <summary>What the dropdown shows when no row matches or there are none (GPUI's empty(); an inbox icon by default).</summary>
    public static readonly StyledProperty<object?> EmptyContentProperty =
        AvaloniaProperty.Register<Select, object?>(nameof(EmptyContent));

    /// <summary>
    /// The whole trigger's content (GPUI's render_trigger), applied to a
    /// <see cref="SelectTriggerContext"/>; the default title, caret and clear
    /// button are not shown.
    /// </summary>
    public static readonly StyledProperty<IDataTemplate?> TriggerTemplateProperty =
        AvaloniaProperty.Register<Select, IDataTemplate?>(nameof(TriggerTemplate));

    /// <summary>The dropdown's width; null for the trigger's (GPUI's menu_width).</summary>
    public static readonly StyledProperty<double?> MenuWidthProperty =
        AvaloniaProperty.Register<Select, double?>(nameof(MenuWidth));

    /// <summary>The rows' maximum height, 320 by default (GPUI's menu_max_h, 20rem).</summary>
    public static readonly StyledProperty<double> MaxDropDownHeightProperty =
        AvaloniaProperty.Register<Select, double>(nameof(MaxDropDownHeight), 320);

    /// <summary>
    /// Whether the trigger shows the row under the cursor while the list is
    /// open (GPUI's Select) rather than the committed selection (Combobox).
    /// Set by the theme: true, false with the <c>combobox</c> class.
    /// </summary>
    public static readonly StyledProperty<bool> TracksCursorProperty =
        AvaloniaProperty.Register<Select, bool>(nameof(TracksCursor));

    /// <summary>An item's text (GPUI's SearchableListItem::title); ToString by default.</summary>
    public static readonly StyledProperty<Func<object?, string>?> TextSelectorProperty =
        AvaloniaProperty.Register<Select, Func<object?, string>?>(nameof(TextSelector));

    /// <summary>
    /// Whether an item matches a query (GPUI's matches); by default its text
    /// contains the query, ignoring case.
    /// </summary>
    public static readonly StyledProperty<Func<object?, string, bool>?> FilterProperty =
        AvaloniaProperty.Register<Select, Func<object?, string, bool>?>(nameof(Filter));

    /// <summary>Whether an item can be chosen (GPUI's disabled / is_item_enabled); all can by default.</summary>
    public static readonly StyledProperty<Func<object?, bool>?> ItemEnabledSelectorProperty =
        AvaloniaProperty.Register<Select, Func<object?, bool>?>(nameof(ItemEnabledSelector));

    /// <summary>
    /// Fetches the items for a query instead of filtering the items (GPUI's
    /// perform_search returning a task); the empty query shows the items.
    /// </summary>
    public static readonly StyledProperty<Func<string?, CancellationToken, Task<IEnumerable<object>>>?> AsyncPopulatorProperty =
        AvaloniaProperty.Register<Select, Func<string?, CancellationToken, Task<IEnumerable<object>>>?>(nameof(AsyncPopulator));

    /// <summary>The trigger's text: the selection's (with the prefix), the cursor's row while open, or null for the placeholder.</summary>
    public static readonly DirectProperty<Select, string?> DisplayTitleProperty =
        AvaloniaProperty.RegisterDirect<Select, string?>(nameof(DisplayTitle), o => o.DisplayTitle);

    /// <summary>What <see cref="TriggerTemplate"/> is applied to.</summary>
    public static readonly DirectProperty<Select, SelectTriggerContext> TriggerContextProperty =
        AvaloniaProperty.RegisterDirect<Select, SelectTriggerContext>(nameof(TriggerContext), o => o.TriggerContext);

    /// <summary>
    /// Raised before a click or Enter on a row changes the selection (GPUI's
    /// on_will_change); handlers may edit <see cref="SelectionChangingEventArgs.Selection"/>
    /// or cancel. Programmatic changes and the clear button do not raise it.
    /// </summary>
    public static readonly RoutedEvent<SelectionChangingEventArgs> SelectionChangingEvent =
        RoutedEvent.Register<Select, SelectionChangingEventArgs>(nameof(SelectionChanging), RoutingStrategies.Bubble);

    /// <summary>Raised when the selection changed.</summary>
    public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<Select, SelectionChangedEventArgs>(nameof(SelectionChanged), RoutingStrategies.Bubble);

    // list.rs start_search: the search field shows its spinner until 100ms after a search.
    private static readonly TimeSpan SearchSettle = TimeSpan.FromMilliseconds(100);

    private readonly List<object?> _selection = [];
    private readonly List<Entry> _entries = [];
    private readonly SelectTriggerContext _triggerContext = new();
    private IList? _selectedItems;
    private object? _selectedItem;
    private int _selectedIndex = -1;
    private int _pendingIndex = -1;
    private bool _isDropDownOpen;
    private string _searchText = string.Empty;
    private string? _lastQuery;
    private string? _displayTitle;
    private int _cursor = -1;
    private int _syncing;
    private bool _clearPressed;
    private List<object?>? _populated;
    private CancellationTokenSource? _populating;
    private IDisposable? _searchSettle;
    private Popup? _popup;
    private SelectList? _list;
    private TextBox? _searchBox;
    private Button? _clearButton;
    private Button? _clearSearchButton;

    /// <summary>One line of the open list: a section header or a row.</summary>
    private readonly record struct Entry(object? Item, bool IsHeader);

    static Select()
    {
        FocusableProperty.OverrideDefaultValue<Select>(true);
        Refreshes(SelectionModeProperty, TitlePrefixProperty, TextSelectorProperty, TracksCursorProperty,
            ItemEnabledSelectorProperty, IsCleanableProperty, IconProperty, TriggerTemplateProperty, PlaceholderTextProperty);
        IsEnabledProperty.Changed.AddClassHandler<Select>((s, _) => s.UpdateState());
    }

    private static void Refreshes(params AvaloniaProperty[] properties)
    {
        foreach (var property in properties)
        {
            property.Changed.AddClassHandler<Select>((s, _) => s.UpdateState());
        }
    }

    /// <summary>Creates a select.</summary>
    public Select()
    {
        SelectedItems = new AvaloniaList<object?>();
        ItemsView.CollectionChanged += (_, _) => OnItemsChanged();
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPreviewPointerReleased, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc cref="SelectionChangingEvent"/>
    public event EventHandler<SelectionChangingEventArgs>? SelectionChanging
    {
        add => AddHandler(SelectionChangingEvent, value);
        remove => RemoveHandler(SelectionChangingEvent, value);
    }

    /// <inheritdoc cref="SelectionChangedEvent"/>
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    /// <summary>Raised when the dropdown opens.</summary>
    public event EventHandler? DropDownOpened;

    /// <summary>Raised when the dropdown closes (GPUI's DismissEvent).</summary>
    public event EventHandler? DropDownClosed;

    /// <inheritdoc cref="SelectionModeProperty"/>
    public SelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <inheritdoc cref="SelectedItemProperty"/>
    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_syncing == 0)
            {
                SetSelection(value is null ? [] : [value]);
            }
        }
    }

    /// <inheritdoc cref="SelectedIndexProperty"/>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_syncing > 0)
            {
                return;
            }
            if (value < 0)
            {
                _pendingIndex = -1;
                SetSelection([]);
            }
            else if (LeafAt(value, out var item))
            {
                _pendingIndex = -1;
                SetSelection([item]);
            }
            else
            {
                // Items declared after the index (XAML) arrive later.
                _pendingIndex = value;
            }
        }
    }

    /// <inheritdoc cref="SelectedItemsProperty"/>
    public IList? SelectedItems
    {
        get => _selectedItems;
        set
        {
            value ??= new AvaloniaList<object?>();
            if (ReferenceEquals(value, _selectedItems))
            {
                return;
            }
            if (_selectedItems is INotifyCollectionChanged previous)
            {
                previous.CollectionChanged -= OnSelectedItemsChanged;
            }
            var old = _selectedItems;
            _selectedItems = value;
            RaisePropertyChanged(SelectedItemsProperty, old, value);
            if (value is INotifyCollectionChanged next)
            {
                next.CollectionChanged += OnSelectedItemsChanged;
            }
            if (old is not null)
            {
                SetSelection(value.Cast<object?>());
            }
        }
    }

    /// <inheritdoc cref="IsDropDownOpenProperty"/>
    public bool IsDropDownOpen
    {
        get => _isDropDownOpen;
        set
        {
            if (value && !IsEffectivelyEnabled)
            {
                return;
            }
            if (SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value))
            {
                OnDropDownChanged(value);
            }
        }
    }

    /// <inheritdoc cref="IsSearchableProperty"/>
    public bool IsSearchable
    {
        get => GetValue(IsSearchableProperty);
        set => SetValue(IsSearchableProperty, value);
    }

    /// <inheritdoc cref="IsCleanableProperty"/>
    public bool IsCleanable
    {
        get => GetValue(IsCleanableProperty);
        set => SetValue(IsCleanableProperty, value);
    }

    /// <inheritdoc cref="PlaceholderTextProperty"/>
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }

    /// <inheritdoc cref="PlaceholderForegroundProperty"/>
    public IBrush? PlaceholderForeground
    {
        get => GetValue(PlaceholderForegroundProperty);
        set => SetValue(PlaceholderForegroundProperty, value);
    }

    /// <inheritdoc cref="SearchPlaceholderTextProperty"/>
    public string? SearchPlaceholderText
    {
        get => GetValue(SearchPlaceholderTextProperty);
        set => SetValue(SearchPlaceholderTextProperty, value);
    }

    /// <inheritdoc cref="SearchTextProperty"/>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetAndRaise(SearchTextProperty, ref _searchText, value ?? string.Empty))
            {
                OnQueryChanged();
            }
        }
    }

    /// <inheritdoc cref="TitlePrefixProperty"/>
    public string? TitlePrefix
    {
        get => GetValue(TitlePrefixProperty);
        set => SetValue(TitlePrefixProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public Geometry? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="FooterProperty"/>
    public object? Footer
    {
        get => GetValue(FooterProperty);
        set => SetValue(FooterProperty, value);
    }

    /// <inheritdoc cref="FooterTemplateProperty"/>
    public IDataTemplate? FooterTemplate
    {
        get => GetValue(FooterTemplateProperty);
        set => SetValue(FooterTemplateProperty, value);
    }

    /// <inheritdoc cref="EmptyContentProperty"/>
    public object? EmptyContent
    {
        get => GetValue(EmptyContentProperty);
        set => SetValue(EmptyContentProperty, value);
    }

    /// <inheritdoc cref="TriggerTemplateProperty"/>
    public IDataTemplate? TriggerTemplate
    {
        get => GetValue(TriggerTemplateProperty);
        set => SetValue(TriggerTemplateProperty, value);
    }

    /// <inheritdoc cref="MenuWidthProperty"/>
    public double? MenuWidth
    {
        get => GetValue(MenuWidthProperty);
        set => SetValue(MenuWidthProperty, value);
    }

    /// <inheritdoc cref="MaxDropDownHeightProperty"/>
    public double MaxDropDownHeight
    {
        get => GetValue(MaxDropDownHeightProperty);
        set => SetValue(MaxDropDownHeightProperty, value);
    }

    /// <inheritdoc cref="TracksCursorProperty"/>
    public bool TracksCursor
    {
        get => GetValue(TracksCursorProperty);
        set => SetValue(TracksCursorProperty, value);
    }

    /// <inheritdoc cref="TextSelectorProperty"/>
    public Func<object?, string>? TextSelector
    {
        get => GetValue(TextSelectorProperty);
        set => SetValue(TextSelectorProperty, value);
    }

    /// <inheritdoc cref="FilterProperty"/>
    public Func<object?, string, bool>? Filter
    {
        get => GetValue(FilterProperty);
        set => SetValue(FilterProperty, value);
    }

    /// <inheritdoc cref="ItemEnabledSelectorProperty"/>
    public Func<object?, bool>? ItemEnabledSelector
    {
        get => GetValue(ItemEnabledSelectorProperty);
        set => SetValue(ItemEnabledSelectorProperty, value);
    }

    /// <inheritdoc cref="AsyncPopulatorProperty"/>
    public Func<string?, CancellationToken, Task<IEnumerable<object>>>? AsyncPopulator
    {
        get => GetValue(AsyncPopulatorProperty);
        set => SetValue(AsyncPopulatorProperty, value);
    }

    /// <inheritdoc cref="DisplayTitleProperty"/>
    public string? DisplayTitle
    {
        get => _displayTitle;
        private set => SetAndRaise(DisplayTitleProperty, ref _displayTitle, value);
    }

    /// <inheritdoc cref="TriggerContextProperty"/>
    public SelectTriggerContext TriggerContext => _triggerContext;

    private bool IsMultiple => SelectionMode.HasFlag(SelectionMode.Multiple);

    /// <summary>Clears the selection (the clear button; GPUI's clear_selection).</summary>
    public void ClearSelection() => SetSelection([]);

    /// <summary>The text of an item: <see cref="TextSelector"/>, or ToString.</summary>
    public string TextOf(object? item) => TextSelector?.Invoke(item) ?? item?.ToString() ?? string.Empty;

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_popup is not null)
        {
            _popup.Opened -= OnPopupOpened;
            _popup.Closed -= OnPopupClosed;
        }
        if (_clearSearchButton is not null)
        {
            _clearSearchButton.Click -= OnClearSearch;
        }
        if (_list is not null)
        {
            _list.Owner = null;
        }
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _list = e.NameScope.Find<SelectList>("PART_List");
        _searchBox = e.NameScope.Find<TextBox>("PART_SearchBox");
        _clearButton = e.NameScope.Find<Button>("PART_ClearButton");
        _clearSearchButton = e.NameScope.Find<Button>("PART_ClearSearchButton");
        if (_popup is not null)
        {
            _popup.Opened += OnPopupOpened;
            _popup.Closed += OnPopupClosed;
        }
        if (_clearSearchButton is not null)
        {
            _clearSearchButton.Click += OnClearSearch;
        }
        if (_list is not null)
        {
            _list.Owner = this;
        }
        BuildView();
        if (!IsDropDownOpen)
        {
            _cursor = CommittedCursor();
        }
        UpdateState();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || e.Source is not Visual source || _popup?.IsInsidePopup(source) == true ||
            !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }
        if (IsDropDownOpen)
        {
            IsDropDownOpen = false;
            e.Handled = true;
        }
        else
        {
            PseudoClasses.Set(":pressed", true);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!e.Handled && e.Source is Visual source && _popup?.IsInsidePopup(source) != true &&
            PseudoClasses.Contains(":pressed"))
        {
            IsDropDownOpen = !IsDropDownOpen;
            e.Handled = true;
        }
        PseudoClasses.Set(":pressed", false);
        base.OnPointerReleased(e);
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PseudoClasses.Set(":pressed", false);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        IsDropDownOpen = false;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectionModeProperty && !IsMultiple && _selection.Count > 1)
        {
            SetSelection([_selection[0]]);
        }
        else if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled)
        {
            IsDropDownOpen = false;
        }
    }

    /// <summary>Whether the open list's line at <paramref name="index"/> is a section header.</summary>
    internal bool IsHeaderAt(int index) => index >= 0 && index < _entries.Count && _entries[index].IsHeader;

    /// <summary>Puts a row container into its line's state.</summary>
    internal void PrepareRow(SelectListItem row, int index)
    {
        if (index < 0 || index >= _entries.Count)
        {
            return;
        }
        var item = _entries[index].Item;
        row.IsCursor = index == _cursor;
        row.IsSelected = IndexOfValue(_selection, item) >= 0;
        row.IsEnabled = IsItemEnabled(item);
    }

    /// <summary>
    /// The user picks the row at <paramref name="index"/> (a click or Enter):
    /// the cursor moves there, <see cref="SelectionChanging"/> may edit the
    /// proposed selection, then a single select closes and a multiple one
    /// stays open (select.rs / combobox.rs on_confirm).
    /// </summary>
    internal void Commit(int index)
    {
        if (index < 0 || index >= _entries.Count || _entries[index].IsHeader)
        {
            return;
        }
        var item = _entries[index].Item;
        SetCursor(index, scroll: false);
        // GPUI confirms a disabled row too (the row only looks disabled); here it cannot be chosen.
        if (!IsItemEnabled(item))
        {
            return;
        }
        var proposed = new List<object?>(_selection);
        var added = new List<object?>();
        var removed = new List<object?>();
        if (IsMultiple)
        {
            // combobox.rs selection_changes: a click toggles the row.
            var at = IndexOfValue(proposed, item);
            if (at >= 0)
            {
                removed.Add(proposed[at]);
                proposed.RemoveAt(at);
            }
            else
            {
                added.Add(item);
                proposed.Add(item);
            }
        }
        else
        {
            removed.AddRange(_selection.Where(old => !Equals(old, item)));
            if (IndexOfValue(_selection, item) < 0)
            {
                added.Add(item);
            }
            proposed = [item];
        }
        var args = new SelectionChangingEventArgs(SelectionChangingEvent, removed, added, proposed);
        RaiseEvent(args);
        if (!args.Cancel)
        {
            SetSelection(args.Selection);
        }
        if (!IsMultiple)
        {
            IsDropDownOpen = false;
        }
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || !IsEffectivelyEnabled || e.KeyModifiers is not (KeyModifiers.None or KeyModifiers.Shift))
        {
            return;
        }
        // The trigger's keys, and the search field's and the list's while open;
        // keys inside the footer belong to its content.
        var fromPopup = e.Source is Visual v && _popup?.IsInsidePopup(v) == true;
        if (fromPopup ? !ReferenceEquals(e.Source, _searchBox) : !ReferenceEquals(e.Source, this))
        {
            return;
        }
        if (!IsDropDownOpen)
        {
            // gpui-base select.rs: ↑, ↓ and Enter open the closed select (the cursor stays).
            if (e.KeyModifiers == KeyModifiers.None && e.Key is Key.Up or Key.Down or Key.Enter)
            {
                IsDropDownOpen = true;
                e.Handled = true;
            }
            return;
        }
        switch (e.Key)
        {
            // list.rs: ↑ and ↓ move the cursor, wrapping; Enter confirms it; Escape closes.
            case Key.Down when e.KeyModifiers == KeyModifiers.None:
                MoveCursor(1);
                e.Handled = true;
                break;
            case Key.Up when e.KeyModifiers == KeyModifiers.None:
                MoveCursor(-1);
                e.Handled = true;
                break;
            case Key.Enter when e.KeyModifiers == KeyModifiers.None:
                if (_cursor >= 0)
                {
                    Commit(_cursor);
                }
                e.Handled = true;
                break;
            case Key.Escape when e.KeyModifiers == KeyModifiers.None:
                IsDropDownOpen = false;
                e.Handled = true;
                break;
            case Key.Tab:
                // Focus leaves the list: the popup closes (select.rs on_blur) and Tab moves on from the trigger.
                IsDropDownOpen = false;
                Focus();
                var direction = e.KeyModifiers == KeyModifiers.Shift ? NavigationDirection.Previous : NavigationDirection.Next;
                TopLevel.GetTopLevel(this)?.FocusManager?.TryMoveFocus(direction);
                e.Handled = true;
                break;
        }
    }

    // The clear button clears without focusing the trigger: GPUI's button takes the press itself.
    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (OnClearButton(e) && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _clearPressed = true;
            e.Handled = true;
        }
    }

    private void OnPreviewPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_clearPressed)
        {
            return;
        }
        _clearPressed = false;
        e.Handled = true;
        if (OnClearButton(e))
        {
            ClearSelection();
        }
    }

    private bool OnClearButton(PointerEventArgs e) =>
        _clearButton is { IsEffectivelyVisible: true } button && IsEffectivelyEnabled &&
        new Rect(button.Bounds.Size).Contains(e.GetPosition(button));

    private void OnClearSearch(object? sender, RoutedEventArgs e)
    {
        // input.rs: the clear button empties the field and keeps its focus.
        SearchText = string.Empty;
        _searchBox?.Focus();
    }

    private void OnPopupOpened(object? sender, EventArgs e)
    {
        if (IsSearchable)
        {
            _searchBox?.Focus();
        }
        ScrollCursorIntoView();
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        IsDropDownOpen = false;
        RefocusAfterClose();
    }

    private void OnDropDownChanged(bool open)
    {
        PseudoClasses.Set(":dropdownopen", open);
        if (open)
        {
            BuildView();
            _cursor = CommittedCursor();
            UpdateState();
            DropDownOpened?.Invoke(this, EventArgs.Empty);
            return;
        }
        // select.rs clear_query_and_restore_cursor: every close drops the query
        // and puts the cursor back on the committed selection.
        _populating?.Cancel();
        _populated = null;
        _lastQuery = null;
        if (_searchText.Length > 0)
        {
            SetAndRaise(SearchTextProperty, ref _searchText, string.Empty);
            PseudoClasses.Set(":query", false);
        }
        BuildView();
        _cursor = CommittedCursor();
        UpdateState();
        RefocusAfterClose();
        DropDownClosed?.Invoke(this, EventArgs.Empty);
    }

    // GPUI refocuses the trigger on every close, unless focus went elsewhere.
    private void RefocusAfterClose()
    {
        if (TopLevel.GetTopLevel(this) is not { } topLevel)
        {
            return;
        }
        var focused = topLevel.FocusManager?.GetFocusedElement();
        if (focused is null || (focused is Visual visual && (visual == this || _popup?.IsInsidePopup(visual) == true || TopLevel.GetTopLevel(visual) is null)))
        {
            if (!ReferenceEquals(focused, this) && IsEffectivelyEnabled)
            {
                Focus();
            }
        }
    }

    private void OnQueryChanged()
    {
        PseudoClasses.Set(":query", _searchText.Length > 0);
        var query = _searchText.Trim();
        if (query == (_lastQuery ?? string.Empty) && _lastQuery is not null)
        {
            return;
        }
        // list.rs start_search: the cursor goes to the first section's first row
        // when the list had rows, and the field shows its spinner a while.
        var hadRows = _entries.Any(e => !e.IsHeader);
        _lastQuery = query;
        SetSearching(true);
        if (AsyncPopulator is { } populate && query.Length > 0)
        {
            _ = PopulateAsync(populate, query, hadRows);
            return;
        }
        _populating?.Cancel();
        _populated = null;
        ShowQueryResults(hadRows);
        SettleSearch();
    }

    private async Task PopulateAsync(Func<string?, CancellationToken, Task<IEnumerable<object>>> populate, string query, bool hadRows)
    {
        _populating?.Cancel();
        var populating = _populating = new CancellationTokenSource();
        try
        {
            var items = await populate(query, populating.Token);
            if (populating.IsCancellationRequested)
            {
                return;
            }
            _populated = [.. items];
            ShowQueryResults(hadRows);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            if (ReferenceEquals(_populating, populating))
            {
                SettleSearch();
            }
        }
    }

    private void ShowQueryResults(bool hadRows)
    {
        BuildView();
        _cursor = hadRows ? FirstRowOfFirstSection() : -1;
        UpdateState();
        if (_list is not null && _entries.Count > 0)
        {
            _list.ScrollIntoView(0);
        }
    }

    private void SetSearching(bool searching)
    {
        _searchSettle?.Dispose();
        PseudoClasses.Set(":searching", searching);
    }

    private void SettleSearch()
    {
        _searchSettle?.Dispose();
        _searchSettle = DispatcherTimer.RunOnce(() => PseudoClasses.Set(":searching", false), SearchSettle);
    }

    private void OnItemsChanged()
    {
        if (_pendingIndex >= 0 && LeafAt(_pendingIndex, out var item))
        {
            _pendingIndex = -1;
            SetSelection([item]);
        }
        var index = LeafIndexOf(_selectedItem);
        _syncing++;
        SetAndRaise(SelectedIndexProperty, ref _selectedIndex, index);
        _syncing--;
        if (_populated is null)
        {
            BuildView();
            if (!IsDropDownOpen)
            {
                _cursor = CommittedCursor();
            }
            else if (_cursor >= _entries.Count || IsHeaderAt(_cursor))
            {
                _cursor = -1;
            }
        }
        UpdateState();
    }

    private void OnSelectedItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_syncing == 0 && _selectedItems is not null)
        {
            SetSelection(_selectedItems.Cast<object?>());
        }
    }

    /// <summary>Replaces the committed selection and raises <see cref="SelectionChanged"/>.</summary>
    private void SetSelection(IEnumerable<object?> items)
    {
        var next = new List<object?>();
        foreach (var item in items)
        {
            if (IndexOfValue(next, item) < 0)
            {
                next.Add(item);
            }
        }
        if (!IsMultiple && next.Count > 1)
        {
            next.RemoveRange(1, next.Count - 1);
        }
        var listMatches = _selectedItems is null || (_selectedItems.Count == next.Count && next.Select((o, i) => Equals(_selectedItems[i], o)).All(x => x));
        if (next.SequenceEqual(_selection) && listMatches)
        {
            return;
        }
        var removed = _selection.Where(old => IndexOfValue(next, old) < 0).ToList();
        var added = next.Where(item => IndexOfValue(_selection, item) < 0).ToList();
        _selection.Clear();
        _selection.AddRange(next);
        _syncing++;
        try
        {
            if (_selectedItems is { IsReadOnly: false, IsFixedSize: false } list && !listMatches)
            {
                list.Clear();
                foreach (var item in _selection)
                {
                    list.Add(item);
                }
            }
            SetAndRaise(SelectedItemProperty, ref _selectedItem, _selection.Count > 0 ? _selection[0] : null);
            SetAndRaise(SelectedIndexProperty, ref _selectedIndex, LeafIndexOf(_selectedItem));
        }
        finally
        {
            _syncing--;
        }
        if (!IsDropDownOpen)
        {
            _cursor = CommittedCursor();
        }
        UpdateState();
        if (removed.Count > 0 || added.Count > 0)
        {
            RaiseEvent(new SelectionChangedEventArgs(SelectionChangedEvent, removed, added));
        }
    }

    /// <summary>
    /// The open list's lines for the query (searchable_list/vec.rs
    /// perform_search): the matching rows, and each section with a matching
    /// row or title. With no row at all the list shows its empty view.
    /// </summary>
    private void BuildView()
    {
        _entries.Clear();
        var query = _searchText.Trim();
        var filter = _populated is null && query.Length > 0;
        var lowered = query.ToLowerInvariant();
        IEnumerable<object?> source = _populated ?? (IEnumerable<object?>)ItemsView;
        foreach (var item in source)
        {
            if (item is SelectGroup group)
            {
                var rows = group.Items.Where(row => !filter || Matches(row, query)).ToList();
                if (rows.Count > 0 || !filter || (group.Title ?? string.Empty).ToLowerInvariant().Contains(lowered, StringComparison.Ordinal))
                {
                    _entries.Add(new Entry(group, true));
                    _entries.AddRange(rows.Select(row => new Entry(row, false)));
                }
            }
            else if (!filter || Matches(item, query))
            {
                _entries.Add(new Entry(item, false));
            }
        }
        // list.rs render_items: sections without rows show the empty view instead.
        if (!_entries.Any(e => !e.IsHeader))
        {
            _entries.Clear();
        }
        if (_list is not null)
        {
            _list.ItemsSource = _entries.Select(e => e.Item).ToList();
        }
        PseudoClasses.Set(":empty-view", _entries.Count == 0);
    }

    private bool Matches(object? item, string query) =>
        Filter is { } filter
            ? filter(item, query)
            : TextOf(item).ToLowerInvariant().Contains(query.ToLowerInvariant(), StringComparison.Ordinal);

    private bool IsItemEnabled(object? item) => ItemEnabledSelector?.Invoke(item) ?? true;

    private int CommittedCursor() =>
        _selection.Count == 0 ? -1 : _entries.FindIndex(e => !e.IsHeader && Equals(e.Item, _selection[0]));

    // IndexPath::default(): the first section's first row, if it has one.
    private int FirstRowOfFirstSection()
    {
        if (_entries.Count == 0)
        {
            return -1;
        }
        if (!_entries[0].IsHeader)
        {
            return 0;
        }
        return _entries.Count > 1 && !_entries[1].IsHeader ? 1 : -1;
    }

    /// <summary>list.rs SelectUp / SelectDown over rows_cache: rows only, wrapping at both ends.</summary>
    private void MoveCursor(int step)
    {
        var rows = Enumerable.Range(0, _entries.Count).Where(i => !_entries[i].IsHeader).ToList();
        if (rows.Count == 0)
        {
            return;
        }
        int next;
        var at = rows.IndexOf(_cursor);
        if (at < 0)
        {
            // cache.rs: down from nothing goes to IndexPath::default(), up wraps to the last row.
            next = step > 0 ? (FirstRowOfFirstSection() is var first and >= 0 ? first : rows[0]) : rows[^1];
        }
        else
        {
            next = rows[((at + step) % rows.Count + rows.Count) % rows.Count];
        }
        SetCursor(next, scroll: true);
    }

    private void SetCursor(int index, bool scroll)
    {
        _cursor = index;
        UpdateState();
        if (scroll)
        {
            ScrollCursorIntoView();
        }
    }

    private void ScrollCursorIntoView()
    {
        if (_list is not null && _cursor >= 0 && _cursor < _entries.Count)
        {
            _list.ScrollIntoView(_cursor);
        }
    }

    /// <summary>Brings the title, the trigger context, the rows and the pseudo-classes up to date.</summary>
    private void UpdateState()
    {
        string? title;
        if (IsMultiple)
        {
            // combobox.rs default_trigger_body: the titles joined.
            title = _selection.Count == 0 ? null : TitlePrefix + string.Join(", ", _selection.Select(TextOf));
        }
        else if (IsDropDownOpen && TracksCursor)
        {
            // select.rs display_title: the row under the cursor, or the placeholder.
            title = _cursor >= 0 && _cursor < _entries.Count && !_entries[_cursor].IsHeader
                ? TitlePrefix + TextOf(_entries[_cursor].Item)
                : null;
        }
        else
        {
            title = _selection.Count == 0 ? null : TitlePrefix + TextOf(_selection[0]);
        }
        DisplayTitle = title;
        _triggerContext.Update(_selection, title, PlaceholderText, IsDropDownOpen, IsEffectivelyEnabled);
        PseudoClasses.Set(":clearable", IsCleanable && _selection.Count > 0 && TriggerTemplate is null);
        PseudoClasses.Set(":has-icon", Icon is not null);
        PseudoClasses.Set(":custom-trigger", TriggerTemplate is not null);
        _list?.UpdateRows();
    }

    /// <summary>The <paramref name="index"/>th item, sections flattened.</summary>
    private bool LeafAt(int index, out object? item)
    {
        var i = 0;
        foreach (var leaf in Leaves())
        {
            if (i++ == index)
            {
                item = leaf;
                return true;
            }
        }
        item = null;
        return false;
    }

    private int LeafIndexOf(object? item)
    {
        if (item is null)
        {
            return -1;
        }
        var i = 0;
        foreach (var leaf in Leaves())
        {
            if (Equals(leaf, item))
            {
                return i;
            }
            i++;
        }
        return -1;
    }

    private IEnumerable<object?> Leaves()
    {
        foreach (var item in ItemsView)
        {
            if (item is SelectGroup group)
            {
                foreach (var row in group.Items)
                {
                    yield return row;
                }
            }
            else
            {
                yield return item;
            }
        }
    }

    private static int IndexOfValue(List<object?> list, object? item) => list.FindIndex(o => Equals(o, item));
}

/// <summary>
/// What a <see cref="Select.TriggerTemplate"/> shows (GPUI's
/// ComboboxTriggerContext): the selection, its title, the placeholder, and
/// whether the select is open and enabled.
/// </summary>
public sealed class SelectTriggerContext : AvaloniaObject
{
    /// <summary>The selected items, in order.</summary>
    public static readonly DirectProperty<SelectTriggerContext, IReadOnlyList<object?>> SelectedItemsProperty =
        AvaloniaProperty.RegisterDirect<SelectTriggerContext, IReadOnlyList<object?>>(nameof(SelectedItems), o => o.SelectedItems);

    /// <summary>The first selected item.</summary>
    public static readonly DirectProperty<SelectTriggerContext, object?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<SelectTriggerContext, object?>(nameof(SelectedItem), o => o.SelectedItem);

    /// <summary>Whether anything is selected.</summary>
    public static readonly DirectProperty<SelectTriggerContext, bool> HasSelectionProperty =
        AvaloniaProperty.RegisterDirect<SelectTriggerContext, bool>(nameof(HasSelection), o => o.HasSelection);

    /// <summary>The default trigger's text (<see cref="Select.DisplayTitle"/>), null for the placeholder.</summary>
    public static readonly DirectProperty<SelectTriggerContext, string?> TitleProperty =
        AvaloniaProperty.RegisterDirect<SelectTriggerContext, string?>(nameof(Title), o => o.Title);

    /// <summary>The select's placeholder.</summary>
    public static readonly DirectProperty<SelectTriggerContext, string?> PlaceholderTextProperty =
        AvaloniaProperty.RegisterDirect<SelectTriggerContext, string?>(nameof(PlaceholderText), o => o.PlaceholderText);

    /// <summary>Whether the dropdown is open.</summary>
    public static readonly DirectProperty<SelectTriggerContext, bool> IsOpenProperty =
        AvaloniaProperty.RegisterDirect<SelectTriggerContext, bool>(nameof(IsOpen), o => o.IsOpen);

    /// <summary>Whether the select is enabled.</summary>
    public static readonly DirectProperty<SelectTriggerContext, bool> IsEnabledProperty =
        AvaloniaProperty.RegisterDirect<SelectTriggerContext, bool>(nameof(IsEnabled), o => o.IsEnabled);

    private IReadOnlyList<object?> _selectedItems = [];
    private object? _selectedItem;
    private bool _hasSelection;
    private string? _title;
    private string? _placeholderText;
    private bool _isOpen;
    private bool _isEnabled = true;

    /// <inheritdoc cref="SelectedItemsProperty"/>
    public IReadOnlyList<object?> SelectedItems => _selectedItems;

    /// <inheritdoc cref="SelectedItemProperty"/>
    public object? SelectedItem => _selectedItem;

    /// <inheritdoc cref="HasSelectionProperty"/>
    public bool HasSelection => _hasSelection;

    /// <inheritdoc cref="TitleProperty"/>
    public string? Title => _title;

    /// <inheritdoc cref="PlaceholderTextProperty"/>
    public string? PlaceholderText => _placeholderText;

    /// <inheritdoc cref="IsOpenProperty"/>
    public bool IsOpen => _isOpen;

    /// <inheritdoc cref="IsEnabledProperty"/>
    public bool IsEnabled => _isEnabled;

    internal void Update(IReadOnlyList<object?> selection, string? title, string? placeholder, bool open, bool enabled)
    {
        if (!_selectedItems.SequenceEqual(selection))
        {
            SetAndRaise(SelectedItemsProperty, ref _selectedItems, [.. selection]);
        }
        SetAndRaise(SelectedItemProperty, ref _selectedItem, selection.Count > 0 ? selection[0] : null);
        SetAndRaise(HasSelectionProperty, ref _hasSelection, selection.Count > 0);
        SetAndRaise(TitleProperty, ref _title, title);
        SetAndRaise(PlaceholderTextProperty, ref _placeholderText, placeholder);
        SetAndRaise(IsOpenProperty, ref _isOpen, open);
        SetAndRaise(IsEnabledProperty, ref _isEnabled, enabled);
    }
}

/// <summary>
/// The arguments of <see cref="Select.SelectionChanging"/> (GPUI's
/// on_will_change): what a click or Enter adds and removes, and the selection
/// it leads to, which a handler may edit, or cancel to keep the current one.
/// </summary>
public sealed class SelectionChangingEventArgs : RoutedEventArgs
{
    /// <summary>Creates the arguments.</summary>
    public SelectionChangingEventArgs(RoutedEvent routedEvent, IReadOnlyList<object?> removedItems, IReadOnlyList<object?> addedItems, IList<object?> selection)
        : base(routedEvent)
    {
        RemovedItems = removedItems;
        AddedItems = addedItems;
        Selection = selection;
    }

    /// <summary>The items the action deselects.</summary>
    public IReadOnlyList<object?> RemovedItems { get; }

    /// <summary>The items the action selects.</summary>
    public IReadOnlyList<object?> AddedItems { get; }

    /// <summary>The selection after the action; handlers may change it.</summary>
    public IList<object?> Selection { get; }

    /// <summary>Keeps the current selection.</summary>
    public bool Cancel { get; set; }
}
