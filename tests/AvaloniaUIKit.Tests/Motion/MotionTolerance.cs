using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Motion;

/// <summary>The two motion frames whose pixels differ for a reason in principle.</summary>
public static class MotionTolerance
{
    public static PixelTolerance? For(GoldenCase golden, GoldenScene scene) => golden.Component switch
    {
        "tooltip" when Fading(golden, scene) => FadingGroup,
        "select" when FadingBelow(golden, scene) => FadingGroup,
        "progress" when golden.Motion!.Name == "loading" && NarrowBar(golden, scene) => NarrowPill,
        _ => null,
    };

    // R5: GPUI fades each primitive, so the two shadow layers (10% black each)
    // show through a fading bubble by up to alpha * (1 - alpha) * 10% each:
    // at most 0.25 * 0.1 * 255 * 2 = 12.75 steps. Avalonia fades the composed group.
    private static readonly PixelTolerance FadingGroup = PixelTolerance.Default with { FlatMax = 13, InkMean = 24 };

    // R19: a bar narrower than its corners are wide is a 2r-wide pill in GPUI
    // (radii clamp to half the width) and the meeting of two rounded ends here.
    private static readonly PixelTolerance NarrowPill = PixelTolerance.Default with { FlatMax = 64, EdgeMax = 128, EdgeMean = 6 };

    private static bool Fading(GoldenCase golden, GoldenScene scene) =>
        scene.Quads.Where(q => q.SolidBackground && q.Bounds.Bottom <= golden.ComponentBounds.Top)
            .OrderBy(q => q.Order).FirstOrDefault() is { } bubble && bubble.Background.A < 0.999;

    // A dropdown below its trigger: the popover surface while it fades in.
    private static bool FadingBelow(GoldenCase golden, GoldenScene scene) =>
        scene.Quads.Where(q => q.SolidBackground && q.Bounds.Top >= golden.ComponentBounds.Bottom && q.Bounds.Width >= golden.ComponentBounds.Width - 0.5)
            .OrderBy(q => q.Order).FirstOrDefault() is { } surface && surface.Background.A < 0.999;

    private static bool NarrowBar(GoldenCase golden, GoldenScene scene)
    {
        var bar = golden.ComponentBounds;
        var indicator = scene.Quads
            .Where(q => q.SolidBackground && q.Background.A > 0.9 && Math.Abs(q.Bounds.Height - bar.Height) < 0.01 && q.Bounds.Width < bar.Width - 0.01)
            .OrderByDescending(q => q.Order).FirstOrDefault();
        return (indicator?.Bounds.Width ?? 0) < bar.Height;
    }
}
