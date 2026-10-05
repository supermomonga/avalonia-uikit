using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// The items panel of a <see cref="Carousel"/> with
/// <see cref="Carousels.TracksPointerProperty"/>: GPUI Kit's CarouselContent
/// (carousel.rs) and its scroll mask (scroll_mask.rs) over CarouselState
/// (state.rs). Every page is the viewport's size and the pages lie side by
/// side <see cref="Gap"/> apart on one track, which rides
/// <see cref="Spring"/> to the selected page. The axis is the carousel's
/// PageTransition's (a vertical PageSlide turns the track); with
/// WrapSelection the track loops, each page drawn in the cycle nearest the
/// viewport.
/// <para>
/// Input, as GPUI handles it: a left-button drag locks to the axis after
/// 2px (a drag across the axis gives the gesture up, and the press makes no
/// click), follows the pointer and selects the nearest page on release; a
/// wheel notch steps one page, the rest of its burst staying here; a
/// trackpad's smaller deltas move the track and snap once 28ms pass without
/// one. A horizontal track keeps every wheel event along its axis; a vertical
/// one hands a scroll that starts at an end to its parent.
/// </para>
/// </summary>
public class CarouselTrack : Panel, ILogicalScrollable
{
    /// <summary>The spring the track rides to the selected page (GPUI: spring_move with a 0.5px epsilon).</summary>
    public static readonly StyledProperty<Spring?> SpringProperty =
        AvaloniaProperty.Register<CarouselTrack, Spring?>(nameof(Spring));

    /// <summary>The space between two pages (GPUI: each item's 16px leading padding).</summary>
    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<CarouselTrack, double>(nameof(Gap), 16);

    private static readonly StyledProperty<double> ClockProperty =
        AvaloniaProperty.Register<CarouselTrack, double>("Clock");

    // Long enough for any spring to settle; it stops as soon as one does.
    private static readonly Animation Clock = new()
    {
        Duration = TimeSpan.FromSeconds(1000),
        Children =
        {
            new KeyFrame { Cue = new Cue(0), Setters = { new Setter(ClockProperty, 0d) } },
            new KeyFrame { Cue = new Cue(1), Setters = { new Setter(ClockProperty, 1000d) } },
        },
    };

    /// <summary>state.rs POINTER_AXIS_LOCK_THRESHOLD.</summary>
    private const double AxisLockThreshold = 2;

    /// <summary>state.rs SCROLL_EVENT_SEPARATION: the quiet that ends a wheel burst or a trackpad gesture.</summary>
    internal static readonly TimeSpan ScrollEventSeparation = TimeSpan.FromMilliseconds(28);

    /// <summary>Pixels per wheel line, as Avalonia's ScrollContentPresenter scrolls.</summary>
    private const double LineHeight = 50;

    private Carousel? _carousel;
    private int _selected = -1;
    private bool _laidOut;
    private Size _page;

    // The track's offset: page i rests at i * step - offset along the axis.
    private double _offset;
    private double _target;
    private SpringState _start;
    private double _startedAt;
    private double _now;
    private CancellationTokenSource? _running;

    private PointerGesture? _pointer;
    private bool _suppressClick;
    private TrackScroll? _scroll;
    private bool _ignoreScrollUntilQuiet;
    private int _settleEpoch;
    private bool _wheelBurstActive;
    private int _wheelBurstEpoch;

    /// <summary>Creates the track; it handles the pointer before the pages do, as GPUI's mask captures it.</summary>
    public CarouselTrack()
    {
        AddHandler(PointerPressedEvent, OnPointerPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerMovedEvent, OnPointerMovedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnPointerReleasedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerCaptureLostEvent, (_, e) => OnCaptureLost(e), RoutingStrategies.Direct);
    }

    /// <inheritdoc cref="SpringProperty"/>
    public Spring? Spring
    {
        get => GetValue(SpringProperty);
        set => SetValue(SpringProperty, value);
    }

