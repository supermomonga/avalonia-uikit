using Avalonia;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI rounds each edge of a flex item to the nearest device pixel, so the
/// widths of items that share a fractional length differ by a device pixel.
/// The panels that share lengths place their edges the same way.
/// </summary>
internal static class LayoutSnap
{
    /// <summary>The nearest device pixel to <paramref name="value"/> (logical px).</summary>
    public static double Edge(Layoutable owner, double value)
    {
        var scale = LayoutHelper.GetLayoutScale(owner);
        return Math.Round(value * scale, MidpointRounding.AwayFromZero) / scale;
    }

    /// <summary>Runs of <paramref name="lengths"/> from <paramref name="start"/>, as snapped (start, length) pairs.</summary>
    public static (double Start, double Length)[] Runs(Layoutable owner, double start, IReadOnlyList<double> lengths, double gap = 0)
    {
        var runs = new (double, double)[lengths.Count];
        var at = start;
        for (var i = 0; i < lengths.Count; i++)
        {
            var a = Edge(owner, at);
            var b = Edge(owner, at + lengths[i]);
            runs[i] = (a, b - a);
            at += lengths[i] + gap;
        }
        return runs;
    }
}
