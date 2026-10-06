using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// A tab being dragged (uikit:Tabs.Reorderable, DragGroup and
/// DetachedWindowFactory), one at a time. The bar the press was in holds the
/// pointer capture to the end, so the tab can move to bars in other windows,
/// and out into a window of its own, while the events keep coming to it.
/// </summary>
/// <remarks>
/// Bars are found by comparing screen points, which only windows share: a
/// browser's views each count from their own origin, so there a tab only
/// moves between bars of one view. Tabs move by render transforms while they
/// are dragged, and the items change once, at the drop.
/// </remarks>
internal sealed class TabDragSession
{
    // How far the pointer moves before a press on a tab becomes a drag.
    private const double Threshold = 4;

    // How far past its bar the tab is dragged before it leaves for a window (Tabalonia's DetachTriggerDistance).
    private const double DetachDistance = 32;

    // The ends of an overflowing bar that scroll it while the pointer is held there, and each step.
    private const double ScrollZone = 24;
    private const double ScrollStep = 8;

    // The pseudo-class of the tab following the pointer.
    private const string Dragging = ":dragging";

    // The other tabs make room as Tabalonia's do.
    private static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(200);

    private static readonly List<WeakReference<SelectingItemsControl>> s_grouped = [];
    private static readonly ConditionalWeakTable<Window, object> s_madeWindows = new();
    private static Press? s_press;
    private static TabDragSession? s_current;

    private readonly SelectingItemsControl _source;
    private readonly IPointer _pointer;
    private readonly object _item;
    private readonly Vector _grab;
    private readonly Dictionary<Control, Moved> _moved = [];
    private readonly HashSet<Window> _left = [];
    private SelectingItemsControl _host;
    private int _from;
    private int _to;
    private PixelPoint _screen;
    private Point _client;
    private Window? _floating;
    private PixelPoint _floatingOffset;
    private Window? _hidden;
    private DispatcherTimer? _scroll;
    private double _scrollStep;
    private bool _ended;

    private TabDragSession(Press press, object item)
    {
        _source = _host = press.Bar;
        _pointer = press.Pointer;
        _item = item;
        _from = _to = press.Bar.IndexFromContainer(press.Tab);
        _grab = press.Bar.TranslatePoint(press.At, press.Tab) is { } grab ? new Vector(grab.X, grab.Y) : default;
        press.Bar.SelectedIndex = _from;
        _pointer.Capture(press.Bar);
    }

