using System.Globalization;
using System.Text;
using Avalonia;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Comparison;

/// <summary>
/// Kinds of pixels, told apart with the GPUI scene, because each is rasterized
/// differently by Metal and Skia (see docs/testing.md).
/// </summary>
public enum Region : byte
{
    /// <summary>Solid fills away from any edge: must match within color quantization (R11).</summary>
    Flat,
    /// <summary>Within one device pixel of a rounded or straight outline: antialiasing differs (R2).</summary>
    Edge,
    /// <summary>Glyph and icon sprites: CoreText/resvg versus Skia rasterization (R1, R4).</summary>
    Ink,
    /// <summary>Gaussian shadows: GPUI's erf approximation versus Skia's blur (R3).</summary>
    Shadow,
}

/// <summary>
/// Per-region limits, in 1/255 steps of the largest channel difference.
/// InkMass* bound Avalonia's ink mass as a share of GPUI's (see
/// <see cref="PixelComparison.InkMass"/>), checked once GPUI paints at least
/// InkMassFloor of it. Calibrated over every case: text and icons land within
/// 0.69-1.07 (the low end in tooltip fade frames), a missing line at 0.
/// </summary>
public sealed record PixelTolerance(
    double FlatMax = 2,
    double EdgeMax = 64,
    double EdgeMean = 3,
    double InkMean = 18,
    double InkMassMin = 0.6,
    double InkMassMax = 1.6,
    double InkMassFloor = 200,
    double ShadowMax = 12)
{
    public static PixelTolerance Default { get; } = new();
}

public sealed class RegionStats
{
    public int Count;
    public double Max;
    public double Sum;
    /// <summary>Sum of GPUI's minus Avalonia's mean channel value: ink missing on one side shows as a bias.</summary>
    public double SignedSum;
    public double SignedMean => Count == 0 ? 0 : SignedSum / Count;
    public int MaxX;
    public int MaxY;
    public double Mean => Count == 0 ? 0 : Sum / Count;
}

public sealed class PixelReport
{
    public required Dictionary<Region, RegionStats> Stats { get; init; }
    public required List<string> Failures { get; init; }
    public bool Passed => Failures.Count == 0;

    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var (region, s) in Stats)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{region}: n={s.Count} max={s.Max:0.#} at ({s.MaxX},{s.MaxY}) mean={s.Mean:0.##} bias={s.SignedMean:0.##}; ");
        }
        foreach (var f in Failures)
        {
            sb.AppendLine().Append("  ").Append(f);
        }
        return sb.ToString();
    }
}

