using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// One placement's notifications in a <see cref="NotificationList"/>: GPUI
/// Kit's ToastStack (base/toast.rs) with the shadcn/Sonner motion tokens. The
/// cards overlap with the newest in front and each older one 14px further out
/// and narrower by 5% of the width (up to two steps); a fourth and older are
/// hidden. Spread (pointer over the stack, or the focus in it), they stand
/// 14px apart at full width. Offsets, insets, the stack's height and the
/// cards' opacity follow GPUI's critically damped springs (400ms).
/// </summary>
internal sealed class NotificationStack : Panel
{
    // base/toast.rs ToastMotion::sonner.
    private const double CollapsedPeek = 14;
    private const double ExpandedGap = 14;
    private const double CollapsedScaleStep = 0.05;
    private const int CollapsedVisible = 3;

    // base/toast.rs ToastStack::render: geometry springs settle in pixels, the fade at the default tolerance.
    private static readonly Spring Geometry = new() { Response = TimeSpan.FromMilliseconds(400), Epsilon = 0.1 };
    private static readonly Spring Fade = new() { Response = TimeSpan.FromMilliseconds(400) };

    /// <summary>Seconds since the clock started; animated on the stack's own clock.</summary>
    private static readonly StyledProperty<double> ClockProperty =
        AvaloniaProperty.Register<NotificationStack, double>("Clock");

    // Long enough for any spring to settle; it stops as soon as they all do.
    private static readonly Animation ClockAnimation = new()
    {
        Duration = TimeSpan.FromSeconds(1000),
        Children =
        {
            new KeyFrame { Cue = new Cue(0), Setters = { new Setter(ClockProperty, 0d) } },
            new KeyFrame { Cue = new Cue(1), Setters = { new Setter(ClockProperty, 1000d) } },
        },
    };

    private readonly Dictionary<Control, Layer> _layers = [];
    private readonly SpringValue _height = new(Geometry);
    private CancellationTokenSource? _running;
    private double _clockBase;
    private double _now;
    private bool _hovered;
    private double _width;
    private double _collapsedHeight;

    public NotificationStack(NotificationPlacement placement)
    {
        Placement = placement;
        // notification.rs: each stack takes the focus (a tab stop), which spreads it.
        Focusable = true;
        FocusAdorner = null;
        // notification.rs: a card slides in from the top or bottom edge, or only fades in at the sides.
        Notifications.SetFromBottom(this, AnchoredBottom);
        Notifications.SetSlides(this, placement is not (NotificationPlacement.LeftCenter or NotificationPlacement.RightCenter));
        Children.CollectionChanged += (_, _) => RunClock();
    }

    public NotificationPlacement Placement { get; }

    /// <summary>Whether the pointer is over the stack or the focus is in it (ToastStackState::is_expanded).</summary>
    public bool IsExpanded => _hovered || IsKeyboardFocusWithin;

    private bool AnchoredBottom => Placement is NotificationPlacement.BottomLeft or NotificationPlacement.BottomCenter or NotificationPlacement.BottomRight;

