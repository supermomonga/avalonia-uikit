using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Motion;

/// <summary>A motion the theme declares, reduced to its values over time.</summary>
public interface IDeclaredMotion
{
    /// <summary>How long the motion runs before it rests (zero for a repeating one).</summary>
    TimeSpan Settle { get; }

    /// <summary>The animated quantities <paramref name="t"/> after the motion starts.</summary>
    double[] ValueAt(TimeSpan t);
}

/// <summary>A property transition: from -> to over the duration with the easing.</summary>
public sealed record DeclaredTransition(TimeSpan Duration, Easing Easing, double From, double To) : IDeclaredMotion
{
    public TimeSpan Settle => Duration;

    public double[] ValueAt(TimeSpan t)
    {
        var progress = Duration <= TimeSpan.Zero ? 1 : Math.Clamp(t / Duration, 0, 1);
        return [progress >= 1 ? To : From + (To - From) * Easing.Ease(progress)];
    }
}

/// <summary>
/// A repeating two-keyframe animation as Avalonia runs it: after <c>Delay</c>,
/// each iteration of <c>Duration</c> eases from <c>From</c> to <c>To</c>, then
/// <c>DelayBetweenIterations</c> passes with the property back at its base value.
/// </summary>
public sealed record DeclaredLoop(Animation Animation, double From, double To, double BaseValue)
{
    public static DeclaredLoop Of(Animation animation, double from, double to, double baseValue)
    {
        if (animation.Children.Count != 2 || animation.Children[0].Cue.CueValue != 0 || animation.Children[1].Cue.CueValue != 1)
        {
            throw new InvalidOperationException("expected a two-keyframe animation from 0% to 100%");
        }
        if (animation.IterationCount != IterationCount.Infinite)
        {
            throw new InvalidOperationException("expected a repeating animation");
        }
        return new DeclaredLoop(animation, from, to, baseValue);
    }

    public double ValueAt(TimeSpan t)
    {
        var local = t - Animation.Delay;
        if (local < TimeSpan.Zero)
        {
            return BaseValue;
        }
        var period = Animation.Duration + Animation.DelayBetweenIterations;
        var inPeriod = TimeSpan.FromTicks(local.Ticks % period.Ticks);
        if (inPeriod >= Animation.Duration)
        {
            return BaseValue;
        }
        var progress = inPeriod / Animation.Duration;
        return From + (To - From) * Animation.Easing.Ease(progress);
    }
}

/// <summary>
/// A repeating keyframe animation without delays, as Avalonia's animator runs it:
/// the animation's easing on the cycle's progress, then the keyframes' values
/// interpolated with the KeySpline of the keyframe each segment ends at.
/// </summary>
public sealed record DeclaredKeyframes(Animation Animation, double[] Values)
{
    public static DeclaredKeyframes Of(Animation animation, params double[] values)
    {
        if (animation.Children.Count != values.Length)
        {
            throw new InvalidOperationException($"expected {values.Length} keyframes, the theme declares {animation.Children.Count}");
        }
        if (animation.Delay != TimeSpan.Zero || animation.DelayBetweenIterations != TimeSpan.Zero || animation.IterationCount != IterationCount.Infinite)
        {
            throw new InvalidOperationException("expected a repeating animation without delays");
        }
        return new DeclaredKeyframes(animation, values);
    }

    public double ValueAt(TimeSpan t)
    {
        var progress = Animation.Easing.Ease(TimeSpan.FromTicks(t.Ticks % Animation.Duration.Ticks) / Animation.Duration);
        var frames = Animation.Children;
        for (var i = 1; i < frames.Count; i++)
        {
            var from = frames[i - 1].Cue.CueValue;
            var to = frames[i].Cue.CueValue;
            if (progress <= to || i == frames.Count - 1)
            {
                var local = to > from ? Math.Clamp((progress - from) / (to - from), 0, 1) : 1;
                if (frames[i].KeySpline is { } spline)
                {
                    local = spline.GetSplineProgress(local);
                }
                return Values[i - 1] + (Values[i] - Values[i - 1]) * local;
            }
        }
        return Values[^1];
    }
}

/// <summary>
/// How one GPUI motion maps onto the Avalonia theme: the animated quantities in
/// GPUI's frames, the motion the theme declares for them, and how to pose a
/// control at given values with motion frozen.
/// </summary>
public interface IMotionProbe
{
    double[] FromGpui(GoldenScene scene, GoldenCase golden);

    /// <summary>The motion the theme declares. Read before motion is frozen.</summary>
    IDeclaredMotion Declared(CaseHost host, GoldenCase golden);

    void Apply(CaseHost host, double[] values);

