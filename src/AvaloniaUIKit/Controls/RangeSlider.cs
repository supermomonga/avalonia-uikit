using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>How a slider maps positions to values (GPUI's SliderScale).</summary>
public enum SliderScale
{
    /// <summary>Values change evenly along the track (the default).</summary>
    Linear,

    /// <summary>
    /// Each part of the track covers the same ratio: from 1 to 1000, a third of
    /// the way is 10 and two thirds 100. Needs Minimum above 0 and Maximum above
    /// Minimum; otherwise the slider is linear.
    /// </summary>
    Logarithmic,
}

/// <summary>
/// GPUI Kit's Slider with a range value (crates/component/src/slider.rs,
/// crates/base/src/slider.rs): two thumbs, <see cref="StartValue"/> and
/// <see cref="EndValue"/>, with the track filled between them. It has the
/// themed Slider's look: the bar, the fill, 16px thumbs and the ring that
/// springs around a hovered or dragged thumb.
/// </summary>
/// <remarks>
/// <para>
/// A press on the track moves the nearer thumb there; a thumb is dragged
/// directly. A thumb stops at the other one. Values are rounded to
/// <see cref="Step"/>; the thumb stays under the pointer while it is dragged
/// (GPUI keeps the pointer's position, not the rounded value's).
/// <see cref="Changed"/> is raised while the user changes the values and
/// <see cref="Released"/> once the pointer is released. Disabled, the thumbs
/// are not drawn.
/// </para>
/// <para>
/// With <see cref="IsRange"/> false the slider has one thumb, at
/// <see cref="EndValue"/>, filled from the start (or, with the reverse class,
/// to the end), and a press on the track can be dragged on. This is how a
/// single value takes a <see cref="Scale"/>, which Avalonia's Slider does not
/// have. GPUI's slider takes no keyboard focus.
/// </para>
/// </remarks>
[TemplatePart("PART_Track", typeof(RangeSliderTrack))]
[TemplatePart("PART_StartThumb", typeof(Thumb))]
[TemplatePart("PART_EndThumb", typeof(Thumb))]
[PseudoClasses(":horizontal", ":vertical", ":range", ":start-active", ":end-active")]
public class RangeSlider : TemplatedControl
{
    /// <summary>The smallest value, 0 by default.</summary>
    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(Minimum));

    /// <summary>The largest value, 100 by default.</summary>
    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(Maximum), 100);

    /// <summary>The step values are rounded to, 1 by default.</summary>
    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(Step), 1);

    /// <summary>The start of the range.</summary>
    public static readonly StyledProperty<double> StartValueProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(StartValue), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The end of the range, or the value of a single-value slider.</summary>
    public static readonly StyledProperty<double> EndValueProperty =
        AvaloniaProperty.Register<RangeSlider, double>(nameof(EndValue), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Whether the slider has two thumbs (true, the default) or one at <see cref="EndValue"/>.</summary>
    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<RangeSlider, bool>(nameof(IsRange), true);

    /// <summary>Horizontal (the default), or vertical with the start at the bottom.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<RangeSlider, Orientation>(nameof(Orientation));

    /// <summary>How positions map to values (GPUI's scale()).</summary>
    public static readonly StyledProperty<SliderScale> ScaleProperty =
        AvaloniaProperty.Register<RangeSlider, SliderScale>(nameof(Scale));

    /// <summary>Where the start thumb is, from 0 at the track's start to 1 at its end.</summary>
    public static readonly DirectProperty<RangeSlider, double> StartRatioProperty =
        AvaloniaProperty.RegisterDirect<RangeSlider, double>(nameof(StartRatio), o => o.StartRatio);

    /// <summary>Where the end thumb is, from 0 at the track's start to 1 at its end.</summary>
    public static readonly DirectProperty<RangeSlider, double> EndRatioProperty =
        AvaloniaProperty.RegisterDirect<RangeSlider, double>(nameof(EndRatio), o => o.EndRatio);

    /// <summary>Raised while the user changes the values (GPUI's SliderEvent::Change).</summary>
    public static readonly RoutedEvent<RangeSliderEventArgs> ChangedEvent =
        RoutedEvent.Register<RangeSlider, RangeSliderEventArgs>(nameof(Changed), RoutingStrategies.Bubble);

    /// <summary>Raised once when the pointer is released after a press or a drag (GPUI's SliderEvent::Release).</summary>
    public static readonly RoutedEvent<RangeSliderEventArgs> ReleasedEvent =
        RoutedEvent.Register<RangeSlider, RangeSliderEventArgs>(nameof(Released), RoutingStrategies.Bubble);

    private RangeSliderTrack? _track;
    private Thumb? _startThumb;
    private Thumb? _endThumb;
    private double _startRatio;
    private double _endRatio;
    // While the pointer sets the values: the ratios follow the pointer, not the rounded values.
    private bool _updatingFromPointer;
    // Which thumb the pointer moves (true for the start), and whether it has moved one.
    private bool? _dragging;
    private bool _changed;
    private Point _lastPoint;

    static RangeSlider()
    {
        FocusableProperty.OverrideDefaultValue<RangeSlider>(false);
        foreach (var property in new AvaloniaProperty[] { MinimumProperty, MaximumProperty, StartValueProperty, EndValueProperty, IsRangeProperty, ScaleProperty })
        {
            property.Changed.AddClassHandler<RangeSlider>((slider, _) => slider.UpdateRatios());
        }
        OrientationProperty.Changed.AddClassHandler<RangeSlider>((slider, _) => slider.UpdatePseudoClasses());
        IsRangeProperty.Changed.AddClassHandler<RangeSlider>((slider, _) => slider.UpdatePseudoClasses());
    }

    /// <summary>Creates a horizontal range slider.</summary>
    public RangeSlider()
    {
        UpdatePseudoClasses();
        // A thumb takes its press; the slider still notes where it was.
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc cref="MinimumProperty"/>
    public double Minimum { get => GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }

    /// <inheritdoc cref="MaximumProperty"/>
    public double Maximum { get => GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

    /// <inheritdoc cref="StepProperty"/>
    public double Step { get => GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <inheritdoc cref="StartValueProperty"/>
    public double StartValue { get => GetValue(StartValueProperty); set => SetValue(StartValueProperty, value); }

    /// <inheritdoc cref="EndValueProperty"/>
    public double EndValue { get => GetValue(EndValueProperty); set => SetValue(EndValueProperty, value); }

    /// <inheritdoc cref="IsRangeProperty"/>
    public bool IsRange { get => GetValue(IsRangeProperty); set => SetValue(IsRangeProperty, value); }

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <inheritdoc cref="ScaleProperty"/>
    public SliderScale Scale { get => GetValue(ScaleProperty); set => SetValue(ScaleProperty, value); }

    /// <inheritdoc cref="StartRatioProperty"/>
    public double StartRatio
    {
        get => _startRatio;
        private set => SetAndRaise(StartRatioProperty, ref _startRatio, value);
    }

    /// <inheritdoc cref="EndRatioProperty"/>
    public double EndRatio
    {
        get => _endRatio;
        private set => SetAndRaise(EndRatioProperty, ref _endRatio, value);
    }

    /// <inheritdoc cref="ChangedEvent"/>
    public event EventHandler<RangeSliderEventArgs>? Changed
    {
        add => AddHandler(ChangedEvent, value);
        remove => RemoveHandler(ChangedEvent, value);
    }

    /// <inheritdoc cref="ReleasedEvent"/>
    public event EventHandler<RangeSliderEventArgs>? Released
    {
        add => AddHandler(ReleasedEvent, value);
        remove => RemoveHandler(ReleasedEvent, value);
    }

    private bool IsLogarithmic => Scale == SliderScale.Logarithmic && Minimum > 0 && Maximum > Minimum;

    /// <summary>slider.rs percentage_to_value: the value at <paramref name="ratio"/> of the track.</summary>
    public double ValueAt(double ratio) => IsLogarithmic
        ? Math.Clamp(Math.Pow(Maximum / Minimum, ratio) * Minimum, Minimum, Maximum)
        : Minimum + (Maximum - Minimum) * ratio;

    /// <summary>slider.rs value_to_percentage: where <paramref name="value"/> is on the track, from 0 to 1.</summary>
    public double RatioOf(double value)
    {
        if (IsLogarithmic)
        {
            return Math.Clamp(Math.Log(value / Minimum, Maximum / Minimum), 0, 1);
        }
        var range = Maximum - Minimum;
        return range <= 0 ? 0 : (value - Minimum) / range;
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        foreach (var thumb in new[] { _startThumb, _endThumb })
        {
            if (thumb is not null)
            {
                thumb.DragStarted -= OnDragStarted;
                thumb.DragCompleted -= OnDragCompleted;
            }
        }
        _track = e.NameScope.Find<RangeSliderTrack>("PART_Track");
        _startThumb = e.NameScope.Find<Thumb>("PART_StartThumb");
        _endThumb = e.NameScope.Find<Thumb>("PART_EndThumb");
        foreach (var thumb in new[] { _startThumb, _endThumb })
        {
            if (thumb is not null)
            {
                thumb.DragStarted += OnDragStarted;
                thumb.DragCompleted += OnDragCompleted;
            }
        }
        UpdateRatios();
    }

    // slider.rs SliderThumb: a thumb's drag moves its own value.
    private void OnDragStarted(object? sender, VectorEventArgs e) => _dragging = ReferenceEquals(sender, _startThumb);

    private void OnDragCompleted(object? sender, VectorEventArgs e) => EndPress();

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Handled || _track is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }
        // slider.rs SliderTrack: a press moves the nearer thumb of a range there.
        var along = Along(e.GetPosition(_track));
        var start = IsRange && along < (StartRatio + (EndRatio - StartRatio) / 2) * Length;
        MoveTo(along, start);
        PseudoClasses.Set(start ? ":start-active" : ":end-active", true);
        // Only a single-value slider follows a drag that starts on the track.
        _dragging = IsRange ? null : false;
        _changed = true;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        // A drag moves the value only where the pointer has moved since the press.
        if (_dragging is { } start && _track is not null && IsEffectivelyEnabled && e.GetPosition(_track) is var point && point != _lastPoint)
        {
            _lastPoint = point;
            MoveTo(Along(point), start);
            _changed = true;
        }
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_track is not null)
        {
            _lastPoint = e.GetPosition(_track);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndPress();
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        // slider.rs on_mouse_up_out: a press that ends elsewhere ends too.
        EndPress();
    }

    private void EndPress()
    {
        _dragging = null;
        PseudoClasses.Set(":start-active", false);
        PseudoClasses.Set(":end-active", false);
        if (_changed)
        {
            // slider.rs handle_release: once, after a real press or drag.
            _changed = false;
            RaiseEvent(new RangeSliderEventArgs(ReleasedEvent, StartValue, EndValue));
        }
    }

    private double Length => _track is null ? 0 : Orientation == Orientation.Horizontal ? _track.Bounds.Width : _track.Bounds.Height;

    /// <summary>The distance from the track's start: from the left, or up from the bottom.</summary>
    private double Along(Point point) => Orientation == Orientation.Horizontal ? point.X : Length - point.Y;

    /// <summary>slider.rs update_value_by_position.</summary>
    private void MoveTo(double along, bool start)
    {
        var length = Length;
        if (length <= 0)
        {
            return;
        }
        var ratio = Math.Clamp(along, 0, length) / length;
        ratio = start ? Math.Clamp(ratio, 0, EndRatio) : Math.Clamp(ratio, IsRange ? StartRatio : 0, 1);
        var value = ValueAt(ratio);
        if (Step > 0)
        {
            value = Math.Round(value / Step, MidpointRounding.AwayFromZero) * Step;
        }
        _updatingFromPointer = true;
        try
        {
            if (start)
            {
                StartRatio = ratio;
                SetCurrentValue(StartValueProperty, Math.Min(value, EndValue));
            }
            else
            {
                EndRatio = ratio;
                SetCurrentValue(EndValueProperty, IsRange ? Math.Max(value, StartValue) : value);
            }
        }
        finally
        {
            _updatingFromPointer = false;
        }
        RaiseEvent(new RangeSliderEventArgs(ChangedEvent, StartValue, EndValue));
    }

    /// <summary>slider.rs update_thumb_pos: the thumbs at the values, clamped to the range.</summary>
    private void UpdateRatios()
    {
        if (_updatingFromPointer)
        {
            return;
        }
        var (min, max) = (Math.Min(Minimum, Maximum), Math.Max(Minimum, Maximum));
        StartRatio = IsRange ? RatioOf(Math.Clamp(StartValue, min, max)) : 0;
        EndRatio = RatioOf(Math.Clamp(EndValue, min, max));
    }

    private void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":horizontal", Orientation == Orientation.Horizontal);
        PseudoClasses.Set(":vertical", Orientation == Orientation.Vertical);
        PseudoClasses.Set(":range", IsRange);
    }
}