    /// <summary>Starts or stops watching <paramref name="bar"/>'s tabs for drags.</summary>
    public static void Watch(SelectingItemsControl bar, bool reorderable)
    {
        bar.RemoveHandler(InputElement.PointerPressedEvent, OnPressed);
        bar.RemoveHandler(InputElement.PointerMovedEvent, OnMoved);
        bar.RemoveHandler(InputElement.PointerReleasedEvent, OnReleased);
        bar.RemoveHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost);
        if (!reorderable)
        {
            return;
        }
        bar.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Bubble, handledEventsToo: true);
        bar.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Bubble, handledEventsToo: true);
        bar.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Bubble, handledEventsToo: true);
        bar.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Direct, handledEventsToo: true);
    }

    /// <summary>Keeps <paramref name="bar"/> among the bars of its drag group, or drops it.</summary>
    public static void Join(SelectingItemsControl bar)
    {
        s_grouped.RemoveAll(reference => !reference.TryGetTarget(out var other) || other == bar);
        if (Tabs.GetDragGroup(bar) is not null)
        {
            s_grouped.Add(new WeakReference<SelectingItemsControl>(bar));
        }
    }

    /// <summary>Closes a window made for dragged tabs once its bars hold no tab.</summary>
    public static void CloseIfEmpty(Window? window)
    {
        if (window is not null && s_madeWindows.TryGetValue(window, out _) && TabCount(window) == 0)
        {
            window.Close();
        }
    }

    private static void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not SelectingItemsControl bar)
        {
            return;
        }
        // A press means the last drag's button went up: end one that never saw it
        // (its window closed under it, say), or no tab would drag again.
        s_current?.End();
        // A press in a bar inside a tab's content reaches the outer bar too: it keeps the inner one's.
        if (e.GetCurrentPoint(bar).Properties.IsLeftButtonPressed &&
            TabAt(bar, e.Source as Visual) is { IsEffectivelyEnabled: true } tab && Tabs.TabList.Of(bar) is not null)
        {
            s_press = new Press(bar, e.Pointer, tab, e.GetPosition(bar));
        }
        else if (s_press?.Bar == bar)
        {
            s_press = null;
        }
    }

    private static void OnMoved(object? sender, PointerEventArgs e)
    {
        if (s_current is { } drag)
        {
            if (drag._source == sender && drag._pointer == e.Pointer)
            {
                e.Handled = true;
                drag.Move(e);
            }
            return;
        }
        if (s_press is not { } press || press.Bar != sender || press.Pointer != e.Pointer)
        {
            return;
        }
        if (!e.GetCurrentPoint(press.Bar).Properties.IsLeftButtonPressed)
        {
            s_press = null;
            return;
        }
        var moved = e.GetPosition(press.Bar) - press.At;
        if (Math.Abs(moved.X) < Threshold && Math.Abs(moved.Y) < Threshold)
        {
            return;
        }
        s_press = null;
        if (press.Bar.IndexFromContainer(press.Tab) < 0 || press.Bar.ItemFromContainer(press.Tab) is not { } item)
        {
            return;
        }
        e.Handled = true;
        s_current = new TabDragSession(press, item);
        s_current.Move(e);
    }

    private static void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (s_press?.Pointer == e.Pointer)
        {
            s_press = null;
        }
        if (s_current is { } drag && drag._source == sender && drag._pointer == e.Pointer)
        {
            e.Handled = true;
            drag.End();
        }
    }

    private static void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (s_current is { } drag && drag._source == sender && drag._pointer == e.Pointer)
        {
            drag.End();
        }
    }

    private void Move(PointerEventArgs e)
    {
        if (TopLevel.GetTopLevel(_source) is not { } top)
        {
            return;
        }
        _client = e.GetPosition(top);
        _screen = top.PointToScreen(_client);
        if (_floating is { } window)
        {
            if (!Attach())
            {
                window.Position = _screen - _floatingOffset;
            }
            return;
        }
        // Off its bar, the tab joins a bar of its group under the pointer (one just below,
        // say); only well off every bar does it leave for a window.
        if ((Off(0) && Attach()) || (Off(DetachDistance) && Detach()))
        {
            return;
        }
        Arrange();
    }

    /// <summary>Whether the pointer is more than <paramref name="distance"/> off the bar holding the tab, in a group.</summary>
    private bool Off(double distance) =>
        Tabs.GetDragGroup(_host) is not null && Part<Control>(_host, "PART_Bar") is { } bar &&
        At(bar) is { } at && !new Rect(bar.Bounds.Size).Inflate(distance).Contains(at);

    /// <summary>Moves the tab to the bar of its group under the pointer, if there is one.</summary>
    private bool Attach()
    {
        if (Tabs.GetDragGroup(_host) is not { } group || BarAt(group) is not { } target || !MoveTo(target, InsertIndex(target)))
        {
            return false;
        }
        if (_floating is { } window)
        {
            // The window that followed the pointer is empty now. It stays, hidden, until the
            // drop: it may hold the pointer capture, and it is reused if the tab leaves again.
            _floating = null;
            _hidden = window;
            window.Hide();
        }
        Arrange();
        return true;
    }

    /// <summary>Moves the tab into a window of its own that follows the pointer.</summary>
    private bool Detach()
    {
        if (TopLevel.GetTopLevel(_host) is not Window from || Tabs.GetDragGroup(_host) is not { } group)
        {
            return false;
        }
        // The only tab of a window made for dragged tabs: the window itself moves.
        if (s_madeWindows.TryGetValue(from, out _) && TabCount(from) == 1)
        {
            Restore();
            Tabs.TabList.Dragged(_host, false);
            Follow(from);
            return true;
        }
        var window = _hidden;
        var bar = window is null ? null : BarIn(window, group);
        if (window is null || bar is null)
        {
            if (Tabs.GetDetachedWindowFactory(_host)?.Invoke(_item) is not { } made)
            {
                return false;
            }
            if (BarIn(made, group) is not { } madeBar)
            {
                made.Close();
                return false;
            }
            s_madeWindows.AddOrUpdate(made, made);
            (window, bar) = (made, madeBar);
        }
        // Put the window where the tab keeps its place under the pointer: first as the tab
        // sits in its window now, then, laid out, exactly.
        var offset = _host.ContainerFromItem(_item) is { } tab && tab.IsAttachedToVisualTree()
            ? tab.PointToScreen(new Point(_grab.X, _grab.Y)) - from.PointToScreen(default)
            : default;
        if (!MoveTo(bar, 0))
        {
            CloseIfEmpty(window);
            return false;
        }
        _hidden = null;
        window.ShowActivated = false;
        window.Position = _screen - offset;
        window.Show();
        if (bar.ContainerFromItem(_item) is { } placed && placed.IsAttachedToVisualTree())
        {
            window.Position += _screen - placed.PointToScreen(new Point(_grab.X, _grab.Y));
        }
        Follow(window);
        return true;
    }

    private void Follow(Window window)
    {
        StopScroll();
        _floating = window;
        _floatingOffset = _screen - window.Position;
    }

    /// <summary>Moves the tab's item from the bar holding it to <paramref name="target"/>.</summary>
    private bool MoveTo(SelectingItemsControl target, int index)
    {
        if (Tabs.TabList.Of(_host) is not { } from || Tabs.TabList.Of(target) is not { } to || from.IndexOf(_item) is not (>= 0 and var at))
        {
            return false;
        }
        StopScroll();
        Restore();
        Tabs.TabList.Dragged(_host, false);
        // Out of one bar before into the other: a tab that is its own item has one parent.
        from.RemoveAt(at);
        try
        {
            to.Insert(Math.Clamp(index, 0, to.Count), _item);
        }
        catch (Exception e) when (e is ArgumentException or InvalidCastException or NotSupportedException)
        {
            // The other bar's list takes items of another type.
            from.Insert(at, _item);
            _host.SelectedItem = _item;
            return false;
        }
        Tabs.TabList.Removed(_host, at, wasSelected: true);
        if (TopLevel.GetTopLevel(_host) is Window left)
        {
            _left.Add(left);
        }
        _host = target;
        _host.SelectedItem = _item;
        _from = _to = to.IndexOf(_item);
        _host.UpdateLayout();
        return true;
    }

    /// <summary>The tab follows the pointer along its bar, and the others make room for it.</summary>
    private void Arrange()
    {
        if (Tabs.TabList.Of(_host)?.IndexOf(_item) is not (>= 0 and var from) ||
            _host.ItemsPanelRoot is not { } panel || !panel.IsAttachedToVisualTree() ||
            _host.ContainerFromIndex(from) is not { } dragged)
        {
            return;
        }
        if (from != _from)
        {
            // The items changed under the drag.
            Restore();
            _from = _to = from;
        }
        var horizontal = panel is not StackPanel { Orientation: Orientation.Vertical };
        double Start(Control tab) => horizontal ? tab.Bounds.X : tab.Bounds.Y;
        double Length(Control tab) => horizontal ? tab.Bounds.Width : tab.Bounds.Height;
        if (At(panel) is not { } pointer)
        {
            return;
        }
        var extent = horizontal ? panel.Bounds.Width : panel.Bounds.Height;
        var start = Math.Clamp(horizontal ? pointer.X - _grab.X : pointer.Y - _grab.Y, 0, Math.Max(0, extent - Length(dragged)));
        Translate(dragged, start - Start(dragged), horizontal, slide: false);
        var center = start + Length(dragged) / 2;
        var to = _from;
        for (var i = 0; i < _host.ItemCount; i++)
        {
            if (i != _from && _host.ContainerFromIndex(i) is { } tab)
            {
                var middle = Start(tab) + Length(tab) / 2;
                if (i > _from && center > middle)
                {
                    to = Math.Max(to, i);
                }
                else if (i < _from && center < middle)
                {
                    to = Math.Min(to, i);
                }
            }
        }
        var room = Length(dragged) + (panel is StackPanel stack ? stack.Spacing : 0);
        for (var i = 0; i < _host.ItemCount; i++)
        {
            if (i != _from && _host.ContainerFromIndex(i) is { } tab)
            {
                var shift = i > _from && i <= to ? -room : i < _from && i >= to ? room : 0;
                Translate(tab, shift, horizontal, slide: true);
            }
        }
        _to = to;
        Tabs.TabList.Dragged(_host, true);
        ScrollAtEnds(horizontal);
    }

    private void Translate(Control tab, double offset, bool horizontal, bool slide)
    {
        if (!_moved.TryGetValue(tab, out var moved))
        {
            moved = new Moved(
                tab.IsSet(Visual.RenderTransformProperty), tab.RenderTransform,
                tab.IsSet(Visual.ZIndexProperty), tab.ZIndex,
                tab.Transitions is null);
            _moved[tab] = moved;
        }
        if (slide && moved.Transition is null)
        {
            moved.Transition = new TransformOperationsTransition
            {
                Property = Visual.RenderTransformProperty,
                Duration = SlideDuration,
                Easing = new CubicEaseOut(),
            };
            (tab.Transitions ??= new Transitions()).Add(moved.Transition);
        }
        if (!slide)
        {
            // Over the other tabs, drawing the selected tab's indicator itself (the theme's :dragging).
            tab.ZIndex = int.MaxValue;
            ((IPseudoClasses)tab.Classes).Set(Dragging, true);
        }
        var translate = TransformOperations.CreateBuilder(1);
        translate.AppendTranslate(horizontal ? offset : 0, horizontal ? 0 : offset);
        var value = translate.Build();
        if (tab.RenderTransform is not TransformOperations current || current.Value != value.Value)
        {
            tab.RenderTransform = value;
        }
    }

    /// <summary>Puts the tabs moved by the drag back as they were.</summary>
    private void Restore()
    {
        foreach (var (tab, moved) in _moved)
        {
            if (moved.Transition is { } transition)
            {
                tab.Transitions?.Remove(transition);
                if (moved.NoTransitions && tab.Transitions is { Count: 0 })
                {
                    tab.Transitions = null;
                }
            }
            if (moved.TransformSet)
            {
                tab.RenderTransform = moved.Transform;
            }
            else
            {
                tab.ClearValue(Visual.RenderTransformProperty);
            }
            if (moved.ZIndexSet)
            {
                tab.ZIndex = moved.ZIndex;
            }
            else
            {
                tab.ClearValue(Visual.ZIndexProperty);
            }
            ((IPseudoClasses)tab.Classes).Set(Dragging, false);
        }
        _moved.Clear();
    }

    /// <summary>Scrolls an overflowing bar while the pointer is held at one of its ends.</summary>
    private void ScrollAtEnds(bool horizontal)
    {
        if (Part<ScrollViewer>(_host, "PART_Scroller") is not { } scroller || At(scroller) is not { } at)
        {
            StopScroll();
            return;
        }
        var viewport = horizontal ? scroller.Viewport.Width : scroller.Viewport.Height;
        var overflow = (horizontal ? scroller.Extent.Width : scroller.Extent.Height) - viewport;
        var position = horizontal ? at.X : at.Y;
        var offset = horizontal ? scroller.Offset.X : scroller.Offset.Y;
        _scrollStep = position < ScrollZone && offset > 0 ? -ScrollStep
            : position > viewport - ScrollZone && offset < overflow ? ScrollStep
            : 0;
        if (overflow <= 0 || _scrollStep == 0)
        {
            StopScroll();
            return;
        }
        if (_scroll is null)
        {
            _scroll = new DispatcherTimer(TimeSpan.FromMilliseconds(16), DispatcherPriority.Input, (_, _) => ScrollStepOnce(scroller, horizontal));
            _scroll.Start();
        }
    }

    private void ScrollStepOnce(ScrollViewer scroller, bool horizontal)
    {
        if (_ended || _floating is not null)
        {
            StopScroll();
            return;
        }
        var overflow = Math.Max(0, (horizontal ? scroller.Extent.Width - scroller.Viewport.Width : scroller.Extent.Height - scroller.Viewport.Height));
        var offset = horizontal ? scroller.Offset.X : scroller.Offset.Y;
        var next = Math.Clamp(offset + _scrollStep, 0, overflow);
        scroller.Offset = horizontal ? scroller.Offset.WithX(next) : scroller.Offset.WithY(next);
        _host.UpdateLayout();
        Arrange();
    }

    private void StopScroll()
    {
        _scroll?.Stop();
        _scroll = null;
    }

    private void End()
    {
        if (_ended)
        {
            return;
        }
        _ended = true;
        s_current = null;
        StopScroll();
        if (_floating is { } window)
        {
            Restore();
            window.Activate();
        }
        else
        {
            Restore();
            Tabs.TabList.Dragged(_host, false);
            if (_to != _from && Tabs.TabList.Of(_host) is { } items && items.IndexOf(_item) is >= 0 and var at)
            {
                items.RemoveAt(at);
                items.Insert(Math.Clamp(_to, 0, items.Count), _item);
            }
            _host.SelectedItem = _item;
            if (TopLevel.GetTopLevel(_host) is Window host && host != TopLevel.GetTopLevel(_source))
            {
                host.Activate();
            }
        }
        if (_pointer.Captured == _source)
        {
            _pointer.Capture(null);
        }
        if (_hidden is { } hidden)
        {
            _left.Add(hidden);
        }
        foreach (var left in _left)
        {
            CloseIfEmpty(left);
        }
    }

    /// <summary>The bar of <paramref name="group"/> under the pointer, other than the one holding the tab.</summary>
    private SelectingItemsControl? BarAt(string group)
    {
        var hostTop = TopLevel.GetTopLevel(_host);
        SelectingItemsControl? found = null;
        for (var i = s_grouped.Count - 1; i >= 0; i--)
        {
            if (!s_grouped[i].TryGetTarget(out var bar))
            {
                s_grouped.RemoveAt(i);
                continue;
            }
            if (bar == _host || Tabs.GetDragGroup(bar) != group || !bar.IsAttachedToVisualTree() ||
                !bar.IsEffectivelyVisible || !bar.IsEffectivelyEnabled || TopLevel.GetTopLevel(bar) is not { } top)
            {
                continue;
            }
            // Screen points compare across windows only.
            if (top != hostTop && (hostTop is not Window || top is not Window { IsVisible: true } window || window == _hidden))
            {
                continue;
            }
            if (Part<Control>(bar, "PART_Bar") is not { } part || At(part) is not { } at || !new Rect(part.Bounds.Size).Contains(at))
            {
                continue;
            }            if (top == hostTop)
            {
                return bar;
            }
            if (found is null || top is Window { IsActive: true })
            {
                found = bar;
            }
        }
        return found;
    }

    /// <summary>Where the pointer would put the tab among <paramref name="target"/>'s tabs.</summary>
    private int InsertIndex(SelectingItemsControl target)
    {
        if (target.ItemsPanelRoot is not { } panel || At(panel) is not { } at)
        {
            return target.ItemCount;
        }
        var horizontal = panel is not StackPanel { Orientation: Orientation.Vertical };
        for (var i = 0; i < target.ItemCount; i++)
        {
            if (target.ContainerFromIndex(i) is { } tab && (horizontal ? at.X < tab.Bounds.Center.X : at.Y < tab.Bounds.Center.Y))
            {
                return i;
            }
        }
        return target.ItemCount;
    }

    /// <summary>
    /// The pointer in <paramref name="visual"/>'s coordinates: in the source's top level
    /// as the pointer events give it, elsewhere through the screen's whole pixels.
    /// </summary>
    private Point? At(Visual visual)
    {
        if (!visual.IsAttachedToVisualTree())
        {
            return null;
        }
        return TopLevel.GetTopLevel(visual) is { } top && top == TopLevel.GetTopLevel(_source)
            ? top.TranslatePoint(_client, visual)
            : visual.PointToClient(_screen);
    }

    /// <summary>The tab (container) of <paramref name="bar"/> that <paramref name="source"/> is in, unless it is in a button there.</summary>
    private static Control? TabAt(SelectingItemsControl bar, Visual? source)
    {
        for (var visual = source; visual is not null && visual != bar; visual = visual.GetVisualParent())
        {
            if (visual is Button)
            {
                return null;
            }
            if (visual is Control control && bar.IndexFromContainer(control) >= 0)
            {
                return control;
            }
        }
        return null;
    }

    /// <summary>The first bar of <paramref name="group"/> in <paramref name="window"/>.</summary>
    private static SelectingItemsControl? BarIn(Window window, string group) =>
        window.GetSelfAndLogicalDescendants().OfType<SelectingItemsControl>()
            .FirstOrDefault(bar => bar is TabStrip or TabControl && Tabs.GetDragGroup(bar) == group);

    /// <summary>The tabs in the grouped bars of <paramref name="window"/>.</summary>
    private static int TabCount(Window window) =>
        window.GetSelfAndLogicalDescendants().OfType<SelectingItemsControl>()
            .Where(bar => bar is TabStrip or TabControl && Tabs.GetDragGroup(bar) is not null)
            .Sum(bar => bar.ItemCount);

    /// <summary>A named part of <paramref name="bar"/>'s template.</summary>
    private static T? Part<T>(SelectingItemsControl bar, string name) where T : Control
    {
        var queue = new Queue<Visual>(bar.GetVisualChildren());
        while (queue.Count > 0)
        {
            var visual = queue.Dequeue();
            if (visual is not StyledElement element || element.TemplatedParent != bar)
            {
                continue;
            }
            if (visual is T part && part.Name == name)
            {
                return part;
            }
            foreach (var child in visual.GetVisualChildren())
            {
                queue.Enqueue(child);
            }
        }
        return null;
    }

    private sealed record Press(SelectingItemsControl Bar, IPointer Pointer, Control Tab, Point At);

    /// <summary>What a moved tab had before the drag, to put back.</summary>
    private sealed class Moved(bool transformSet, ITransform? transform, bool zIndexSet, int zIndex, bool noTransitions)
    {
        public bool TransformSet { get; } = transformSet;

        public ITransform? Transform { get; } = transform;

        public bool ZIndexSet { get; } = zIndexSet;

        public int ZIndex { get; } = zIndex;

        public bool NoTransitions { get; } = noTransitions;

        public TransformOperationsTransition? Transition { get; set; }
    }
}