    /// <summary>The quantities of a live control, as an animation is running.</summary>
    double[] Live(CaseHost host);

    double Tolerance { get; }

    /// <summary>Pixel limits for a frame posed at <paramref name="values"/>, when a relaxation applies.</summary>
    Comparison.PixelTolerance? PixelToleranceFor(CaseHost host, double[] values) => null;
}

public static class MotionProbes
{
    public static IMotionProbe For(GoldenCase golden) => (golden.Component, golden.Motion!.Name) switch
    {
        ("switch", _) => new SwitchThumb(),
        ("checkbox", _) => new CheckMark(),
        ("progress", "value") => new ProgressValue(),
        ("progress", "loading") => new ProgressLoading(),
        ("spinner", _) => new SpinnerTurn(),
        ("tooltip", _) => new TooltipEnter(),
        _ => throw new NotSupportedException($"no motion probe for {golden.Id}"),
    };

    public static bool Has(GoldenCase golden) =>
        golden.Component is "switch" or "checkbox" or "progress" or "spinner" or "tooltip";

    public static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static DeclaredTransition FromTransitions(Transitions? transitions, AvaloniaProperty property, double from, double to)
    {
        var transition = transitions?.OfType<DoubleTransition>().FirstOrDefault(t => t.Property == property)
            ?? throw new InvalidOperationException($"the theme declares no transition for {property.Name}");
        return new DeclaredTransition(transition.Duration, transition.Easing, from, to);
    }

    /// <summary>The animation a control theme's nested style runs on a template part.</summary>
    private static Animation ThemeAnimation(Control control, string partName)
    {
        var theme = control.Theme ?? (ControlTheme)control.FindResource(control.GetType())!;
        foreach (var style in Flatten(theme))
        {
            if (style is Style s && s.Selector?.ToString()?.Contains($"#{partName}", StringComparison.Ordinal) == true && s.Animations.Count > 0)
            {
                return (Animation)s.Animations[0];
            }
        }
        throw new InvalidOperationException($"the theme declares no animation on {partName}");
    }

    private static IEnumerable<IStyle> Flatten(IStyle style)
    {
        yield return style;
        if (style is StyleBase b)
        {
            foreach (var child in b.Children.SelectMany(Flatten))
            {
                yield return child;
            }
        }
    }

    private sealed class SwitchThumb : IMotionProbe
    {
        // GPUI snaps the thumb quad to device pixels (half a logical pixel).
        public double Tolerance => 0.26;

        public double[] FromGpui(GoldenScene scene, GoldenCase golden)
        {
            var thumb = golden.Str("size", "medium") switch { "xsmall" or "small" => 12.0, "large" => 20.0, _ => 16.0 };
            var quad = scene.Quads.Where(q => q.SolidBackground && Math.Abs(q.Bounds.Width - thumb) < 0.01 && Math.Abs(q.Bounds.Height - thumb) < 0.01)
                .OrderByDescending(q => q.Order).First();
            return [quad.Bounds.X - (golden.ComponentBounds.X + 2)];
        }

        public IDeclaredMotion Declared(CaseHost host, GoldenCase golden)
        {
            var toggle = (ToggleSwitch)host.Control;
            var travel = Part<Canvas>(host.Window, "PART_SwitchKnob").Bounds.Width;
            var on = !golden.Bool("checked");
            return FromTransitions(toggle.KnobTransitions, Canvas.LeftProperty, on ? 0 : travel, on ? travel : 0);
        }

        public void Apply(CaseHost host, double[] values) =>
            Canvas.SetLeft(Part<Panel>(host.Window, "PART_MovingKnobs"), values[0]);

        public double[] Live(CaseHost host) => [Canvas.GetLeft(Part<Panel>(host.Window, "PART_MovingKnobs"))];
    }

    private sealed class CheckMark : IMotionProbe
    {
        public double Tolerance => 0.002;

        public double[] FromGpui(GoldenScene scene, GoldenCase golden)
        {
            var box = new Rect(golden.ComponentBounds.X, golden.ComponentBounds.Y, 20, golden.ComponentBounds.Height);
            var marks = scene.Sprites.Where(s => box.Contains(s.Bounds.Center)).ToList();
            return [marks.Count == 0 ? 0 : marks.Max(s => s.Color.A)];
        }

        public IDeclaredMotion Declared(CaseHost host, GoldenCase golden)
        {
            var on = !golden.Bool("checked");
            return FromTransitions(Part<PathIcon>(host.Window, "PART_Mark").Transitions, Visual.OpacityProperty, on ? 0 : 1, on ? 1 : 0);
        }

        public void Apply(CaseHost host, double[] values) => Part<PathIcon>(host.Window, "PART_Mark").Opacity = values[0];

