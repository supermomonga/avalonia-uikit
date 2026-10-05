using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's ResizablePanelGroup (<c>h_resizable</c> / <c>v_resizable</c>;
/// crates/base/src/resizable): panels side by side (<see cref="Orientation"/>
/// Horizontal, the default) or stacked (Vertical), with a resize handle on the
/// boundary between each two visible panels. The handles are GridSplitters
/// with the GridSplitter theme: a 1px hairline in a 9px band and the pill that
/// answers the pointer (resizable.rs). Children are
/// <see cref="ResizablePanel"/>s; any other control is a panel without a size.
/// <para>
/// Layout follows GPUI's flex rules (panel.rs): on the first layout a sized
/// panel keeps its <see cref="ResizablePanel.Size"/> and the panels without one
/// share what is left equally, within their size ranges. From then on every
/// panel has a length; when the group's own length changes the lengths scale
/// by the same ratio (ResizableState::adjust_to_container_size), and when they
/// do not fill it (a panel was hidden) the visible panels grow by equal parts
/// or shrink in proportion to their lengths, keeping the lengths for later.
/// </para>
/// <para>
/// Dragging a handle resizes the panel before it to the pointer
/// (resize_panel_at_handle): growing it shrinks the panels after it in turn,
/// each down to its minimum, and shrinking it below its minimum shrinks the
/// panels before it in turn, the panel after the handle taking the room. The
/// drag starts when the pointer has moved more than 2px from where it went
/// down, and that first move only starts it (div.rs). <see cref="ResizePanel"/>
/// resizes a panel by the same rules. Adding a panel makes room for it in
/// proportion (insert_panel); removing one lets the others fill its room in
/// proportion (remove_panel). <see cref="Resized"/> is raised with the lengths
/// when a drag ends and after <see cref="ResizePanel"/>. The handles take no
/// focus, as GPUI's; one an app makes focusable moves with the arrow keys by
/// its <c>KeyboardIncrement</c>.
/// </para>
/// </summary>
public class ResizablePanelGroup : Panel
{
    /// <summary>PANEL_MIN_SIZE (resizable/mod.rs): the default smallest length of a panel.</summary>
    internal const double PanelMinSize = 100;

    // div.rs DRAG_THRESHOLD: a drag starts once the pointer is further than this from where it went down.
    private const double DragThreshold = 2;

    /// <summary>Whether the panels sit side by side (Horizontal, GPUI's h_resizable) or stacked (Vertical).</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<ResizablePanelGroup, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>Raised with the panels' lengths when a drag ends and after <see cref="ResizePanel"/> (GPUI's ResizablePanelEvent::Resized).</summary>
    public static readonly RoutedEvent<ResizablePanelResizedEventArgs> ResizedEvent =
        RoutedEvent.Register<ResizablePanelGroup, ResizablePanelResizedEventArgs>(nameof(Resized), RoutingStrategies.Bubble);

    // ResizableState::sizes: each child's length, NaN until it is first laid out
    // (a panel whose state has no size yet).
    private readonly List<double> _sizes = [];
    // The handle before each child but the first.
    private readonly List<ResizablePanelHandle> _handles = [];
    // Each child's length and start as last arranged (0 for a hidden one).
    private double[] _laidOut = [];
    private double[] _starts = [];
    // The group's length along its orientation at the last arrange (ResizableState::bounds).
    private double _length = double.NaN;
    // The handle being pressed, where the pointer went down, and whether the drag has started.
    private ResizablePanelHandle? _pressed;
    private Point _pressedAt;
    private bool _dragging;

    static ResizablePanelGroup()
    {
        AffectsMeasure<ResizablePanelGroup>(OrientationProperty);
        OrientationProperty.Changed.AddClassHandler<ResizablePanelGroup>((group, _) => group.OrientHandles());
    }

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <inheritdoc cref="ResizedEvent"/>
    public event EventHandler<ResizablePanelResizedEventArgs>? Resized
    {
        add => AddHandler(ResizedEvent, value);
        remove => RemoveHandler(ResizedEvent, value);
    }

    /// <summary>
    /// The children's lengths along the group (GPUI's <c>ResizableState::sizes</c>):
    /// NaN for a panel not laid out yet; a hidden panel keeps the length it had.
    /// </summary>
    public IReadOnlyList<double> Sizes => [.. _sizes];