    /// <summary>
    /// base/toast.rs: the stack is hovered while the pointer last moved within
    /// it; collapsed, only within the collapsed height (at the bottom when the
    /// stack is anchored there).
    /// </summary>
    public void PointerMovedTo(Point point)
    {
        var height = Bounds.Height;
        var area = IsExpanded
            ? new Rect(0, 0, Bounds.Width, height)
            : new Rect(0, AnchoredBottom ? height - _collapsedHeight : 0, Bounds.Width, _collapsedHeight);
        var hovered = area.Contains(point);
        if (hovered != _hovered)
        {
            _hovered = hovered;
            RunClock();
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ClockProperty)
        {
            // The clock's value falls back to 0 as it stops; time stands still meanwhile.
            if (_running is null)
            {
                return;
            }
            _now = _clockBase + change.GetNewValue<double>();
            InvalidateMeasure();
        }
        else if (change.Property == IsKeyboardFocusWithinProperty)
        {
            RunClock();
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        foreach (var gone in _layers.Keys.Where(c => !shown.Contains(c)).ToList())
        {
            _layers.Remove(gone);
        }
        var count = shown.Count;
        var expanded = IsExpanded;
        // ToastStack::render lays the stack out with the heights measured on the last frame.
        var heights = shown.Select(c => _layers.TryGetValue(c, out var layer) ? layer.Height : 0).ToArray();
        var (collapsedHeight, expandedHeight, offsets) = StackGeometry(heights, ExpandedGap, CollapsedPeek, AnchoredBottom);
        _collapsedHeight = collapsedHeight;
        var stackHeight = _height.Sample(expanded ? expandedHeight : collapsedHeight, _now);
        var moving = _height.IsMoving;
        var width = double.IsInfinity(availableSize.Width) ? _width : availableSize.Width;
        for (var index = 0; index < count; index++)
        {
            var child = shown[index];
            if (!_layers.TryGetValue(child, out var layer))
            {
                _layers[child] = layer = new Layer();
            }
            var rank = count - 1 - index;
            var (collapsed, spread) = offsets[index];
            layer.Offset = layer.OffsetSpring.Sample(expanded ? spread : collapsed, _now);
            layer.Inset = layer.InsetSpring.Sample(
                expanded ? 0 : _width * (CollapsedScaleStep * Math.Min(rank, CollapsedVisible - 1) / 2), _now);
            layer.Opacity = layer.OpacitySpring.Sample(expanded || rank < CollapsedVisible ? 1 : 0, _now);
            // ToastStack::render: a collapsed layer behind the visible ones is not painted at all.
            layer.Hidden = !expanded && rank >= CollapsedVisible;
            moving |= layer.OffsetSpring.IsMoving || layer.InsetSpring.IsMoving || layer.OpacitySpring.IsMoving;
            child.Measure(new Size(Math.Max(0, width - 2 * layer.Inset), double.PositiveInfinity));
            // Measured now, laid out with on the next frame, as ToastStack's on_prepaint does.
            if (layer.Height != child.DesiredSize.Height)
            {
                layer.Height = child.DesiredSize.Height;
                moving = true;
            }
        }
        if (moving)
        {
            StartClock();
        }
        else
        {
            StopClock();
        }
        return new Size(width, stackHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_width != finalSize.Width)
        {
            // ToastStack keeps the measured width for the next frame's insets.
            _width = finalSize.Width;
            StartClock();
        }
        foreach (var child in Children)
        {
            if (!_layers.TryGetValue(child, out var layer))
            {
                continue;
            }
            child.Opacity = layer.Hidden ? 0 : layer.Opacity;
            child.IsHitTestVisible = !layer.Hidden;
            child.Arrange(new Rect(layer.Inset, layer.Offset, Math.Max(0, finalSize.Width - 2 * layer.Inset), child.DesiredSize.Height));
        }
        return finalSize;
    }

    /// <summary>
    /// base/toast.rs stack_geometry: the collapsed and spread heights and each
    /// layer's (collapsed, spread) offset, oldest first.
    /// </summary>
    internal static (double Collapsed, double Expanded, (double Collapsed, double Expanded)[] Offsets) StackGeometry(
        IReadOnlyList<double> heights, double gap, double peek, bool anchoredBottom)
    {
        var count = heights.Count;
        var expandedHeight = heights.Sum() + gap * Math.Max(0, count - 1);
        var front = count > 0 ? heights[^1] : 0;
        var collapsedHeight = front + peek * Math.Max(0, count - 1);
        for (var index = 0; index < count; index++)
        {
            collapsedHeight = Math.Max(collapsedHeight, heights[index] + peek * (count - 1 - index));
        }
        var offsets = new (double, double)[count];
        for (var index = 0; index < count; index++)
        {
            var rank = count - 1 - index;
            var newer = 0d;
            for (var later = index + 1; later < count; later++)
            {
                newer += heights[later];
            }
            var expanded = anchoredBottom ? expandedHeight - newer - gap * rank - heights[index] : newer + gap * rank;
            var collapsed = anchoredBottom ? collapsedHeight - heights[index] - peek * rank : peek * rank;
            offsets[index] = (collapsed, expanded);
        }
        return (collapsedHeight, expandedHeight, offsets);
    }

    /// <summary>Lays the stack out again for a new target, on the clock until the springs settle.</summary>
    private void RunClock()
    {
        InvalidateMeasure();
        StartClock();
    }

    /// <summary>Runs the clock, which lays the stack out on every frame, so the springs and the measured heights catch up.</summary>
    private void StartClock()
    {
        if (_running is not null || !this.IsAttachedToVisualTree())
        {
            return;
        }
        _clockBase = _now;
        _running = new CancellationTokenSource();
        _ = ClockAnimation.RunAsync(this, _running.Token);
    }

    private void StopClock()
    {
        var running = _running;
        _running = null;
        _clockBase = _now;
        running?.Cancel();
    }

    /// <summary>A card's place in the stack, as ToastStack keeps it per item.</summary>
    private sealed class Layer
    {
        public SpringValue OffsetSpring { get; } = new(Geometry);
        public SpringValue InsetSpring { get; } = new(Geometry);
        public SpringValue OpacitySpring { get; } = new(Fade);
        public double Height { get; set; }
        public double Offset { get; set; }
        public double Inset { get; set; }
        public double Opacity { get; set; } = 1;
        public bool Hidden { get; set; }
    }

    /// <summary>
    /// base/motion.rs spring: the first target is taken at once; a later one is
    /// reached on the spring, keeping the value's position and velocity.
    /// </summary>
    private sealed class SpringValue(Spring spring)
    {
        private bool _initialized;
        private SpringState _start;
        private double _target;
        private double _startedAt;

        public bool IsMoving { get; private set; }

        public double Sample(double target, double now)
        {
            if (!_initialized || !IsMoving && _start.Position == target)
            {
                Adopt(target, now);
                return target;
            }
            var current = IsMoving ? spring.Step(_start, _target, now - _startedAt) : _start;
            if (spring.IsSettled(current, target))
            {
                Adopt(target, now);
                return target;
            }
            if (!IsMoving || target != _target)
            {
                _start = current;
                _startedAt = now;
                _target = target;
                IsMoving = true;
            }
            return current.Position;
        }

        private void Adopt(double target, double now)
        {
            _initialized = true;
            _start = new SpringState(target, 0);
            _target = target;
            _startedAt = now;
            IsMoving = false;
        }
    }
}