        public double[] Live(CaseHost host) => [Part<PathIcon>(host.Window, "PART_Mark").Opacity];
    }

    /// <summary>progress.rs: the indicator's width after a value change (duration_normal, easing_move).</summary>
    private sealed class ProgressValue : IMotionProbe
    {
        public double Tolerance => 0.26;

        public double[] FromGpui(GoldenScene scene, GoldenCase golden)
        {
            var bar = golden.ComponentBounds;
            var indicator = scene.Quads.Where(q => q.SolidBackground && q.Background.A > 0.9 && Math.Abs(q.Bounds.Height - bar.Height) < 0.01 && q.Bounds.Width < bar.Width - 0.01)
                .OrderByDescending(q => q.Order).FirstOrDefault();
            return [indicator?.Bounds.Width ?? 0];
        }

        public IDeclaredMotion Declared(CaseHost host, GoldenCase golden)
        {
            var width = host.Control.Bounds.Width;
            return FromTransitions(Part<Border>(host.Window, "PART_Indicator").Transitions, Avalonia.Layout.Layoutable.WidthProperty,
                width * golden.Num("value", 0) / 100, width * golden.Num("value_to", 0) / 100);
        }

        // ProgressBar sizes its indicator from Value on every arrange, so the pose is a value.
        public void Apply(CaseHost host, double[] values)
        {
            var progress = (ProgressBar)host.Control;
            progress.Value = values[0] / progress.Bounds.Width * 100;
        }

        public double[] Live(CaseHost host) => [Part<Border>(host.Window, "PART_Indicator").Width];
    }

    /// <summary>progress.rs `loading`: the bar's left and right edges over the 1s cycle.</summary>
    private sealed class ProgressLoading : IMotionProbe
    {
        public double Tolerance => 0.26;

        public double[] FromGpui(GoldenScene scene, GoldenCase golden)
        {
            var bar = golden.ComponentBounds;
            var indicator = scene.Quads.Where(q => q.SolidBackground && q.Background.A > 0.9 && Math.Abs(q.Bounds.Height - bar.Height) < 0.01 && q.Bounds.Width < bar.Width - 0.01)
                .OrderByDescending(q => q.Order).FirstOrDefault();
            return indicator is null ? [0, 0] : [indicator.Bounds.Left - bar.X, indicator.Bounds.Right - bar.X];
        }

        public IDeclaredMotion Declared(CaseHost host, GoldenCase golden)
        {
            var progress = (ProgressBar)host.Control;
            var w = progress.TemplateSettings.IndeterminateEndingOffset;
            var bar = DeclaredLoop.Of(ThemeAnimation(progress, "PART_IndeterminateBar"), -w, 0, -w);
            var clip = DeclaredKeyframes.Of(ThemeAnimation(progress, "PART_IndeterminateClip"), w, w, 0.6 * w, 0.4 * w, 0);
            return new Loading(bar, clip, w);
        }

        public void Apply(CaseHost host, double[] values)
        {
            var w = ((ProgressBar)host.Control).TemplateSettings.IndeterminateEndingOffset;
            Part<Border>(host.Window, "PART_IndeterminateClip").Width = w - values[0];
            Translate(host, "PART_IndeterminateBar").X = values[1] - w;
        }

        public double[] Live(CaseHost host)
        {
            var w = ((ProgressBar)host.Control).TemplateSettings.IndeterminateEndingOffset;
            return [w - Part<Border>(host.Window, "PART_IndeterminateClip").Width, Translate(host, "PART_IndeterminateBar").X + w];
        }

        private static TranslateTransform Translate(CaseHost host, string part) =>
            (TranslateTransform)Part<Control>(host.Window, part).RenderTransform!;

        // R19: a bar narrower than its corners are wide is a 2r-wide pill in GPUI
        // (radii clamp to half the width) and the meeting of two rounded ends here.
        public Comparison.PixelTolerance? PixelToleranceFor(CaseHost host, double[] values) =>
            values[1] - values[0] < 2 * ((ProgressBar)host.Control).CornerRadius.TopLeft
                ? Comparison.PixelTolerance.Default with { FlatMax = 64, EdgeMax = 128, EdgeMean = 6 }
                : null;

        private sealed record Loading(DeclaredLoop Bar, DeclaredKeyframes Clip, double Width) : IDeclaredMotion
        {
            public TimeSpan Settle => TimeSpan.Zero;

            public double[] ValueAt(TimeSpan t)
            {
                var left = Width - Clip.ValueAt(t);
                var right = Bar.ValueAt(t) + Width;
                // A bar narrower than nothing paints nothing; GPUI reports no indicator then.
                return right - left <= 0.001 ? [0, 0] : [left, right];
            }
        }
    }