    private bool IsHorizontal => Orientation == Orientation.Horizontal;

    /// <summary>
    /// Resizes the child at <paramref name="index"/> to <paramref name="size"/>
    /// with the drag's rules, within its size range and the group's length, and
    /// raises <see cref="Resized"/> (ResizableState::resize_panel). The last
    /// panel has no handle after it, so the one before it is resized to leave it
    /// that length. Nothing happens for a hidden panel or before the first layout.
    /// </summary>
    public void ResizePanel(int index, double size)
    {
        var visible = VisibleIndices();
        var at = visible.IndexOf(index);
        if (at < 0 || visible.Any(i => double.IsNaN(_sizes[i])))
        {
            return;
        }
        if (at + 1 < visible.Count)
        {
            ResizeAt(visible, at, size);
        }
        else if (at > 0)
        {
            // mod.rs: the last panel is driven by resizing the previous one, so the freed room lands here.
            var previous = visible[at - 1];
            ResizeAt(visible, at - 1, _sizes[previous] + _sizes[index] - size);
        }
        RaiseResized();
    }

    /// <inheritdoc />
    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        base.ChildrenChanged(sender, e);
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                InsertPanels(e.NewStartingIndex, e.NewItems!.Count);
                break;
            case NotifyCollectionChangedAction.Remove:
                _sizes.RemoveRange(e.OldStartingIndex, e.OldItems!.Count);
                FillLength();
                break;
            case NotifyCollectionChangedAction.Replace:
                for (var i = 0; i < e.NewItems!.Count; i++)
                {
                    _sizes[e.OldStartingIndex + i] = double.NaN;
                }
                break;
            case NotifyCollectionChangedAction.Move:
                var moved = _sizes.GetRange(e.OldStartingIndex, e.OldItems!.Count);
                _sizes.RemoveRange(e.OldStartingIndex, moved.Count);
                _sizes.InsertRange(e.NewStartingIndex, moved);
                break;
        }
        SyncHandles();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = IsHorizontal;
        var lengths = Resolve(horizontal ? availableSize.Width : availableSize.Height);
        var cross = horizontal ? availableSize.Height : availableSize.Width;
        double total = 0, desiredCross = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Measure(horizontal ? new Size(lengths[i], cross) : new Size(cross, lengths[i]));
            total += lengths[i];
            desiredCross = Math.Max(desiredCross, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
        }
        ShowHandles();
        foreach (var handle in _handles)
        {
            handle.Measure(horizontal ? new Size(1, cross) : new Size(cross, 1));
        }
        return horizontal ? new Size(total, desiredCross) : new Size(desiredCross, total);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = IsHorizontal;
        var length = horizontal ? finalSize.Width : finalSize.Height;
        var cross = horizontal ? finalSize.Height : finalSize.Width;
        if (!double.IsNaN(_length) && length != _length)
        {
            _length = length;
            FillLength();
        }
        _length = length;
        var lengths = Resolve(length);
        // update_panel_size: a panel's first layout gives it its length. With every
        // length known the panels may flex differently (sized panels stop being
        // flex_none), as GPUI's next frame does: lay out the settled lengths.
        var first = false;
        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i].IsVisible && double.IsNaN(_sizes[i]))
            {
                _sizes[i] = lengths[i];
                first = true;
            }
        }
        if (first && Resolve(length) is var settled && !settled.SequenceEqual(lengths))
        {
            lengths = settled;
            InvalidateMeasure();
        }
        _laidOut = lengths;
        _starts = new double[lengths.Length];
        double offset = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            _starts[i] = offset;
            Children[i].Arrange(horizontal ? new Rect(offset, 0, lengths[i], cross) : new Rect(0, offset, cross, lengths[i]));
            offset += lengths[i];
        }
        // resize_handle.rs: the 1px hairline is the first pixel of the panel after the
        // boundary; the theme widens the handle into its 9px band around it.
        for (var i = 0; i < _handles.Count; i++)
        {
            var start = _starts[i + 1];
            _handles[i].Arrange(horizontal ? new Rect(start, 0, 1, cross) : new Rect(0, start, cross, 1));
        }
        return finalSize;
    }

    internal void OnHandlePressed(ResizablePanelHandle handle, Point at)
    {
        _pressed = handle;
        _pressedAt = at;
        _dragging = false;
    }

    internal void OnHandleMoved(ResizablePanelHandle handle, Point at)
    {
        if (_pressed != handle)
        {
            return;
        }
        // div.rs: the drag starts with the first move past the threshold; the group
        // only resizes on the moves after it (it reads the drag when it paints).
        if (!_dragging)
        {
            _dragging = ((Vector)(at - _pressedAt)).Length > DragThreshold;
            return;
        }
        var visible = VisibleIndices();
        var after = visible.IndexOf(_handles.IndexOf(handle) + 1);
        if (after <= 0)
        {
            return;
        }
        var resized = visible[after - 1];
        // The panel's length runs to the pointer: e.position - panel.bounds.left().
        ResizeAt(visible, after - 1, (IsHorizontal ? at.X : at.Y) - _starts[resized]);
    }

    internal void OnHandleReleased(ResizablePanelHandle handle)
    {
        if (_pressed != handle)
        {
            return;
        }
        var dragged = _dragging;
        _pressed = null;
        _dragging = false;
        // ResizePanelGroupElement: a mouse up ends the drag and reports the sizes.
        if (dragged)
        {
            RaiseResized();
        }
    }

    internal void OnHandleKey(ResizablePanelHandle handle, double delta)
    {
        var visible = VisibleIndices();
        var after = visible.IndexOf(_handles.IndexOf(handle) + 1);
        if (after > 0 && !visible.Any(i => double.IsNaN(_sizes[i])))
        {
            ResizeAt(visible, after - 1, _sizes[visible[after - 1]] + delta);
            RaiseResized();
        }
    }

    private void RaiseResized() => RaiseEvent(new ResizablePanelResizedEventArgs(ResizedEvent, Sizes));

    private List<int> VisibleIndices()
    {
        var visible = new List<int>(Children.Count);
        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i].IsVisible)
            {
                visible.Add(i);
            }
        }
        return visible;
    }

    private static double SizeOf(Control child) => child is ResizablePanel panel ? panel.Size : double.NaN;

    private static (double Min, double Max) RangeOf(Control child)
    {
        if (child is not ResizablePanel panel)
        {
            return (PanelMinSize, double.PositiveInfinity);
        }
        var min = Math.Max(0, panel.MinSize);
        return (min, Math.Max(min, panel.MaxSize));
    }

    // ResizableState::insert_panel: panels added to a laid-out group take their sizes
    // (PANEL_MIN_SIZE without one) and the others give up room in proportion.
    // Before the first layout they are laid out with the rest.
    private void InsertPanels(int start, int count)
    {
        var added = Children.Skip(start).Take(count).ToList();
        // The visible panels already there, as indices into _sizes before the insert.
        var others = VisibleIndices().Where(i => i < start || i >= start + count).Select(i => i < start ? i : i - count).ToList();
        if (double.IsNaN(_length) || others.Count == 0 || others.Any(i => double.IsNaN(_sizes[i])) || added.Any(c => !c.IsVisible))
        {
            _sizes.InsertRange(start, Enumerable.Repeat(double.NaN, count));
            return;
        }
        var sizes = added.Select(c => SizeOf(c) is var s && !double.IsNaN(s) ? s : PanelMinSize).ToList();
        var container = Math.Max(_length, 1);
        var leftover = Math.Max(container - sizes.Sum(), 1);
        foreach (var i in others)
        {
            _sizes[i] = leftover * (_sizes[i] / container);
        }
        _sizes.InsertRange(start, sizes);
    }

    // ResizableState::adjust_to_container_size: the visible panels' lengths scale
    // to fill the group's length (once every one of them has a length).
    private void FillLength()
    {
        var visible = VisibleIndices();
        if (double.IsNaN(_length) || _length <= 0 || visible.Count == 0 || visible.Any(i => double.IsNaN(_sizes[i])))
        {
            return;
        }
        var total = visible.Sum(i => _sizes[i]);
        if (!double.IsFinite(total) || total <= 0)
        {
            return;
        }
        foreach (var i in visible)
        {
            _sizes[i] = _length * (_sizes[i] / total);
        }
    }

    // ResizableState::resize_panel_at_handle, over the visible panels: `at` is the
    // panel before the handle, `size` the length it is asked to take.
    private void ResizeAt(List<int> visible, int at, double size)
    {
        if (at >= visible.Count - 1)
        {
            return;
        }
        var old = visible.Select(i => _sizes[i]).ToArray();
        var ranges = visible.Select(i => RangeOf(Children[i])).ToArray();
        if (size - old[at] == 0)
        {
            return;
        }
        var (min, max) = ranges[at];
        var newSize = Math.Clamp(size, min, max);
        var sizes = (double[])old.Clone();
        var ix = at;
        if (size - old[at] > 0)
        {
            // Growing: the panels after it give up room in turn, each down to its minimum.
            var changed = newSize - old[at];
            sizes[at] = newSize;
            while (changed > 0 && ix < old.Length - 1)
            {
                ix++;
                var reduce = Math.Min(changed, Math.Max(sizes[ix] - ranges[ix].Min, 0));
                sizes[ix] -= reduce;
                changed -= reduce;
            }
        }
        else
        {
            // Shrinking below its minimum shrinks the panels before it in turn; the
            // panel after the handle takes all the room given up.
            var changed = newSize - size;
            sizes[at] = newSize;
            while (changed > 0 && ix > 0)
            {
                ix--;
                var reduce = Math.Min(changed, Math.Max(sizes[ix] - ranges[ix].Min, 0));
                changed -= reduce;
                sizes[ix] -= reduce;
            }
            sizes[at + 1] += old[at] - size - changed;
        }
        // More than the group holds: the resized panel gives the excess back.
        var total = sizes.Sum();
        if (total > _length)
        {
            sizes[at] = Math.Max(sizes[at] - (total - _length), min);
        }
        for (var i = 0; i < visible.Count; i++)
        {
            _sizes[visible[i]] = sizes[i];
        }
        InvalidateMeasure();
    }

    /// <summary>
    /// The children's lengths for a group <paramref name="length"/> long, by the
    /// flex rules GPUI's panels lay out with (panel.rs): a panel with a length
    /// has it as its basis and grows and shrinks; a sized panel on its first
    /// layout keeps its size (flex_none); one without a size starts from the
    /// whole length and shrinks. Each stays within its size range.
    /// </summary>
    private double[] Resolve(double length)
    {
        var count = Children.Count;
        var lengths = new double[count];
        var items = new List<FlexItem>(count);
        var infinite = double.IsInfinity(length) || double.IsNaN(length);
        for (var i = 0; i < count; i++)
        {
            var child = Children[i];
            if (!child.IsVisible)
            {
                continue;
            }
            var (min, max) = RangeOf(child);
            var item = new FlexItem { Index = i, Min = min, Max = max };
            if (!double.IsNaN(_sizes[i]))
            {
                // flex_basis(size.min(end).max(start)).
                (item.Basis, item.Grow, item.Shrink) = (Math.Clamp(_sizes[i], min, max), 1, 1);
            }
            else if (SizeOf(child) is var size && !double.IsNaN(size))
            {
                // A zero size is no size preference (initial_size.is_zero()).
                (item.Basis, item.Grow, item.Shrink) = size > 0 ? (size, 0, 0) : (0, 1, 1);
            }
            else
            {
                // size_full: the basis is the whole length.
                (item.Basis, item.Grow, item.Shrink) = (infinite ? min : length, 1, 1);
            }
            items.Add(item);
        }
        if (infinite)
        {
            length = items.Sum(item => Math.Clamp(item.Basis, item.Min, item.Max));
        }
        Flex(items, length);
        foreach (var item in items)
        {
            lengths[item.Index] = item.Target;
        }
        return lengths;
    }

    // The flexbox algorithm for resolving flexible lengths (CSS Flexbox §9.7), on one line.
    private static void Flex(List<FlexItem> items, double length)
    {
        var growing = items.Sum(item => Math.Clamp(item.Basis, item.Min, item.Max)) < length;
        foreach (var item in items)
        {
            var hypothetical = Math.Clamp(item.Basis, item.Min, item.Max);
            item.Target = item.Basis;
            if ((growing ? item.Grow : item.Shrink) == 0
                || (growing && item.Basis > hypothetical)
                || (!growing && item.Basis < hypothetical))
            {
                item.Target = hypothetical;
                item.Frozen = true;
            }
        }
        while (items.Any(item => !item.Frozen))
        {
            var free = length - items.Sum(item => item.Frozen ? item.Target : item.Basis);
            var open = items.Where(item => !item.Frozen).ToList();
            if (growing)
            {
                var grow = open.Sum(item => item.Grow);
                foreach (var item in open)
                {
                    item.Target = item.Basis + (grow > 0 ? free * item.Grow / grow : 0);
                }
            }
            else
            {
                var shrink = open.Sum(item => item.Shrink * item.Basis);
                foreach (var item in open)
                {
                    item.Target = item.Basis + (shrink > 0 ? free * item.Shrink * item.Basis / shrink : 0);
                }
            }
            double violation = 0;
            foreach (var item in open)
            {
                var clamped = Math.Clamp(item.Target, item.Min, item.Max);
                item.Violation = clamped - item.Target;
                violation += item.Violation;
                item.Target = clamped;
            }
            foreach (var item in open)
            {
                if (violation == 0 || (violation > 0 && item.Violation > 0) || (violation < 0 && item.Violation < 0))
                {
                    item.Frozen = true;
                }
            }
            if (violation != 0)
            {
                // Unfrozen items are laid out again from their bases next round.
                foreach (var item in open.Where(item => !item.Frozen))
                {
                    item.Target = item.Basis;
                }
            }
        }
    }

    // Keeps one handle before each child but the first, after the children in the visual tree.
    private void SyncHandles()
    {
        var wanted = Math.Max(Children.Count - 1, 0);
        while (_handles.Count > wanted)
        {
            var handle = _handles[^1];
            _handles.RemoveAt(_handles.Count - 1);
            LogicalChildren.Remove(handle);
            VisualChildren.Remove(handle);
        }
        while (_handles.Count < wanted)
        {
            var handle = new ResizablePanelHandle(this);
            _handles.Add(handle);
            LogicalChildren.Add(handle);
            VisualChildren.Add(handle);
        }
        OrientHandles();
        InvalidateMeasure();
    }

    private void OrientHandles()
    {
        var direction = IsHorizontal ? GridResizeDirection.Columns : GridResizeDirection.Rows;
        foreach (var handle in _handles)
        {
            handle.ResizeDirection = direction;
        }
    }

    // A handle sits between two visible panels: before a visible child that has one before it.
    private void ShowHandles()
    {
        var seen = Children.Count > 0 && Children[0].IsVisible;
        for (var i = 0; i < _handles.Count; i++)
        {
            var shown = seen && Children[i + 1].IsVisible;
            if (_handles[i].IsVisible != shown)
            {
                _handles[i].IsVisible = shown;
            }
            seen |= Children[i + 1].IsVisible;
        }
    }

    private sealed class FlexItem
    {
        public int Index;
        public double Basis, Grow, Shrink, Min, Max, Target, Violation;
        public bool Frozen;
    }
}

