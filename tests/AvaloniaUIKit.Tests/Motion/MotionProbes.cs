using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Motion;

/// <summary>A transition the theme declares, reduced to what drives a frame.</summary>
public sealed record DeclaredMotion(TimeSpan Duration, Easing Easing, double From, double To)
{
    /// <summary>The value Avalonia's transition produces <paramref name="t"/> after it starts.</summary>
    public double ValueAt(TimeSpan t)
    {
        var progress = Duration <= TimeSpan.Zero ? 1 : Math.Clamp(t / Duration, 0, 1);
        return progress >= 1 ? To : From + (To - From) * Easing.Ease(progress);
    }
}

/// <summary>
/// How one GPUI motion maps onto the Avalonia theme: the animated quantity in
/// GPUI's frames, the transition the theme declares for it, and how to pose a
/// control at a given value with motion frozen.
/// </summary>
public interface IMotionProbe
{
    /// <summary>The animated quantity in a GPUI frame.</summary>
    double FromGpui(GoldenScene scene, GoldenCase golden);

    /// <summary>
    /// The transition the theme applies when the trigger turns the control on
    /// (<paramref name="towardOn"/>) or off. Read before motion is frozen.
    /// </summary>
    DeclaredMotion Declared(CaseHost host, bool towardOn);

    /// <summary>Poses the control at <paramref name="value"/>.</summary>
    void Apply(CaseHost host, double value);

    /// <summary>How far GPUI's recorded value may be from the declared curve.</summary>
    double Tolerance { get; }
}

public static class MotionProbes
{
    public static IMotionProbe For(string component) => component switch
    {
        "switch" => new SwitchThumb(),
        "checkbox" => new CheckMark(),
        _ => throw new NotSupportedException($"no motion probe for {component}"),
    };

    public static T Part<T>(Visual root, string name) where T : Control =>
        root.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static DeclaredMotion FromTransitions(Transitions? transitions, AvaloniaProperty property, double from, double to)
    {
        var transition = transitions?.OfType<DoubleTransition>().FirstOrDefault(t => t.Property == property)
            ?? throw new InvalidOperationException($"the theme declares no transition for {property.Name}");
        return new DeclaredMotion(transition.Duration, transition.Easing, from, to);
    }

    /// <summary>switch.rs: the thumb's x, driven by `spring_move`.</summary>
    private sealed class SwitchThumb : IMotionProbe
    {
        // GPUI snaps the thumb quad to device pixels (half a logical pixel).
        public double Tolerance => 0.26;

        public double FromGpui(GoldenScene scene, GoldenCase golden)
        {
            var thumb = Thumb(golden);
            var origin = golden.ComponentBounds.X + 2;
            var quad = scene.Quads.Where(q => q.SolidBackground && Math.Abs(q.Bounds.Width - thumb) < 0.01 && Math.Abs(q.Bounds.Height - thumb) < 0.01)
                .OrderByDescending(q => q.Order).First();
            return quad.Bounds.X - origin;
        }

        public DeclaredMotion Declared(CaseHost host, bool towardOn)
        {
            var toggle = (ToggleSwitch)host.Control;
            var travel = Part<Canvas>(host.Window, "PART_SwitchKnob").Bounds.Width;
            return FromTransitions(toggle.KnobTransitions, Canvas.LeftProperty, towardOn ? 0 : travel, towardOn ? travel : 0);
        }

        public void Apply(CaseHost host, double value) =>
            Canvas.SetLeft(Part<Panel>(host.Window, "PART_MovingKnobs"), value);

        private static double Thumb(GoldenCase golden) => golden.Str("size", "medium") switch
        {
            "xsmall" or "small" => 12,
            "large" => 20,
            _ => 16,
        };
    }

    /// <summary>checkbox.rs: the check mark's opacity, driven by `spring_control`.</summary>
    private sealed class CheckMark : IMotionProbe
    {
        public double Tolerance => 0.002;

        public double FromGpui(GoldenScene scene, GoldenCase golden)
        {
            var box = new Rect(golden.ComponentBounds.X, golden.ComponentBounds.Y, 20, golden.ComponentBounds.Height);
            var marks = scene.Sprites.Where(s => box.Contains(s.Bounds.Center)).ToList();
            return marks.Count == 0 ? 0 : marks.Max(s => s.Color.A);
        }

        public DeclaredMotion Declared(CaseHost host, bool towardOn)
        {
            var mark = Part<PathIcon>(host.Window, "PART_Mark");
            return FromTransitions(mark.Transitions, Visual.OpacityProperty, towardOn ? 0 : 1, towardOn ? 1 : 0);
        }

        public void Apply(CaseHost host, double value) =>
            Part<PathIcon>(host.Window, "PART_Mark").Opacity = value;
    }
}
