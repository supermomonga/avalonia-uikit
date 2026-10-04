using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's TreeItem (base/tree.rs): a label, children, and whether it is
/// expanded or disabled. A <see cref="Tree"/> shows these without a children
/// selector; it reads and writes <see cref="IsExpanded"/>.
/// </summary>
public class TreeItem : INotifyPropertyChanged
{
    private bool _isExpanded;
    private bool _isDisabled;

    /// <summary>The label.</summary>
    public string Label { get; set; } = "";

    /// <summary>The children; an item with children is a folder.</summary>
    [Content]
    public AvaloniaList<TreeItem> Children { get; } = [];

    /// <summary>Whether the children are shown.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => Set(ref _isExpanded, value);
    }

    /// <summary>Whether the item can be selected, toggled and right-clicked.</summary>
    public bool IsDisabled
    {
        get => _isDisabled;
        set => Set(ref _isDisabled, value);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public override string ToString() => Label;

    private void Set(ref bool field, bool value, [CallerMemberName] string? name = null)
    {
        if (field != value)
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}

/// <summary>
/// A visible row of a <see cref="Tree"/> (GPUI's TreeEntry): the item, its
/// depth, and its state when the rows were built.
/// </summary>
public sealed class TreeEntry
{
    internal TreeEntry(object item, int depth, bool isFolder, bool isExpanded, bool isDisabled)
    {
        Item = item;
        Depth = depth;
        IsFolder = isFolder;
        IsExpanded = isExpanded;
        IsDisabled = isDisabled;
    }

    /// <summary>The item.</summary>
    public object Item { get; }

    /// <summary>The depth: 0 for a root.</summary>
    public int Depth { get; }

    /// <summary>Whether the item has children.</summary>
    public bool IsFolder { get; }

    /// <summary>Whether the item is expanded.</summary>
    public bool IsExpanded { get; }

    /// <summary>Whether the item is disabled.</summary>
    public bool IsDisabled { get; }
}

/// <summary>The item a <see cref="Tree"/> event is about (GPUI's TreeEvent).</summary>
public class TreeEventArgs(RoutedEvent routedEvent, object? item) : RoutedEventArgs(routedEvent)
{
    /// <summary>The item, or null when the selection is cleared.</summary>
    public object? Item { get; } = item;
}

/// <summary>
/// GPUI Kit's Tree (tree.rs, base/tree.rs): the visible part of a tree,
/// flattened into rows (GPUI's TreeEntry) and virtualized. Each row is a
/// <see cref="ListItem"/> indented 16px per level with a folder, open-folder or
/// file icon before the item's content.
/// <para>
/// A press selects a row and toggles a folder; Up and Down move the selection
/// and wrap at the ends, Right expands and Left collapses the selected folder,
/// Enter toggles it. A right press marks a row (<see cref="RightClickedItem"/>)
/// without selecting it. Disabled items take no presses.
/// </para>
/// <para>
/// Roots come from <see cref="ItemsSource"/> (or <see cref="Items"/>); children
/// from <see cref="ChildrenSelector"/>, else a <see cref="TreeItem"/>'s
/// children, else the <see cref="ItemTemplate"/>'s items when it is an
/// <see cref="ITreeDataTemplate"/>. Items are told apart by Equals.
/// </para>
/// </summary>
[TemplatePart("PART_Rows", typeof(Control))]
public class Tree : TemplatedControl, IRowsOwner
{
    /// <summary>The roots.</summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<Tree, IEnumerable?>(nameof(ItemsSource));

    /// <summary>The template for an item's content after its icon (a TreeDataTemplate also gives children).</summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<Tree, IDataTemplate?>(nameof(ItemTemplate));

    /// <summary>An item's children.</summary>
    public static readonly StyledProperty<Func<object, IEnumerable?>?> ChildrenSelectorProperty =
        AvaloniaProperty.Register<Tree, Func<object, IEnumerable?>?>(nameof(ChildrenSelector));

    /// <summary>Whether an item is disabled (besides <see cref="TreeItem.IsDisabled"/>).</summary>
    public static readonly StyledProperty<Func<object, bool>?> DisabledSelectorProperty =
        AvaloniaProperty.Register<Tree, Func<object, bool>?>(nameof(DisabledSelector));

    /// <summary>The selected item.</summary>
    public static readonly DirectProperty<Tree, object?> SelectedItemProperty =
        AvaloniaProperty.RegisterDirect<Tree, object?>(nameof(SelectedItem), o => o.SelectedItem, (o, v) => o.SelectedItem = v, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The selected row, or -1.</summary>
    public static readonly DirectProperty<Tree, int> SelectedIndexProperty =
        AvaloniaProperty.RegisterDirect<Tree, int>(nameof(SelectedIndex), o => o.SelectedIndex, (o, v) => o.SelectedIndex = v, unsetValue: -1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The right-clicked item (GPUI's right_clicked_ix).</summary>
    public static readonly DirectProperty<Tree, object?> RightClickedItemProperty =
        AvaloniaProperty.RegisterDirect<Tree, object?>(nameof(RightClickedItem), o => o.RightClickedItem);

    /// <summary>Raised when the selection changes.</summary>
    public static readonly RoutedEvent<TreeEventArgs> SelectionChangedEvent =
        RoutedEvent.Register<Tree, TreeEventArgs>(nameof(SelectionChanged), RoutingStrategies.Bubble);

    /// <summary>Raised when an item is expanded (GPUI's TreeEvent::Expanded).</summary>
    public static readonly RoutedEvent<TreeEventArgs> ExpandedEvent =
        RoutedEvent.Register<Tree, TreeEventArgs>(nameof(Expanded), RoutingStrategies.Bubble);

    /// <summary>Raised when an item is collapsed (GPUI's TreeEvent::Collapsed).</summary>
    public static readonly RoutedEvent<TreeEventArgs> CollapsedEvent =
        RoutedEvent.Register<Tree, TreeEventArgs>(nameof(Collapsed), RoutingStrategies.Bubble);

    private static readonly object s_rowKey = new();

    private readonly HashSet<object> _expanded = [];
    private readonly List<INotifyCollectionChanged> _observedLists = [];
    private readonly List<TreeItem> _observedItems = [];
    private readonly List<IDisposable> _childBindings = [];
    private List<TreeEntry> _entries = [];
    private Dictionary<object, int> _indexOf = [];
    private ListRows? _rowsControl;
    private IDataTemplate? _entryTemplate;
    private object? _selectedItem;
    private int _selectedIndex = -1;
    private object? _rightClickedItem;
    private bool _rebuilding;
    private (object Item, ScrollStrategy Strategy)? _deferredScroll;

    static Tree()
    {
        FocusableProperty.OverrideDefaultValue<Tree>(true);
    }

    /// <summary>Creates a tree.</summary>
    public Tree()
    {
        Items.CollectionChanged += (_, _) =>
        {
            if (ItemsSource is null)
            {
                Rebuild();
            }
        };
    }

    /// <inheritdoc cref="ItemsSourceProperty"/>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>The roots when <see cref="ItemsSource"/> is not set.</summary>
    [Content]
    public AvaloniaList<object> Items { get; } = [];

    /// <inheritdoc cref="ItemTemplateProperty"/>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <inheritdoc cref="ChildrenSelectorProperty"/>
    public Func<object, IEnumerable?>? ChildrenSelector
    {
        get => GetValue(ChildrenSelectorProperty);
        set => SetValue(ChildrenSelectorProperty, value);
    }

    /// <inheritdoc cref="DisabledSelectorProperty"/>
    public Func<object, bool>? DisabledSelector
    {
        get => GetValue(DisabledSelectorProperty);
        set => SetValue(DisabledSelectorProperty, value);
    }

    /// <summary>
    /// The selected item. Setting an item under a collapsed folder expands
    /// its ancestors (GPUI's set_selected_item).
    /// </summary>
    public object? SelectedItem
    {
        get => _selectedItem;
        set => SetSelectedItem(value);
    }

    /// <inheritdoc cref="SelectedIndexProperty"/>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set => Select(value >= 0 && value < _entries.Count ? _entries[value].Item : null);
    }

    /// <inheritdoc cref="RightClickedItemProperty"/>
    public object? RightClickedItem
    {
        get => _rightClickedItem;
        private set => SetAndRaise(RightClickedItemProperty, ref _rightClickedItem, value);
    }

    /// <summary>The visible rows (GPUI's entries).</summary>
    public IReadOnlyList<TreeEntry> Entries => _entries;

    /// <inheritdoc cref="SelectionChangedEvent"/>
    public event EventHandler<TreeEventArgs>? SelectionChanged
    {
        add => AddHandler(SelectionChangedEvent, value);
        remove => RemoveHandler(SelectionChangedEvent, value);
    }

    /// <inheritdoc cref="ExpandedEvent"/>
    public event EventHandler<TreeEventArgs>? Expanded
    {
        add => AddHandler(ExpandedEvent, value);
        remove => RemoveHandler(ExpandedEvent, value);
    }

    /// <inheritdoc cref="CollapsedEvent"/>
    public event EventHandler<TreeEventArgs>? Collapsed
    {
        add => AddHandler(CollapsedEvent, value);
        remove => RemoveHandler(CollapsedEvent, value);
    }

    /// <summary>The row of a visible item, or -1 (GPUI's index_of).</summary>
    public int IndexOf(object item) => _indexOf.TryGetValue(item, out var index) ? index : -1;

    /// <summary>Whether an item is expanded.</summary>
    public bool IsItemExpanded(object item) => item is TreeItem tree ? tree.IsExpanded : _expanded.Contains(item);

    /// <summary>Expands an item, raising <see cref="Expanded"/> if it was collapsed.</summary>
    public void Expand(object item) => SetExpanded(item, true);

    /// <summary>Collapses an item, raising <see cref="Collapsed"/> if it was expanded.</summary>
    public void Collapse(object item) => SetExpanded(item, false);

    /// <summary>Scrolls to a row (GPUI's scroll_to_item): a row out of view goes to the strategy's place.</summary>
    public void ScrollToItem(int index, ScrollStrategy strategy = ScrollStrategy.Top) =>
        _rowsControl?.ScrollToRow(index, strategy, RowScrollMode.UniformList);

    /// <summary>
    /// Expands the item's ancestors and scrolls it into view (GPUI's
    /// reveal_item). Before the first layout, it waits for one.
    /// </summary>
    public void RevealItem(object item, ScrollStrategy strategy = ScrollStrategy.Top)
    {
        ExpandAncestors(item);
        _deferredScroll = (item, strategy);
        ApplyDeferredScroll();
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_rowsControl is not null)
        {
            _rowsControl.Owner = null;
            _rowsControl.ItemsSource = null;
        }
        base.OnApplyTemplate(e);
        // The row's icon, indent and content, from the theme.
        _entryTemplate = this.TryFindResource("UIKitTreeEntryTemplate", ActualThemeVariant, out var template) ? template as IDataTemplate : null;
        _rowsControl = e.NameScope.Find<ListRows>("PART_Rows");
        if (_rowsControl is not null)
        {
            _rowsControl.Owner = this;
        }
        Rebuild();
        ApplyDeferredScroll();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsSourceProperty)
        {
            // base/tree.rs set_items: new items clear the selection and the mark.
            Select(null);
            RightClickedItem = null;
            Rebuild();
        }
        else if (change.Property == ChildrenSelectorProperty || change.Property == DisabledSelectorProperty ||
                 change.Property == ItemTemplateProperty)
        {
            Rebuild();
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsLeftButtonPressed && !properties.IsRightButtonPressed)
        {
            return;
        }
        Focus(NavigationMethod.Pointer);
        // A disabled row takes no input, so the press lands outside every row.
        if (EntryAt(e.Source) is not { } index || _entries[index].IsDisabled)
        {
            return;
        }
        if (properties.IsRightButtonPressed)
        {
            // base/tree.rs: a right press marks the row without selecting it.
            RightClickedItem = _entries[index].Item;
            UpdateRealizedRows();
            return;
        }
        // base/tree.rs on_entry_click: select the row and toggle a folder.
        Select(_entries[index].Item);
        Toggle(index);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || _entries.Count == 0)
        {
            return;
        }
        var selected = _selectedIndex;
        switch (e.Key)
        {
            case Key.Up:
            {
                // base/tree.rs on_action_up: from the selection (or 0), the row before; the last before the first.
                var index = Math.Max(selected, 0) - 1;
                SelectRow(index >= 0 ? index : _entries.Count - 1, ScrollStrategy.Top);
                e.Handled = true;
                break;
            }
            case Key.Down:
            {
                // base/tree.rs on_action_down: from the selection (or 0), the row after; the first after the last.
                var index = Math.Max(selected, 0) + 1;
                SelectRow(index < _entries.Count ? index : 0, ScrollStrategy.Bottom);
                e.Handled = true;
                break;
            }
            case Key.Left when selected >= 0 && _entries[selected] is { IsFolder: true, IsExpanded: true }:
            case Key.Right when selected >= 0 && _entries[selected] is { IsFolder: true, IsExpanded: false }:
            case Key.Enter when selected >= 0 && _entries[selected].IsFolder:
                // on_action_left / right / confirm: collapse, expand or toggle the selected folder.
                Toggle(selected);
                e.Handled = true;
                break;
        }
    }

    private void SelectRow(int index, ScrollStrategy strategy)
    {
        Select(_entries[index].Item);
        ScrollToItem(index, strategy);
    }

    private int? EntryAt(object? source)
    {
        for (var visual = source as Visual; visual is not null && visual != this; visual = visual.GetVisualParent())
        {
            if (visual is ListItem item && _rowsControl?.IndexFromContainer(item) is int index and >= 0 && index < _entries.Count)
            {
                return index;
            }
        }
        return null;
    }

    private void Toggle(int index)
    {
        var entry = _entries[index];
        if (!entry.IsFolder)
        {
            return;
        }
        // base/tree.rs toggle_expand: a toggle also clears the right-clicked mark.
        RightClickedItem = null;
        SetExpanded(entry.Item, !entry.IsExpanded);
    }

    private void SetExpanded(object item, bool expanded)
    {
        if (IsItemExpanded(item) == expanded)
        {
            return;
        }
        _rebuilding = true;
        try
        {
            if (item is TreeItem tree)
            {
                tree.IsExpanded = expanded;
            }
            else if (expanded)
            {
                _expanded.Add(item);
            }
            else
            {
                _expanded.Remove(item);
            }
        }
        finally
        {
            _rebuilding = false;
        }
        RaiseEvent(new TreeEventArgs(expanded ? ExpandedEvent : CollapsedEvent, item));
        Rebuild();
    }

    private void SetSelectedItem(object? item)
    {
        // base/tree.rs set_selected_item: an item out of view gets its ancestors expanded.
        if (item is not null && IndexOf(item) < 0)
        {
            ExpandAncestors(item);
        }
        Select(item);
    }

    private void ExpandAncestors(object item)
    {
        // base/tree.rs expand_ancestors: from the root down, each collapsed ancestor opens.
        var path = new List<object>();
        foreach (var root in Roots())
        {
            if (FindPath(root, item, path))
            {
                break;
            }
        }
        var changed = false;
        _rebuilding = true;
        try
        {
            foreach (var ancestor in path)
            {
                if (!IsItemExpanded(ancestor))
                {
                    if (ancestor is TreeItem tree)
                    {
                        tree.IsExpanded = true;
                    }
                    else
                    {
                        _expanded.Add(ancestor);
                    }
                    RaiseEvent(new TreeEventArgs(ExpandedEvent, ancestor));
                    changed = true;
                }
            }
        }
        finally
        {
            _rebuilding = false;
        }
        if (changed)
        {
            Rebuild();
        }
    }

    /// <summary>Finds the ancestors of <paramref name="target"/> under <paramref name="node"/>, root first.</summary>
    private bool FindPath(object node, object target, List<object> path)
    {
        if (Equals(node, target))
        {
            return true;
        }
        if (ChildrenOf(node, null) is not { } children)
        {
            return false;
        }
        path.Add(node);
        foreach (var child in children)
        {
            if (child is not null && FindPath(child, target, path))
            {
                return true;
            }
        }
        path.RemoveAt(path.Count - 1);
        return false;
    }

    private void Select(object? item)
    {
        var index = item is null ? -1 : IndexOf(item);
        if (Equals(item, _selectedItem) && index == _selectedIndex)
        {
            return;
        }
        var oldItem = _selectedItem;
        var oldIndex = _selectedIndex;
        _selectedItem = item;
        _selectedIndex = index;
        RaisePropertyChanged(SelectedItemProperty, oldItem, item);
        RaisePropertyChanged(SelectedIndexProperty, oldIndex, index);
        UpdateRealizedRows();
        if (!Equals(item, oldItem))
        {
            RaiseEvent(new TreeEventArgs(SelectionChangedEvent, item));
        }
    }

    private void ApplyDeferredScroll()
    {
        if (_deferredScroll is not { } request || _rowsControl is null || IndexOf(request.Item) is not (var index and >= 0))
        {
            return;
        }
        _deferredScroll = null;
        ScrollToItem(index, request.Strategy);
    }

    private IEnumerable Roots() => ItemsSource ?? Items;

    /// <summary>An item's children; <paramref name="bindings"/> keeps a template's binding alive.</summary>
    private IEnumerable? ChildrenOf(object item, List<IDisposable>? bindings)
    {
        if (ChildrenSelector is { } selector)
        {
            return selector(item);
        }
        if (item is TreeItem tree)
        {
            return tree.Children;
        }
        if (ItemTemplate is ITreeDataTemplate template)
        {
            // A TreeDataTemplate binds its ItemsSource against the item as data context.
            var probe = new ChildrenProbe { DataContext = item };
            var binding = template.BindChildren(probe, ChildrenProbe.ChildrenProperty, item);
            if (bindings is not null)
            {
                bindings.Add(binding);
                probe.PropertyChanged += (_, e) =>
                {
                    if (e.Property == ChildrenProbe.ChildrenProperty)
                    {
                        Rebuild();
                    }
                };
            }
            else
            {
                binding.Dispose();
            }
            return probe.Children;
        }
        return null;
    }

    private void Rebuild()
    {
        if (_rebuilding)
        {
            return;
        }
        _rebuilding = true;
        try
        {
            foreach (var list in _observedLists)
            {
                list.CollectionChanged -= OnCollectionChanged;
            }
            _observedLists.Clear();
            foreach (var item in _observedItems)
            {
                item.PropertyChanged -= OnItemChanged;
            }
            _observedItems.Clear();
            foreach (var binding in _childBindings)
            {
                binding.Dispose();
            }
            _childBindings.Clear();

            // base/tree.rs add_entry: depth-first, the children of expanded items only.
            var entries = new List<TreeEntry>();
            var indexOf = new Dictionary<object, int>();
            var roots = Roots();
            Observe(roots);
            foreach (var root in roots)
            {
                if (root is not null)
                {
                    Add(root, 0);
                }
            }

            void Add(object item, int depth)
            {
                if (item is TreeItem tree)
                {
                    tree.PropertyChanged += OnItemChanged;
                    _observedItems.Add(tree);
                }
                var children = ChildrenOf(item, _childBindings);
                var isFolder = children is not null && HasAny(children);
                var isExpanded = IsItemExpanded(item);
                var isDisabled = item is TreeItem { IsDisabled: true } || DisabledSelector?.Invoke(item) == true;
                indexOf.TryAdd(item, entries.Count);
                entries.Add(new TreeEntry(item, depth, isFolder, isExpanded, isDisabled));
                if (children is not null)
                {
                    Observe(children);
                    if (isFolder && isExpanded)
                    {
                        foreach (var child in children)
                        {
                            if (child is not null)
                            {
                                Add(child, depth + 1);
                            }
                        }
                    }
                }
            }

            _entries = entries;
            _indexOf = indexOf;
            if (_rowsControl is not null)
            {
                _rowsControl.ItemsSource = entries;
            }
        }
        finally
        {
            _rebuilding = false;
        }
        // The selection follows its item; an item out of view keeps it with no row.
        var index = _selectedItem is null ? -1 : IndexOf(_selectedItem);
        if (index != _selectedIndex)
        {
            var old = _selectedIndex;
            _selectedIndex = index;
            RaisePropertyChanged(SelectedIndexProperty, old, index);
        }
        ApplyDeferredScroll();
    }

    private static bool HasAny(IEnumerable items)
    {
        if (items is ICollection collection)
        {
            return collection.Count > 0;
        }
        var enumerator = items.GetEnumerator();
        try
        {
            return enumerator.MoveNext();
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    private void Observe(IEnumerable items)
    {
        if (items is INotifyCollectionChanged observable && !_observedLists.Contains(observable))
        {
            observable.CollectionChanged += OnCollectionChanged;
            _observedLists.Add(observable);
        }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TreeItem.IsExpanded) or nameof(TreeItem.IsDisabled))
        {
            Rebuild();
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
            if (container is ListItem row && row.Content is TreeEntry entry)
            {
                row.IsSelected = Equals(entry.Item, _selectedItem);
                row.IsSecondarySelected = Equals(entry.Item, _rightClickedItem);
            }
        }
    }

    object IRowsOwner.RecycleKey(object? row) => s_rowKey;

    Control IRowsOwner.CreateRow(object recycleKey) => new ListItem();

    void IRowsOwner.PrepareRow(Control container, object? row, int index)
    {
        if (container is not ListItem item || row is not TreeEntry entry)
        {
            return;
        }
        item.Content = entry;
        item.ContentTemplate = _entryTemplate;
        item.IsEnabled = !entry.IsDisabled;
        item.IsSelected = Equals(entry.Item, _selectedItem);
        item.IsSecondarySelected = Equals(entry.Item, _rightClickedItem);
    }

    void IRowsOwner.ClearRow(Control container)
    {
        container.ClearValue(ContentControl.ContentTemplateProperty);
        container.ClearValue(ContentControl.ContentProperty);
    }

    /// <summary>Holds the children a TreeDataTemplate binds for an item.</summary>
    private sealed class ChildrenProbe : StyledElement
    {
        public static readonly StyledProperty<IEnumerable?> ChildrenProperty =
            AvaloniaProperty.Register<ChildrenProbe, IEnumerable?>(nameof(Children));

        public IEnumerable? Children => GetValue(ChildrenProperty);
    }
}