/// <summary>The lengths of a <see cref="ResizablePanelGroup"/>'s panels after it was resized.</summary>
public class ResizablePanelResizedEventArgs(RoutedEvent routedEvent, IReadOnlyList<double> sizes) : RoutedEventArgs(routedEvent)
{
    /// <summary>Each child's length along the group (<see cref="ResizablePanelGroup.Sizes"/>).</summary>
    public IReadOnlyList<double> Sizes { get; } = sizes;
}

/// <summary>
/// A <see cref="ResizablePanelGroup"/>'s resize handle (resize_handle.rs): a
/// GridSplitter with its theme whose drag the group handles.
/// </summary>
internal sealed class ResizablePanelHandle(ResizablePanelGroup group) : GridSplitter
{
    protected override Type StyleKeyOverride => typeof(GridSplitter);

    // The group resizes its panels itself; GridSplitter's own resizing needs a Grid.
    protected override Grid? GetParentGrid() => null;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            group.OnHandlePressed(this, e.GetPosition(group));
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        group.OnHandleMoved(this, e.GetPosition(group));
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        group.OnHandleReleased(this);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        group.OnHandleReleased(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var columns = ResizeDirection == GridResizeDirection.Columns;
        var delta = (e.Key, columns) switch
        {
            (Key.Left, true) or (Key.Up, false) => -KeyboardIncrement,
            (Key.Right, true) or (Key.Down, false) => KeyboardIncrement,
            _ => 0,
        };
        if (delta != 0)
        {
            group.OnHandleKey(this, delta);
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