/// <summary>The values of a <see cref="RangeSlider"/> when it raised an event.</summary>
public sealed class RangeSliderEventArgs : RoutedEventArgs
{
    /// <summary>Creates the arguments.</summary>
    public RangeSliderEventArgs(RoutedEvent routedEvent, double startValue, double endValue) : base(routedEvent)
    {
        StartValue = startValue;
        EndValue = endValue;
    }

    /// <summary>The start of the range.</summary>
    public double StartValue { get; }

    /// <summary>The end of the range, or the single value.</summary>
    public double EndValue { get; }
}

/// <summary>
/// The track of a <see cref="RangeSlider"/>: the bar's extent. It lays the
/// fill (PART_Fill) between the ratios across its middle and centers each
/// thumb (PART_StartThumb, PART_EndThumb) on its ratio; other children fill it.
/// </summary>
public class RangeSliderTrack : Panel
{
    /// <summary>The direction of the track.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<RangeSliderTrack, Orientation>(nameof(Orientation));

    /// <summary>Where the start thumb is, from 0 to 1.</summary>
    public static readonly StyledProperty<double> StartRatioProperty =
        AvaloniaProperty.Register<RangeSliderTrack, double>(nameof(StartRatio));

    /// <summary>Where the end thumb is, from 0 to 1.</summary>
    public static readonly StyledProperty<double> EndRatioProperty =
        AvaloniaProperty.Register<RangeSliderTrack, double>(nameof(EndRatio));