    /// <summary>tooltip.rs: the enter effect, a fade from 0 and a 4px slide up over 150ms (ease-out-cubic).</summary>
    private sealed class TooltipEnter : IMotionProbe
    {
        // Alpha to one 8-bit step; the bubble's position to GPUI's half-pixel snapping.
        public double Tolerance => 0.26;

        public double[] FromGpui(GoldenScene scene, GoldenCase golden)
        {
            var bubble = scene.Quads.Where(q => q.SolidBackground && q.Bounds.Bottom <= golden.ComponentBounds.Top && q.Bounds.Width < golden.Viewport.Width)
                .OrderBy(q => q.Order).First();
            var restingTop = golden.ComponentBounds.Top - 10.5 - bubble.Bounds.Height;
            return [bubble.Background.A, bubble.Bounds.Top - restingTop];
        }

        public IDeclaredMotion Declared(CaseHost host, GoldenCase golden)
        {
            var theme = (ControlTheme)Avalonia.Application.Current!.FindResource(typeof(ToolTip))!;
            var animation = Flatten(theme).OfType<Style>()
                .First(s => s.Selector?.ToString()?.Contains("#PART_Motion", StringComparison.Ordinal) == true && s.Animations.Count > 0)
                .Animations.Cast<Animation>().First();
            return new Enter(animation);
        }

        public void Apply(CaseHost host, double[] values)
        {
            ToolTip.SetIsOpen(host.Control, true);
            host.Flush();
            var motion = Part<Panel>(host.Window, "PART_Motion");
            motion.Opacity = values[0];
            ((TranslateTransform)motion.RenderTransform!).Y = values[1];
            host.Flush();
        }

        public double[] Live(CaseHost host)
        {
            var motion = Part<Panel>(host.Window, "PART_Motion");
            return [motion.Opacity, ((TranslateTransform)motion.RenderTransform!).Y];
        }

        // R5: GPUI fades each primitive, so the two shadow layers (10% black each)
        // show through a fading bubble by up to alpha * (1 - alpha) * 10% each:
        // at most 0.25 * 0.1 * 255 * 2 = 12.75 steps. Avalonia fades the composed group.
        public Comparison.PixelTolerance? PixelToleranceFor(CaseHost host, double[] values) =>
            values[0] < 1 ? Comparison.PixelTolerance.Default with { FlatMax = 13, InkMean = 24 } : null;

        private sealed record Enter(Animation Animation) : IDeclaredMotion
        {
            public TimeSpan Settle => Animation.Duration;

            public double[] ValueAt(TimeSpan t)
            {
                var progress = Math.Clamp(t / Animation.Duration, 0, 1);
                var eased = Animation.Easing.Ease(progress);
                var from = Animation.Children[0].Setters.Cast<Setter>().ToDictionary(x => x.Property!.Name, x => Convert.ToDouble(x.Value, System.Globalization.CultureInfo.InvariantCulture));
                var to = Animation.Children[^1].Setters.Cast<Setter>().ToDictionary(x => x.Property!.Name, x => Convert.ToDouble(x.Value, System.Globalization.CultureInfo.InvariantCulture));
                return [from["Opacity"] + (to["Opacity"] - from["Opacity"]) * eased, from["Y"] + (to["Y"] - from["Y"]) * eased];
            }
        }
    }

    /// <summary>spinner.rs: the icon's rotation, one eased turn per 0.8s.</summary>
    private sealed class SpinnerTurn : IMotionProbe
    {
        public double Tolerance => 0.01;

        public double[] FromGpui(GoldenScene scene, GoldenCase golden)
        {
            var sprite = scene.Sprites.First();
            return [Angle(sprite)];
        }

        private static double Angle(SceneSprite sprite)
        {
            var angle = sprite.RotationDegrees;
            return angle < 0 ? angle + 360 : angle;
        }

        public IDeclaredMotion Declared(CaseHost host, GoldenCase golden) =>
            new Turn(DeclaredLoop.Of(ThemeAnimation(host.Control, "PART_Icon"), 0, 360, 0));

        public void Apply(CaseHost host, double[] values) =>
            ((RotateTransform)Part<PathIcon>(host.Window, "PART_Icon").RenderTransform!).Angle = values[0];

        public double[] Live(CaseHost host) =>
            [((RotateTransform)Part<PathIcon>(host.Window, "PART_Icon").RenderTransform!).Angle];

        private sealed record Turn(DeclaredLoop Loop) : IDeclaredMotion
        {
            public TimeSpan Settle => TimeSpan.Zero;

            public double[] ValueAt(TimeSpan t) => [Loop.ValueAt(t) % 360];
        }
    }
}
