using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>Motion helpers the theme's templates attach to their parts.</summary>
public static class Motion
{
    /// <summary>
    /// Transitions that take effect once the element has been laid out for the
    /// first time. Values a control settles into while it loads (a progress
    /// bar's indicator width) then appear at once, as GPUI animates changes
    /// but not a component's first frame.
    /// </summary>
    public static readonly AttachedProperty<Transitions?> SettledTransitionsProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, Transitions?>("SettledTransitions", typeof(Motion));

    /// <summary>The spring that moves the element's <see cref="SpringValueProperty"/> or its Canvas.Left.</summary>
    public static readonly AttachedProperty<Spring?> SpringProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, Spring?>("Spring", typeof(Motion));

    /// <summary>Where the spring travels to. The first value is taken at once, as GPUI does.</summary>
    public static readonly AttachedProperty<double> SpringTargetProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, double>("SpringTarget", typeof(Motion), double.NaN);

    /// <summary>Where the spring is now: bind the animated property to it.</summary>
    public static readonly AttachedProperty<double> SpringValueProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, double>("SpringValue", typeof(Motion));

    /// <summary>
    /// Shows changes to the element's Canvas.Left through the spring: the
    /// control still places the element, and a translation keeps it where the
    /// spring is until the spring arrives.
    /// </summary>
    public static readonly AttachedProperty<bool> SpringsCanvasLeftProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, bool>("SpringsCanvasLeft", typeof(Motion));

    /// <summary>
    /// Whether the spring travels (true) or takes new targets at once, for a
    /// value the pointer is moving directly (a dragged switch thumb).
    /// </summary>
    public static readonly AttachedProperty<bool> SpringTravelProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, bool>("SpringTravel", typeof(Motion), true);

    /// <summary>Seconds since the running spring's clock started; animated on the element's clock.</summary>
    internal static readonly AttachedProperty<double> SpringClockProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, double>("SpringClock", typeof(Motion));

    private static readonly AttachedProperty<SpringRunner?> RunnerProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, SpringRunner?>("Runner", typeof(Motion));

    static Motion()
    {
        SettledTransitionsProperty.Changed.AddClassHandler<Layoutable>(OnSettledTransitionsChanged);
        SpringTargetProperty.Changed.AddClassHandler<Layoutable>((element, _) => Runner(element).Retarget(GetSpringTarget(element)));
        SpringsCanvasLeftProperty.Changed.AddClassHandler<Layoutable>(OnSpringsCanvasLeftChanged);
        SpringClockProperty.Changed.AddClassHandler<Layoutable>((element, e) => element.GetValue(RunnerProperty)?.OnClock(e.GetNewValue<double>()));
    }

    /// <summary>Gets the transitions <paramref name="element"/> takes on after its first layout.</summary>
    public static Transitions? GetSettledTransitions(Layoutable element) => element.GetValue(SettledTransitionsProperty);

    /// <summary>Sets the transitions <paramref name="element"/> takes on after its first layout.</summary>
    public static void SetSettledTransitions(Layoutable element, Transitions? value) => element.SetValue(SettledTransitionsProperty, value);

    /// <summary>Gets the element's spring.</summary>
    public static Spring? GetSpring(Layoutable element) => element.GetValue(SpringProperty);

    /// <summary>Sets the element's spring.</summary>
    public static void SetSpring(Layoutable element, Spring? value) => element.SetValue(SpringProperty, value);

    /// <summary>Gets where the element's spring travels to.</summary>
    public static double GetSpringTarget(Layoutable element) => element.GetValue(SpringTargetProperty);

    /// <summary>Sets where the element's spring travels to.</summary>
    public static void SetSpringTarget(Layoutable element, double value) => element.SetValue(SpringTargetProperty, value);

    /// <summary>Gets where the element's spring is now.</summary>
    public static double GetSpringValue(Layoutable element) => element.GetValue(SpringValueProperty);

    /// <summary>Sets where the element's spring is now (the spring does this).</summary>
    public static void SetSpringValue(Layoutable element, double value) => element.SetValue(SpringValueProperty, value);

    /// <summary>Gets whether Canvas.Left changes show through the spring.</summary>
    public static bool GetSpringsCanvasLeft(Layoutable element) => element.GetValue(SpringsCanvasLeftProperty);

    /// <summary>Sets whether Canvas.Left changes show through the spring.</summary>
    public static void SetSpringsCanvasLeft(Layoutable element, bool value) => element.SetValue(SpringsCanvasLeftProperty, value);

    /// <summary>Gets whether the spring travels to new targets.</summary>
    public static bool GetSpringTravel(Layoutable element) => element.GetValue(SpringTravelProperty);