    /// <inheritdoc cref="GapProperty"/>
    public double Gap
    {
        get => GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    /// <summary>How far the track has moved along its axis, in pixels from the first page.</summary>
    public double TrackOffset => _offset;

    /// <summary>Whether a drag or a trackpad gesture moves the track now.</summary>
    public bool IsInteracting => _pointer is not null || _scroll is not null;

    private bool IsHorizontal => !IsVertical(_carousel?.PageTransition);

    private bool IsLooping => _carousel is { WrapSelection: true } && Count > 1;

    private int Count => Children.Count;

    private double PageLength => IsHorizontal ? _page.Width : _page.Height;

    private double Step => PageLength + Gap;

    private double Cycle => Count * Step;

    private double MaxOffset => Math.Max(0, (Count - 1) * Step);

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _carousel = this.FindAncestorOfType<Carousel>();
        if (_carousel is not null)
        {
            _carousel.PropertyChanged += OnCarouselPropertyChanged;
            _selected = _carousel.SelectedIndex;
        }
        _laidOut = false;
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_carousel is not null)
        {
            _carousel.PropertyChanged -= OnCarouselPropertyChanged;
            _carousel = null;
        }
        StopSpring();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ClockProperty)
        {
            OnClock(change.GetNewValue<double>());
        }
        else if (change.Property == GapProperty)
        {
            InvalidateArrange();
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = new Size();
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            desired = new Size(Math.Max(desired.Width, child.DesiredSize.Width), Math.Max(desired.Height, child.DesiredSize.Height));
        }
        return new Size(
            double.IsInfinity(availableSize.Width) ? desired.Width : availableSize.Width,
            double.IsInfinity(availableSize.Height) ? desired.Height : availableSize.Height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var resized = _page != finalSize;
        _page = finalSize;
        if (!_laidOut || (resized && !IsInteracting))
        {
            // GPUI starts a new spring when the geometry changes: the track takes its place at once.
            StopSpring();
            _offset = _target = RestingOffset(_selected);
        }
        _laidOut = true;
        var horizontal = IsHorizontal;
        // A track that does not loop is a scroll container: GPUI's ScrollHandle keeps
        // its offset within the content, so a spring overshooting an end stops there.
        var shown = IsLooping ? _offset : Math.Clamp(_offset, 0, MaxOffset);
        for (var i = 0; i < Children.Count; i++)
        {
            var start = i * Step + LoopShift(i) - shown;
            Children[i].Arrange(horizontal
                ? new Rect(start, 0, finalSize.Width, finalSize.Height)
                : new Rect(0, start, finalSize.Width, finalSize.Height));
        }
        return finalSize;
    }

    /// <summary>
    /// Where page <paramref name="index"/> is drawn in a looping track
    /// (CarouselState::loop_item_offset): the cycle nearest the viewport.
    /// </summary>
    private double LoopShift(int index)
    {
        if (!IsLooping || Cycle <= 0)
        {
            return 0;
        }
        var cycles = Math.Clamp(Math.Round((_offset - index * Step) / Cycle), -1, 1);
        return cycles * Cycle;
    }

    private double RestingOffset(int index) => Math.Clamp(index, 0, Math.Max(0, Count - 1)) * Step;

    private void OnCarouselPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SelectingItemsControl.SelectedIndexProperty)
        {
            OnSelectedChanged(_selected, e.GetNewValue<int>());
        }
        else if (e.Property == Carousel.PageTransitionProperty || e.Property == SelectingItemsControl.WrapSelectionProperty)
        {
            CancelInteractions();
            Jump(RestingOffset(_selected));
            InvalidateArrange();
        }
    }

    private void OnSelectedChanged(int previous, int selected)
    {
        _selected = selected;
        CancelInteractions();
        if (selected < 0 || !_laidOut)
        {
            return;
        }
        var target = RestingOffset(selected);
        // state.rs adjacent_loop_target: wrapping past an end continues into the next cycle.
        if (IsLooping && previous == Count - 1 && selected == 0)
        {
            target = Cycle;
        }
        else if (IsLooping && previous == 0 && selected == Count - 1)
        {
            target = -Step;
        }
        SpringTo(target);
    }

    // ---- The spring (carousel.rs CarouselContent: spring_move toward the snap target) ----

    private void SpringTo(double target)
    {
        if (!_laidOut || Spring is null)
        {
            Jump(target);
            Settle();
            return;
        }
        if (_running is not null)
        {
            // gpui_base::spring keeps the velocity when the target moves.
            _start = Current();
            _startedAt = _now;
            _target = target;
            return;
        }
        _target = target;
        if (_offset == target)
        {
            Settle();
            return;
        }
        _start = new SpringState(_offset, 0);
        _startedAt = 0;
        _now = 0;
        _running = new CancellationTokenSource();
        _ = Clock.RunAsync(this, _running.Token);
    }

    private SpringState Current() => Spring!.Step(_start, _target, _now - _startedAt);

    private void OnClock(double seconds)
    {
        if (_running is null || Spring is null)
        {
            return;
        }
        _now = seconds;
        var state = Current();
        if (Spring.IsSettled(state, _target))
        {
            Jump(_target);
            Settle();
            return;
        }
        _offset = state.Position;
        InvalidateArrange();
    }

    private void Jump(double offset)
    {
        StopSpring();
        _offset = offset;
        _target = offset;
        InvalidateArrange();
    }

    /// <summary>Freezes the track where it is (a press stops GPUI's spring: it no longer travels).</summary>
    private void StopSpring()
    {
        var running = _running;
        _running = null;
        running?.Cancel();
    }

    /// <summary>A loop that settled in the next or previous cycle moves back to the same place in the middle one.</summary>
    private void Settle()
    {
        if (IsLooping && _laidOut)
        {
            var real = RestingOffset(_selected);
            if (Math.Abs(_offset - real) > 0.01 && Math.Abs(Math.Abs(_offset - real) - Cycle) < 0.5)
            {
                _offset = real;
                _target = real;
                InvalidateArrange();
            }
        }
    }

    /// <summary>The offset kept within the track's ends, or within one cycle of them when it loops.</summary>
    private double Clamped(double offset)
    {
        if (!IsLooping)
        {
            return Math.Clamp(offset, 0, MaxOffset);
        }
        // state.rs normalize_loop_coordinate: a whole cycle away looks the same.
        while (offset <= -Cycle)
        {
            offset += Cycle;
        }
        while (offset >= MaxOffset + Cycle)
        {
            offset -= Cycle;
        }
        return offset;
    }

    /// <summary>The page nearest the offset (state.rs nearest_index), across the loop's cycles.</summary>
    private int NearestIndex(double offset)
    {
        var best = -1;
        var bestDistance = double.MaxValue;
        for (var i = 0; i < Count; i++)
        {
            var distance = Math.Abs(offset - i * Step);
            if (IsLooping)
            {
                distance = Math.Min(distance, Math.Min(Math.Abs(offset - i * Step - Cycle), Math.Abs(offset - i * Step + Cycle)));
            }
            if (distance < bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }
        return best;
    }

    /// <summary>
    /// Ends a drag or a trackpad gesture (state.rs finish_snapshot): the
    /// nearest page is selected, or the track returns to the selected one.
    /// A looping drag that crossed a quarter page off an end wraps
    /// (loop_boundary_index).
    /// </summary>
    private void FinishSnapshot(int startIndex, double totalDelta)
    {
        var selected = NearestIndex(_offset);
        if (IsLooping && Math.Abs(totalDelta) >= Math.Max(1, PageLength * 0.25))
        {
            if (startIndex == 0 && totalDelta > 0)
            {
                selected = Count - 1;
            }
            else if (startIndex == Count - 1 && totalDelta < 0)
            {
                selected = 0;
            }
        }
        if (selected >= 0 && selected != _selected && _carousel is not null)
        {
            _carousel.SelectedIndex = selected;
            return;
        }
        var target = RestingOffset(_selected);
        if (IsLooping && Math.Abs(_offset - target) > Cycle / 2)
        {
            target += _offset > target ? Cycle : -Cycle;
        }
        SpringTo(target);
    }

    private void CancelInteractions()
    {
        var cancelledScroll = _scroll is not null;
        if (_pointer is { } pointer)
        {
            _pointer = null;
            if (pointer.Locked && pointer.Pointer.Captured == this)
            {
                pointer.Pointer.Capture(null);
            }
        }
        _scroll = null;
        _settleEpoch++;
        if (cancelledScroll)
        {
            _ignoreScrollUntilQuiet = true;
            ScheduleIgnoredScrollRecovery();
        }
    }

    // ---- The pointer (scroll_mask.rs mouse handlers, state.rs begin/update/finish_drag) ----

    private void OnPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        _suppressClick = false;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || Count < 2 || !_laidOut)
        {
            return;
        }
        StopSpring();
        var cancelledScroll = _scroll is not null;
        _scroll = null;
        _settleEpoch++;
        if (cancelledScroll)
        {
            _ignoreScrollUntilQuiet = true;
            ScheduleIgnoredScrollRecovery();
        }
        _pointer = new PointerGesture(e.Pointer, e.GetPosition(this), _offset, _selected);
    }

    private void OnPointerMovedTunnel(object? sender, PointerEventArgs e)
    {
        if (_pointer is not { } gesture || gesture.Pointer != e.Pointer)
        {
            return;
        }
        var delta = e.GetPosition(this) - gesture.StartPosition;
        var (primary, cross) = IsHorizontal ? (delta.X, delta.Y) : (delta.Y, delta.X);
        if (!gesture.Locked)
        {
            if (Math.Max(Math.Abs(primary), Math.Abs(cross)) <= AxisLockThreshold)
            {
                return;
            }
            if (Math.Abs(cross) > Math.Abs(primary))
            {
                // A drag across the axis belongs to an ancestor; the press makes no click.
                _pointer = null;
                _suppressClick = true;
                SpringTo(RestingOffset(_selected));
                return;
            }
            gesture.Locked = true;
            // The pages' controls give up the press: GPUI stops the release before them.
            e.Pointer.Capture(this);
        }
        gesture.TotalDelta = primary;
        var next = Clamped(gesture.StartOffset - primary);
        if (next != _offset)
        {
            _offset = next;
            InvalidateArrange();
        }
        e.Handled = true;
    }

    private void OnPointerReleasedTunnel(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
        {
            return;
        }
        var suppress = _suppressClick;
        _suppressClick = false;
        if (_pointer is { } gesture && gesture.Pointer == e.Pointer)
        {
            _pointer = null;
            suppress |= gesture.Locked;
            if (gesture.Locked && e.Pointer.Captured == this)
            {
                e.Pointer.Capture(null);
            }
            FinishSnapshot(gesture.StartIndex, gesture.TotalDelta);
        }
        if (suppress)
        {
            if (e.Pointer.Captured is { } captured && captured != this)
            {
                // The pressed control lets go without a click.
                e.Pointer.Capture(null);
            }
            e.Handled = true;
        }
    }

    private void OnCaptureLost(PointerCaptureLostEventArgs e)
    {
        if (_pointer is { Locked: true } gesture && gesture.Pointer == e.Pointer)
        {
            _pointer = null;
            FinishSnapshot(gesture.StartIndex, gesture.TotalDelta);
        }
    }

    // ---- The wheel (scroll_mask.rs ScrollWheelEvent, state.rs handle_wheel_step / handle_scroll_delta) ----

    /// <inheritdoc />
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (e.Handled || _carousel is null || !_laidOut)
        {
            return;
        }
        var delta = e.Delta;
        // As ScrollContentPresenter: Shift turns a vertical wheel sideways.
        if (e.KeyModifiers == KeyModifiers.Shift && delta.X == 0)
        {
            delta = new Vector(delta.Y, delta.X);
        }
        // Only the dominant axis counts, then only the track's.
        if (delta.X != 0 && delta.Y != 0)
        {
            delta = Math.Abs(delta.X) > Math.Abs(delta.Y) ? new Vector(delta.X, 0) : new Vector(0, delta.Y);
        }
        var horizontal = IsHorizontal;
        var primary = horizontal ? delta.X : delta.Y;
        if (primary == 0)
        {
            return;
        }
        // R-navigation-2: Avalonia's wheel event does not say whether its delta is
        // a wheel's lines or a trackpad's pixels; whole lines are taken as notches.
        var consumed = IsNotch(primary) ? WheelStep(primary) : ScrollBy(primary * LineHeight);
        // A horizontal track keeps every gesture along its axis, even at an end.
        if (consumed || horizontal)
        {
            e.Handled = true;
        }
    }

    private static bool IsNotch(double lines) => Math.Abs(lines) >= 1 && Math.Abs(lines - Math.Round(lines)) < 1e-6;

    /// <summary>One wheel notch (handle_wheel_step): the first event of a burst steps a page or hands the burst on.</summary>
    private bool WheelStep(double delta)
    {
        if (_ignoreScrollUntilQuiet)
        {
            ScheduleIgnoredScrollRecovery();
            return false;
        }
        if (_wheelBurstActive)
        {
            ScheduleWheelBurstEnd();
            return true;
        }
        var before = _carousel!.SelectedIndex;
        if (delta > 0)
        {
            _carousel.Previous();
        }
        else
        {
            _carousel.Next();
        }
        if (_carousel.SelectedIndex != before)
        {
            _wheelBurstActive = true;
            ScheduleWheelBurstEnd();
            return true;
        }
        _ignoreScrollUntilQuiet = true;
        ScheduleIgnoredScrollRecovery();
        return false;
    }

    /// <summary>
    /// A trackpad's precise delta (handle_scroll_delta): the track moves with
    /// it and snaps once the gesture goes quiet. A gesture that starts where
    /// the track cannot move belongs to an ancestor until it ends; one this
    /// track owns stays here even at an end.
    /// </summary>
    private bool ScrollBy(double delta)
    {
        if (Count < 2)
        {
            return false;
        }
        if (_ignoreScrollUntilQuiet)
        {
            ScheduleIgnoredScrollRecovery();
            return false;
        }
        var owned = _scroll is not null;
        if (_scroll is null)
        {
            StopSpring();
            _pointer = null;
            _scroll = new TrackScroll(_selected);
        }
        _scroll.TotalDelta += delta;
        var next = Clamped(_offset - delta);
        var moved = next != _offset;
        if (moved)
        {
            _offset = next;
            InvalidateArrange();
        }
        ScheduleScrollSettle();
        if (!moved && !owned && !IsLooping)
        {
            // state.rs defer_scroll_to_ancestor.
            _scroll = null;
            _settleEpoch++;
            _ignoreScrollUntilQuiet = true;
            ScheduleIgnoredScrollRecovery();
            return false;
        }
        return true;
    }

    private void ScheduleScrollSettle()
    {
        var epoch = ++_settleEpoch;
        DispatcherTimer.RunOnce(() =>
        {
            if (_settleEpoch == epoch && _scroll is { } gesture)
            {
                _scroll = null;
                FinishSnapshot(gesture.StartIndex, gesture.TotalDelta);
            }
        }, ScrollEventSeparation);
    }

    private void ScheduleIgnoredScrollRecovery()
    {
        var epoch = ++_settleEpoch;
        DispatcherTimer.RunOnce(() =>
        {
            if (_settleEpoch == epoch)
            {
                _ignoreScrollUntilQuiet = false;
            }
        }, ScrollEventSeparation);
    }

    private void ScheduleWheelBurstEnd()
    {
        var epoch = ++_wheelBurstEpoch;
        DispatcherTimer.RunOnce(() =>
        {
            if (_wheelBurstEpoch == epoch)
            {
                _wheelBurstActive = false;
            }
        }, ScrollEventSeparation);
    }

    private static bool IsVertical(IPageTransition? transition) => transition switch
    {
        PageSlide slide => slide.Orientation == PageSlide.SlideAxis.Vertical,
        CompositePageTransition composite => composite.PageTransitions.OfType<PageSlide>().FirstOrDefault()?.Orientation == PageSlide.SlideAxis.Vertical,
        _ => false,
    };

    private sealed class PointerGesture(IPointer pointer, Point startPosition, double startOffset, int startIndex)
    {
        public IPointer Pointer { get; } = pointer;
        public Point StartPosition { get; } = startPosition;
        public double StartOffset { get; } = startOffset;
        public int StartIndex { get; } = startIndex;
        public double TotalDelta { get; set; }
        public bool Locked { get; set; }
    }

    private sealed class TrackScroll(int startIndex)
    {
        public int StartIndex { get; } = startIndex;
        public double TotalDelta { get; set; }
    }

    // ---- ILogicalScrollable: the track scrolls itself, so the template's ScrollViewer has nothing to scroll. ----

    private EventHandler? _scrollInvalidated;

    bool ILogicalScrollable.CanHorizontallyScroll { get; set; }

    bool ILogicalScrollable.CanVerticallyScroll { get; set; }

    bool IScrollable.CanHorizontallyScroll => false;

    bool IScrollable.CanVerticallyScroll => false;

    bool ILogicalScrollable.IsLogicalScrollEnabled => true;

    Size ILogicalScrollable.ScrollSize => new(1, 1);

    Size ILogicalScrollable.PageScrollSize => new(1, 1);

    Size IScrollable.Extent => new(1, 1);

    Size IScrollable.Viewport => new(1, 1);

    Vector IScrollable.Offset
    {
        get => default;
        set { }
    }

    event EventHandler? ILogicalScrollable.ScrollInvalidated
    {
        add => _scrollInvalidated += value;
        remove => _scrollInvalidated -= value;
    }

    bool ILogicalScrollable.BringIntoView(Control target, Rect targetRect) => false;

    Control? ILogicalScrollable.GetControlInDirection(NavigationDirection direction, Control? from) => null;

    void ILogicalScrollable.RaiseScrollInvalidated(EventArgs e) => _scrollInvalidated?.Invoke(this, e);
}