    /// <summary>Whether the fill runs from the end thumb to the track's end (a single value's reverse).</summary>
    public static readonly StyledProperty<bool> IsFillReversedProperty =
        AvaloniaProperty.Register<RangeSliderTrack, bool>(nameof(IsFillReversed));

    static RangeSliderTrack()
    {
        AffectsArrange<RangeSliderTrack>(OrientationProperty, StartRatioProperty, EndRatioProperty, IsFillReversedProperty);
    }

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <inheritdoc cref="StartRatioProperty"/>
    public double StartRatio { get => GetValue(StartRatioProperty); set => SetValue(StartRatioProperty, value); }

    /// <inheritdoc cref="EndRatioProperty"/>
    public double EndRatio { get => GetValue(EndRatioProperty); set => SetValue(EndRatioProperty, value); }

    /// <inheritdoc cref="IsFillReversedProperty"/>
    public bool IsFillReversed { get => GetValue(IsFillReversedProperty); set => SetValue(IsFillReversedProperty, value); }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = new Size();
        foreach (var child in Children)
        {
            child.Measure(Size.Infinity);
            if (child.Name is not ("PART_StartThumb" or "PART_EndThumb" or "PART_Fill"))
            {
                size = new Size(Math.Max(size.Width, child.DesiredSize.Width), Math.Max(size.Height, child.DesiredSize.Height));
            }
        }
        return size;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var length = horizontal ? finalSize.Width : finalSize.Height;
        var across = horizontal ? finalSize.Height : finalSize.Width;
        // A rectangle from `from` to `to` along the track (from the bottom when vertical), `thickness`
        // across its middle; GPUI rounds each edge to the nearest device pixel.
        Rect Span(double from, double to, double thickness)
        {
            (from, to) = (LayoutSnap.Edge(this, from), LayoutSnap.Edge(this, to));
            var offset = (across - thickness) / 2;
            return horizontal
                ? new Rect(from, offset, Math.Max(0, to - from), thickness)
                : new Rect(offset, length - to, thickness, Math.Max(0, to - from));
        }
        foreach (var child in Children)
        {
            var desired = child.DesiredSize;
            var thickness = horizontal ? desired.Height : desired.Width;
            switch (child.Name)
            {
                case "PART_Fill":
                    var (from, to) = IsFillReversed ? (EndRatio, 1.0) : (StartRatio, EndRatio);
                    child.Arrange(Span(from * length, to * length, thickness));
                    break;
                case "PART_StartThumb" or "PART_EndThumb":
                    var center = (child.Name == "PART_StartThumb" ? StartRatio : EndRatio) * length;
                    var extent = horizontal ? desired.Width : desired.Height;
                    child.Arrange(Span(center - extent / 2, center + extent / 2, thickness));
                    break;
                default:
                    child.Arrange(new Rect(finalSize));
                    break;
            }
        }
        return finalSize;
    }
}