    /// <summary>Sets whether the spring travels to new targets.</summary>
    public static void SetSpringTravel(Layoutable element, bool value) => element.SetValue(SpringTravelProperty, value);

    private static void OnSettledTransitionsChanged(Layoutable element, AvaloniaPropertyChangedEventArgs e) =>
        AfterFirstLayout(element, () => element.Transitions = GetSettledTransitions(element));

    private static void OnSpringsCanvasLeftChanged(Layoutable element, AvaloniaPropertyChangedEventArgs e)
    {
        if (!e.GetNewValue<bool>())
        {
            return;
        }
        var runner = Runner(element);
        runner.Retarget(Canvas.GetLeft(element));
        element.PropertyChanged += (_, change) =>
        {
            if (change.Property == Canvas.LeftProperty)
            {
                runner.Retarget(change.GetNewValue<double>());
            }
        };
    }

    private static SpringRunner Runner(Layoutable element)
    {
        if (element.GetValue(RunnerProperty) is { } runner)
        {
            return runner;
        }
        runner = new SpringRunner(element);
        element.SetValue(RunnerProperty, runner);
        return runner;
    }

    /// <summary>Runs <paramref name="action"/> now if the element has been laid out, else after its first layout.</summary>
    private static void AfterFirstLayout(Layoutable element, Action action)
    {
        if (element.IsArrangeValid && element.IsAttachedToVisualTree())
        {
            action();
            return;
        }
        element.LayoutUpdated += OnFirstLayout;

        void OnFirstLayout(object? sender, EventArgs args)
        {
            element.LayoutUpdated -= OnFirstLayout;
            action();
        }
    }

    /// <summary>
    /// Drives one element's spring. Time comes from an animation on the
    /// element's own clock, so the spring moves with every other animation.
    /// </summary>
    private sealed class SpringRunner
    {
        // Long enough for any spring to settle; it stops as soon as one does.
        private static readonly Animation Clock = new()
        {
            Duration = TimeSpan.FromSeconds(1000),
            Children =
            {
                new KeyFrame { Cue = new Cue(0), Setters = { new Setter(SpringClockProperty, 0d) } },
                new KeyFrame { Cue = new Cue(1), Setters = { new Setter(SpringClockProperty, 1000d) } },
            },
        };

        private readonly Layoutable _element;
        private bool _laidOut;
        private bool _initialized;
        private SpringState _start;
        private double _target;
        private double _startedAt;
        private double _now;
        private CancellationTokenSource? _running;
        private TranslateTransform? _offset;

        public SpringRunner(Layoutable element)
        {
            _element = element;
            AfterFirstLayout(element, () => _laidOut = true);
        }

        private Spring? Policy => GetSpring(_element);

        public void Retarget(double target)
        {
            if (double.IsNaN(target))
            {
                return;
            }
            if (!_initialized || !_laidOut || Policy is null || !GetSpringTravel(_element))
            {
                Adopt(target);
                return;
            }
            if (_running is not null)
            {
                _start = Current();
                _startedAt = _now;
                _target = target;
                Show(_start.Position);
                return;
            }
            if (_start.Position == target && _start.Velocity == 0)
            {
                _target = target;
                Show(target);
                return;
            }
            _target = target;
            _startedAt = 0;
            _now = 0;
            _running = new CancellationTokenSource();
            _ = Clock.RunAsync(_element, _running.Token);
            Show(_start.Position);
        }

        public void OnClock(double seconds)
        {
            if (_running is null)
            {
                return;
            }
            _now = seconds;
            var state = Current();
            if (Policy!.IsSettled(state, _target))
            {
                Adopt(_target);
                return;
            }
            Show(state.Position);
        }

        private SpringState Current() => Policy!.Step(_start, _target, _now - _startedAt);

        private void Adopt(double target)
        {
            _initialized = true;
            _target = target;
            _start = new SpringState(target, 0);
            var running = _running;
            _running = null;
            running?.Cancel();
            Show(target);
        }

        private void Show(double position)
        {
            if (GetSpringsCanvasLeft(_element))
            {
                _offset ??= new TranslateTransform();
                _element.RenderTransform ??= _offset;
                var left = Canvas.GetLeft(_element);
                // Snap to device pixels as layout does (and GPUI does with every edge).
                var scale = TopLevel.GetTopLevel(_element)?.RenderScaling ?? 1;
                _offset.X = double.IsNaN(left) ? 0 : Math.Round(position * scale) / scale - left;
            }
            else
            {
                SetSpringValue(_element, position);
            }
        }
    }
}