public static class PixelComparison
{
    /// <summary>Classifies every device pixel of a frame from the GPUI scene.</summary>
    public static Region[] Classify(GoldenScene scene, int width, int height, double scale, IEnumerable<Primitive>? actual = null)
    {
        var regions = new Region[width * height];
        void Mark(Region region, Func<double, double, bool> inside, Rect area)
        {
            var x0 = Math.Max(0, (int)Math.Floor(area.X * scale));
            var y0 = Math.Max(0, (int)Math.Floor(area.Y * scale));
            var x1 = Math.Min(width, (int)Math.Ceiling(area.Right * scale));
            var y1 = Math.Min(height, (int)Math.Ceiling(area.Bottom * scale));
            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++)
                {
                    var i = y * width + x;
                    if (Priority(region) > Priority(regions[i]) && inside((x + 0.5) / scale, (y + 0.5) / scale))
                    {
                        regions[i] = region;
                    }
                }
            }
        }

        var reach = 1.0 / scale; // one device pixel, in logical pixels
        // Antialiasing kernels differ by up to one and a half device pixels at tight corners (R2).
        var edgeReach = 1.5 / scale;
        foreach (var q in scene.Quads)
        {
            var hasBorder = q.BorderWidths != default && !q.BorderColor.IsTransparent;
            var visibleFill = q.SolidBackground && !q.Background.IsTransparent;
            if (!hasBorder && !visibleFill)
            {
                continue;
            }
            var area = q.Bounds.Inflate(edgeReach * 1.5).Intersect(q.Clip.Inflate(edgeReach));
            Mark(Region.Edge, (x, y) =>
            {
                var d = RoundedRectDistance(q.Bounds, q.Radii, x, y);
                if (Math.Abs(d) <= edgeReach * 1.01)
                {
                    return true;
                }
                if (hasBorder)
                {
                    var inner = q.Bounds.Deflate(q.BorderWidths);
                    var innerRadii = new CornerRadius(
                        Math.Max(0, q.Radii.TopLeft - Math.Max(q.BorderWidths.Left, q.BorderWidths.Top)),
                        Math.Max(0, q.Radii.TopRight - Math.Max(q.BorderWidths.Right, q.BorderWidths.Top)),
                        Math.Max(0, q.Radii.BottomRight - Math.Max(q.BorderWidths.Right, q.BorderWidths.Bottom)),
                        Math.Max(0, q.Radii.BottomLeft - Math.Max(q.BorderWidths.Left, q.BorderWidths.Bottom)));
                    return Math.Abs(RoundedRectDistance(inner, innerRadii, x, y)) <= edgeReach * 1.01;
                }
                return false;
            }, area);
        }
        // The outlines Avalonia paints count as edges too: text measurement may
        // place an edge up to one device pixel away from GPUI's (R9).
        foreach (var p in actual ?? [])
        {
            if (p.Kind == PrimitiveKind.Shadow)
            {
                continue;
            }
            var area = p.Bounds.Inflate(edgeReach * 1.5);
            Mark(Region.Edge, (x, y) =>
            {
                if (Math.Abs(RoundedRectDistance(p.Bounds, p.Radii, x, y)) <= edgeReach * 1.01)
                {
                    return true;
                }
                if (p.Kind == PrimitiveKind.Band)
                {
                    var inner = p.Bounds.Deflate(p.Widths);
                    var innerRadii = new CornerRadius(
                        Math.Max(0, p.Radii.TopLeft - Math.Max(p.Widths.Left, p.Widths.Top)),
                        Math.Max(0, p.Radii.TopRight - Math.Max(p.Widths.Right, p.Widths.Top)),
                        Math.Max(0, p.Radii.BottomRight - Math.Max(p.Widths.Right, p.Widths.Bottom)),
                        Math.Max(0, p.Radii.BottomLeft - Math.Max(p.Widths.Left, p.Widths.Bottom)));
                    return Math.Abs(RoundedRectDistance(inner, innerRadii, x, y)) <= edgeReach * 1.01;
                }
                return false;
            }, area);
        }
        foreach (var s in scene.Shadows)
        {
            // Only where the shadow shows: outside the element casting it.
            var spread = s.Sigma * 3 + reach;
            Mark(Region.Shadow, (x, y) => RoundedRectDistance(s.ElementBounds, s.ElementRadii, x, y) > -edgeReach,
                s.Bounds.Inflate(spread).Intersect(s.Clip));
        }
        foreach (var s in scene.Sprites)
        {
            Mark(Region.Ink, (_, _) => true, s.Bounds.Inflate(reach).Intersect(s.Clip.Inflate(reach)));
        }
        foreach (var u in scene.Underlines)
        {
            Mark(Region.Ink, (_, _) => true, u.Bounds.Inflate(reach));
        }
        foreach (var p in scene.Paths)
        {
            Mark(Region.Ink, (_, _) => true, p.Inflate(reach));
        }
        return regions;
    }

    private static int Priority(Region r) => r switch
    {
        Region.Flat => 0,
        Region.Shadow => 1,
        Region.Edge => 2,
        Region.Ink => 3,
        _ => 0,
    };

    /// <summary>Signed distance from a point to a rounded rectangle (negative inside).</summary>
    public static double RoundedRectDistance(Rect r, CornerRadius radii, double x, double y)
    {
        var cx = r.X + r.Width / 2;
        var cy = r.Y + r.Height / 2;
        var px = x - cx;
        var py = y - cy;
        var radius = px < 0
            ? (py < 0 ? radii.TopLeft : radii.BottomLeft)
            : (py < 0 ? radii.TopRight : radii.BottomRight);
        radius = Math.Min(radius, Math.Min(r.Width, r.Height) / 2);
        var qx = Math.Abs(px) - r.Width / 2 + radius;
        var qy = Math.Abs(py) - r.Height / 2 + radius;
        var outside = Math.Sqrt(Math.Pow(Math.Max(qx, 0), 2) + Math.Pow(Math.Max(qy, 0), 2));
        return outside + Math.Min(Math.Max(qx, qy), 0) - radius;
    }

    /// <summary>
    /// How much ink each side paints over what lies beneath it: the sum, over ink
    /// pixels, of each pixel's largest channel difference from the fills under it
    /// (from GPUI's scene). Rasterizers draw text a little bolder or lighter; a
    /// missing or extra glyph, icon or line changes the sum far more.
    /// </summary>
    public static (double Expected, double Actual) InkMass(RgbaImage expected, RgbaImage actual, Region[] regions, GoldenScene scene, double scale)
    {
        double e = 0, a = 0;
        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                if (regions[y * expected.Width + x] != Region.Ink)
                {
                    continue;
                }
                var bg = Beneath(scene, (x + 0.5) / scale, (y + 0.5) / scale);
                var ep = expected.Pixel(x, y);
                var ap = actual.Pixel(x, y);
                e += Math.Max(Math.Abs(ep[0] - bg.R), Math.Max(Math.Abs(ep[1] - bg.G), Math.Abs(ep[2] - bg.B)));
                a += Math.Max(Math.Abs(ap[0] - bg.R), Math.Max(Math.Abs(ap[1] - bg.G), Math.Abs(ap[2] - bg.B)));
            }
        }
        return (e, a);
    }

    private static (double R, double G, double B) Beneath(GoldenScene scene, double x, double y)
    {
        double r = 0, g = 0, b = 0;
        foreach (var q in scene.Quads.OrderBy(q => q.Order))
        {
            if (!q.SolidBackground || q.Background.IsTransparent || RoundedRectDistance(q.Bounds, q.Radii, x, y) > 0 || !q.Clip.Contains(new Point(x, y)))
            {
                continue;
            }
            var alpha = q.Background.A;
            r = r * (1 - alpha) + q.Background.R * 255 * alpha;
            g = g * (1 - alpha) + q.Background.G * 255 * alpha;
            b = b * (1 - alpha) + q.Background.B * 255 * alpha;
        }
        return (r, g, b);
    }

    public static PixelReport Compare(RgbaImage expected, RgbaImage actual, Region[] regions, PixelTolerance tolerance, (double Expected, double Actual)? inkMass = null)
    {
        var failures = new List<string>();
        var stats = Enum.GetValues<Region>().ToDictionary(r => r, _ => new RegionStats());
        if (expected.Width != actual.Width || expected.Height != actual.Height)
        {
            failures.Add($"size {actual.Width}x{actual.Height} != expected {expected.Width}x{expected.Height}");
            return new PixelReport { Stats = stats, Failures = failures };
        }
        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                var region = regions[y * expected.Width + x];
                // Outlines and centered text may sit one device pixel apart (R9), so
                // edge and ink pixels are compared with the closest of GPUI's pixels
                // around them.
                double d = region is Region.Edge or Region.Ink ? NearestDiff(expected, actual, x, y) : Diff(expected, actual, x, y, x, y);
                var s = stats[region];
                s.Count++;
                s.Sum += d;
                var ep = expected.Pixel(x, y);
                var ap = actual.Pixel(x, y);
                s.SignedSum += (ep[0] + ep[1] + ep[2] - ap[0] - ap[1] - ap[2]) / 3.0;
                if (d > s.Max)
                {
                    s.Max = d;
                    s.MaxX = x;
                    s.MaxY = y;
                }
            }
        }
        Check(stats[Region.Flat].Max <= tolerance.FlatMax, $"flat max {stats[Region.Flat].Max} > {tolerance.FlatMax} at ({stats[Region.Flat].MaxX},{stats[Region.Flat].MaxY})");
        Check(stats[Region.Edge].Max <= tolerance.EdgeMax, $"edge max {stats[Region.Edge].Max} > {tolerance.EdgeMax} at ({stats[Region.Edge].MaxX},{stats[Region.Edge].MaxY})");
        Check(stats[Region.Edge].Mean <= tolerance.EdgeMean, $"edge mean {stats[Region.Edge].Mean:0.##} > {tolerance.EdgeMean}");
        Check(stats[Region.Ink].Mean <= tolerance.InkMean, $"ink mean {stats[Region.Ink].Mean:0.##} > {tolerance.InkMean}");
        if (inkMass is { } mass && mass.Expected >= tolerance.InkMassFloor)
        {
            var ratio = mass.Actual / mass.Expected;
            Check(ratio >= tolerance.InkMassMin && ratio <= tolerance.InkMassMax,
                $"ink mass {ratio:0.##}x GPUI's (allowed {tolerance.InkMassMin}-{tolerance.InkMassMax}): text, icon or line missing or extra");
        }
        Check(stats[Region.Shadow].Max <= tolerance.ShadowMax, $"shadow max {stats[Region.Shadow].Max} > {tolerance.ShadowMax} at ({stats[Region.Shadow].MaxX},{stats[Region.Shadow].MaxY})");
        return new PixelReport { Stats = stats, Failures = failures };

        void Check(bool ok, string message)
        {
            if (!ok)
            {
                failures.Add(message);
            }
        }
    }

    private static double Diff(RgbaImage expected, RgbaImage actual, int ex, int ey, int ax, int ay)
    {
        var e = expected.Pixel(ex, ey);
        var a = actual.Pixel(ax, ay);
        return Math.Max(Math.Abs(e[0] - a[0]), Math.Max(Math.Abs(e[1] - a[1]), Math.Abs(e[2] - a[2])));
    }

    private static double NearestDiff(RgbaImage expected, RgbaImage actual, int x, int y)
    {
        var best = double.MaxValue;
        for (var dy = -1; dy <= 1; dy++)
        {
            for (var dx = -1; dx <= 1; dx++)
            {
                var ex = x + dx;
                var ey = y + dy;
                if (ex >= 0 && ey >= 0 && ex < expected.Width && ey < expected.Height)
                {
                    best = Math.Min(best, Diff(expected, actual, ex, ey, x, y));
                }
            }
        }
        return best;
    }

    /// <summary>Writes the images a reviewer needs to see why a case failed.</summary>
    public static string WriteArtifacts(string id, RgbaImage expected, RgbaImage actual, Region[] regions, PixelReport report)
    {
        var dir = Path.Combine(Repo.Artifacts, id.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);
        expected.Save(Path.Combine(dir, "gpui.png"));
        actual.Save(Path.Combine(dir, "avalonia.png"));
        if (expected.Width == actual.Width && expected.Height == actual.Height)
        {
            var diff = new RgbaImage(expected.Width, expected.Height);
            var mask = new RgbaImage(expected.Width, expected.Height);
            for (var y = 0; y < expected.Height; y++)
            {
                for (var x = 0; x < expected.Width; x++)
                {
                    var e = expected.Pixel(x, y);
                    var a = actual.Pixel(x, y);
                    var d = Math.Max(Math.Abs(e[0] - a[0]), Math.Max(Math.Abs(e[1] - a[1]), Math.Abs(e[2] - a[2])));
                    var v = (byte)Math.Min(255, d * 4);
                    var p = diff.Pixel(x, y);
                    p[0] = v; p[1] = (byte)(e[1] / 4); p[2] = (byte)(e[2] / 4); p[3] = 255;
                    var m = mask.Pixel(x, y);
                    (m[0], m[1], m[2]) = regions[y * expected.Width + x] switch
                    {
                        Region.Edge => ((byte)255, (byte)160, (byte)0),
                        Region.Ink => ((byte)0, (byte)120, (byte)255),
                        Region.Shadow => ((byte)160, (byte)0, (byte)255),
                        _ => ((byte)230, (byte)230, (byte)230),
                    };
                    m[3] = 255;
                }
            }
            diff.Save(Path.Combine(dir, "diff.png"));
            mask.Save(Path.Combine(dir, "mask.png"));
        }
        File.WriteAllText(Path.Combine(dir, "report.txt"), report.ToString());
        return dir;
    }
}
