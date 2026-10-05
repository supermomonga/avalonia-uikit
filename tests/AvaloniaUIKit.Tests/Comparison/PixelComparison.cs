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
    /// <summary>
    /// Solid fills away from any edge: must match within color quantization
    /// (R11). A translucent color over another fill quantizes twice (its alpha
    /// and the blend), so the limit is two steps.
    /// </summary>
    Flat,
    /// <summary>Within one device pixel of a rounded or straight outline: antialiasing differs (R2).</summary>
    Edge,
    /// <summary>Glyph and icon sprites: CoreText/resvg versus Skia rasterization (R1, R4).</summary>
    Ink,
    /// <summary>Gaussian shadows: GPUI's erf approximation versus Skia's blur (R3).</summary>
    Shadow,
    /// <summary>Raster images: inside, both renderers sample alike.</summary>
    Image,
    /// <summary>
    /// The rim of a raster image: GPUI samples its atlas tile with transparent
    /// neighbours, so a magnified image fades out over half a source pixel at
    /// its edges where Skia clamps (R31). Geometry is the structure's to check.
    /// </summary>
    ImageEdge,
    /// <summary>
    /// Inside a gradient fill: GPUI's shader dithers a gradient against banding,
    /// Skia does not (R32). One noise moves each color channel by up to two 8-bit
    /// steps and the alpha by up to three, so an opaque gradient lets up to 3/255
    /// of what is under it through.
    /// </summary>
    Gradient,
    /// <summary>Not compared: an area a test excludes for a cited relaxation.</summary>
    Excluded,
}

/// <summary>
/// Per-region limits, in 1/255 steps of the largest channel difference.
/// InkMass* bound Avalonia's ink mass as a share of GPUI's (see
/// <see cref="PixelComparison.InkMass"/>), checked once GPUI paints at least
/// InkMassFloor of it. Calibrated over every case: text and icons land within
/// 0.65-1.10 (the low end in a fading tab pill frame), a missing line at 0.
/// BandMass* bound each corner and side of a border the same way (see
/// <see cref="PixelComparison.BandMasses"/>): 0.69-1.32 over every case, an
/// accordion's corners under its items' fills at 0.41, a doubled line at 2.
/// </summary>
public sealed record PixelTolerance(
    double FlatMax = 2,
    double EdgeMax = 64,
    double EdgeMean = 3,
    double InkMean = 18,
    double InkMassMin = 0.6,
    double InkMassMax = 1.6,
    double InkMassFloor = 200,
    double BandMassMin = 0.6,
    double BandMassMax = 1.6,
    double BandMassFloor = 200,
    double ShadowMax = 12,
    double ImageMean = 3,
    double ImageEdgeMean = 12,
    double GradientMax = 5,
    double GradientMean = 1.25)
{
    public static PixelTolerance Default { get; } = new();
}

/// <summary>The ink of one corner or side of a GPUI border, in GPUI's frame and Avalonia's.</summary>
public sealed record BandMass(string Piece, double Expected, double Actual);

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
        // skip: where inside is known to fail (an outline test deep inside a box), not visited.
        void Mark(Region region, Func<double, double, bool> inside, Rect area, Rect skip = default)
        {
            var x0 = Math.Max(0, (int)Math.Floor(area.X * scale));
            var y0 = Math.Max(0, (int)Math.Floor(area.Y * scale));
            var x1 = Math.Min(width, (int)Math.Ceiling(area.Right * scale));
            var y1 = Math.Min(height, (int)Math.Ceiling(area.Bottom * scale));
            var (sx0, sx1, sy0, sy1) = Centers(skip, scale);
            for (var y = y0; y < y1; y++)
            {
                var skipping = y >= sy0 && y < sy1;
                for (var x = x0; x < x1; x++)
                {
                    if (skipping && x >= sx0 && x < sx1)
                    {
                        x = sx1 - 1;
                        continue;
                    }
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
            if (!hasBorder && !VisibleFill(q))
            {
                continue;
            }
            var area = q.Bounds.Inflate(edgeReach * 1.5).Intersect(q.Clip.Inflate(edgeReach));
            var inner = q.Bounds.Deflate(q.BorderWidths);
            var innerRadii = InnerRadii(q.Radii, q.BorderWidths);
            var skip = Deep(q.Bounds, q.Radii, edgeReach * 1.01);
            if (hasBorder)
            {
                skip = skip.Intersect(Deep(inner, innerRadii, edgeReach * 1.01));
            }
            Mark(Region.Edge, (x, y) =>
            {
                var d = RoundedRectDistance(q.Bounds, q.Radii, x, y);
                if (Math.Abs(d) <= edgeReach * 1.01)
                {
                    return true;
                }
                if (hasBorder)
                {
                    return Math.Abs(RoundedRectDistance(inner, innerRadii, x, y)) <= edgeReach * 1.01;
                }
                return false;
            }, area, skip);
        }
        // Where a clip cuts a quad, the cut is an edge too: GPUI rounds the clip's
        // edges to the nearest device pixel, Avalonia's layout rounds sizes up (R9).
        foreach (var q in scene.Quads)
        {
            var cut = q.Bounds.Intersect(q.Clip);
            if (cut == q.Bounds || cut.Width <= 0 || cut.Height <= 0 ||
                (q.BorderWidths == default || q.BorderColor.IsTransparent) && !VisibleFill(q))
            {
                continue;
            }
            Mark(Region.Edge, (x, y) => Math.Abs(RoundedRectDistance(q.Clip, default, x, y)) <= edgeReach * 1.01,
                cut.Inflate(edgeReach * 1.5).Intersect(q.Bounds), Deep(q.Clip, default, edgeReach * 1.01));
        }
        // The outlines Avalonia paints count as edges too: text measurement may
        // place an edge up to one device pixel away from GPUI's (R9).
        foreach (var p in actual ?? [])
        {
            if (p.Kind is PrimitiveKind.Shadow or PrimitiveKind.Image)
            {
                continue;
            }
            var area = p.Bounds.Inflate(edgeReach * 1.5);
            var band = p.Kind == PrimitiveKind.Band;
            var inner = p.Bounds.Deflate(p.Widths);
            var innerRadii = InnerRadii(p.Radii, p.Widths);
            var skip = Deep(p.Bounds, p.Radii, edgeReach * 1.01);
            if (band)
            {
                skip = skip.Intersect(Deep(inner, innerRadii, edgeReach * 1.01));
            }
            Mark(Region.Edge, (x, y) =>
            {
                if (Math.Abs(RoundedRectDistance(p.Bounds, p.Radii, x, y)) <= edgeReach * 1.01)
                {
                    return true;
                }
                if (band)
                {
                    return Math.Abs(RoundedRectDistance(inner, innerRadii, x, y)) <= edgeReach * 1.01;
                }
                return false;
            }, area, skip);
        }
        foreach (var q in scene.Quads.Where(q => q.Gradient is not null))
        {
            Mark(Region.Gradient, (x, y) => RoundedRectDistance(q.Bounds, q.Radii, x, y) < 0, q.Bounds.Intersect(q.Clip));
        }
        foreach (var image in scene.Images)
        {
            var drawn = image.Bounds.Intersect(image.Clip);
            Mark(Region.Image, (x, y) => RoundedRectDistance(drawn, image.Radii, x, y) < -edgeReach, drawn);
            // Half a source pixel at up to 5x: 2.5 device pixels.
            var rim = 2.5 / scale;
            Mark(Region.ImageEdge, (x, y) => Math.Abs(RoundedRectDistance(drawn, image.Radii, x, y)) <= rim * 1.01, drawn.Inflate(rim * 1.5),
                Deep(drawn, image.Radii, rim * 1.01));
        }
        foreach (var s in scene.Shadows)
        {
            // Only where the shadow shows: outside the element casting it.
            var spread = s.Sigma * 3 + reach;
            Mark(Region.Shadow, (x, y) => RoundedRectDistance(s.ElementBounds, s.ElementRadii, x, y) > -edgeReach,
                s.Bounds.Inflate(spread).Intersect(s.Clip), Deep(s.ElementBounds, s.ElementRadii, edgeReach));
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

    // A solid or a gradient fill that shows: its outline is an edge, wherever it is (R2).
    private static bool VisibleFill(SceneQuad q) =>
        q.SolidBackground ? !q.Background.IsTransparent : q.Gradient is { } g && !(g.Start.IsTransparent && g.End.IsTransparent);

    private static int Priority(Region r) => r switch
    {
        Region.Flat => 0,
        Region.Gradient => 1,
        Region.Shadow => 2,
        Region.Image => 3,
        Region.ImageEdge => 4,
        Region.Edge => 5,
        Region.Ink => 6,
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
    /// The part of a rounded rectangle more than <paramref name="depth"/> inside
    /// its outline, where <see cref="RoundedRectDistance"/> is below -depth
    /// (empty if there is none): a test for pixels near the outline fails there,
    /// so a large box costs its perimeter rather than its area.
    /// </summary>
    private static Rect Deep(Rect r, CornerRadius radii, double depth)
    {
        // Farther than every corner radius from each side, the distance is to the nearest side.
        var inset = Math.Max(Math.Max(Math.Max(radii.TopLeft, radii.TopRight), Math.Max(radii.BottomRight, radii.BottomLeft)), depth) + 1e-3;
        return r.Width > 2 * inset && r.Height > 2 * inset ? r.Deflate(inset) : default;
    }

    /// <summary>The device pixels whose centers lie inside <paramref name="area"/>: [x0, x1) by [y0, y1).</summary>
    private static (int X0, int X1, int Y0, int Y1) Centers(Rect area, double scale) =>
        ((int)Math.Floor(area.X * scale - 0.5) + 1, (int)Math.Ceiling(area.Right * scale - 0.5),
         (int)Math.Floor(area.Y * scale - 0.5) + 1, (int)Math.Ceiling(area.Bottom * scale - 0.5));

    /// <summary>The radii of the inner edge of a border: each corner's radius less its wider side.</summary>
    private static CornerRadius InnerRadii(CornerRadius radii, Thickness widths) => new(
        Math.Max(0, radii.TopLeft - Math.Max(widths.Left, widths.Top)),
        Math.Max(0, radii.TopRight - Math.Max(widths.Right, widths.Top)),
        Math.Max(0, radii.BottomRight - Math.Max(widths.Right, widths.Bottom)),
        Math.Max(0, radii.BottomLeft - Math.Max(widths.Left, widths.Bottom)));

    /// <summary>
    /// How much ink each side paints over what lies beneath it: the sum, over ink
    /// pixels, of each pixel's largest channel difference from the fills under it
    /// (from GPUI's scene). Rasterizers draw text a little bolder or lighter; a
    /// missing or extra glyph, icon or line changes the sum far more.
    /// </summary>
    public static (double Expected, double Actual) InkMass(RgbaImage expected, RgbaImage actual, Region[] regions, GoldenScene scene, double scale)
    {
        double e = 0, a = 0;
        var fills = Fills(scene);
        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                if (regions[y * expected.Width + x] != Region.Ink)
                {
                    continue;
                }
                var bg = Beneath(fills, (x + 0.5) / scale, (y + 0.5) / scale);
                var ep = expected.Pixel(x, y);
                var ap = actual.Pixel(x, y);
                e += Math.Max(Math.Abs(ep[0] - bg.R), Math.Max(Math.Abs(ep[1] - bg.G), Math.Abs(ep[2] - bg.B)));
                a += Math.Max(Math.Abs(ap[0] - bg.R), Math.Max(Math.Abs(ap[1] - bg.G), Math.Abs(ap[2] - bg.B)));
            }
        }
        return (e, a);
    }

    /// <summary>
    /// How much ink each of GPUI's borders paints, corner by corner and side by
    /// side: the sum, over the band and its antialiasing, of each pixel's largest
    /// channel difference from the fills under it. The edge regions compare a
    /// pixel with the closest of GPUI's, so a hairline painted over (a child's
    /// fill on a rounded corner) or left out passes them; its mass drops.
    /// </summary>
    public static List<BandMass> BandMasses(RgbaImage expected, RgbaImage actual, Region[] regions, GoldenScene scene, double scale)
    {
        var edgeReach = 1.5 / scale;
        var result = new List<BandMass>();
        var fills = Fills(scene);
        // GPUI paints a border cut by clips as one quad per clip.
        foreach (var group in scene.Quads
            .Where(q => q.BorderWidths != default && !q.BorderColor.IsTransparent)
            .GroupBy(q => (q.Bounds, q.Radii, q.BorderWidths, q.BorderColor)))
        {
            var (b, radii, w, _) = group.Key;
            // A popup or a moving element may land a device pixel off (R9): its
            // edge must not leave the band where a clip cuts it.
            var clips = group.Select(q => q.Clip.Inflate(edgeReach)).ToArray();
            var inner = b.Deflate(w);
            var innerRadii = InnerRadii(radii, w);
            var hollow = inner.Width > 0 && inner.Height > 0;
            // The hollow inside the band is left out below: not visited.
            var (sx0, sx1, sy0, sy1) = Centers(hollow ? Deep(inner, innerRadii, edgeReach * 1.01) : default, scale);
            // Where a clip cuts a side, the cut lands a device pixel either way (R9):
            // a hairline there may be painted twice as thick or not at all.
            var shown = group.Select(q => q.Clip).Aggregate((a, c) => a.Union(c));
            var cut = new[] { shown.Y > b.Y + 0.01, shown.Right < b.Right - 0.01, shown.Bottom < b.Bottom - 0.01, shown.X > b.X + 0.01 };
            var pieces = new (double Expected, double Actual)[8];
            var area = b.Inflate(edgeReach * 1.5);
            var x0 = Math.Max(0, (int)Math.Floor(area.X * scale));
            var y0 = Math.Max(0, (int)Math.Floor(area.Y * scale));
            var x1 = Math.Min(expected.Width, (int)Math.Ceiling(area.Right * scale));
            var y1 = Math.Min(expected.Height, (int)Math.Ceiling(area.Bottom * scale));
            for (var y = y0; y < y1; y++)
            {
                var skipping = y >= sy0 && y < sy1;
                for (var x = x0; x < x1; x++)
                {
                    if (skipping && x >= sx0 && x < sx1)
                    {
                        x = sx1 - 1;
                        continue;
                    }
                    var px = (x + 0.5) / scale;
                    var py = (y + 0.5) / scale;
                    if (regions[y * expected.Width + x] is Region.Ink or Region.Excluded ||
                        !InAny(clips, new Point(px, py)) ||
                        RoundedRectDistance(b, radii, px, py) > edgeReach * 1.01 ||
                        (hollow && RoundedRectDistance(inner, innerRadii, px, py) < -edgeReach * 1.01))
                    {
                        continue;
                    }
                    var bg = Beneath(fills, px, py);
                    var ep = expected.Pixel(x, y);
                    var ap = actual.Pixel(x, y);
                    var piece = Piece(b, radii, w, px, py);
                    pieces[piece].Expected += Math.Max(Math.Abs(ep[0] - bg.R), Math.Max(Math.Abs(ep[1] - bg.G), Math.Abs(ep[2] - bg.B)));
                    pieces[piece].Actual += Math.Max(Math.Abs(ap[0] - bg.R), Math.Max(Math.Abs(ap[1] - bg.G), Math.Abs(ap[2] - bg.B)));
                }
            }
            for (var i = 0; i < pieces.Length; i++)
            {
                // Corner i lies between sides i - 1 and i (top, right, bottom, left).
                if (i < 4 ? cut[i] || cut[(i + 3) % 4] : cut[i - 4])
                {
                    continue;
                }
                result.Add(new BandMass($"{PieceNames[i]} of the border {VisualAssert.Fmt(b)}", pieces[i].Expected, pieces[i].Actual));
            }
        }
        return result;
    }

    private static readonly string[] PieceNames =
        ["top-left corner", "top-right corner", "bottom-right corner", "bottom-left corner", "top side", "right side", "bottom side", "left side"];

    // A corner reaches as far as its radius or the border's widths; the rest of
    // the band belongs to the nearest side that has a border.
    private static int Piece(Rect b, CornerRadius r, Thickness w, double x, double y)
    {
        var left = x < b.X + b.Width / 2;
        var top = y < b.Y + b.Height / 2;
        var (radius, wx, wy) = (left, top) switch
        {
            (true, true) => (r.TopLeft, w.Left, w.Top),
            (false, true) => (r.TopRight, w.Right, w.Top),
            (false, false) => (r.BottomRight, w.Right, w.Bottom),
            (true, false) => (r.BottomLeft, w.Left, w.Bottom),
        };
        var dx = left ? x - b.X : b.Right - x;
        var dy = top ? y - b.Y : b.Bottom - y;
        if (dx < Math.Max(radius, wx) && dy < Math.Max(radius, wy))
        {
            return (left, top) switch { (true, true) => 0, (false, true) => 1, (false, false) => 2, _ => 3 };
        }
        var side = -1;
        var nearest = double.MaxValue;
        foreach (var (index, width, distance) in new[] { (4, w.Top, y - b.Y), (5, w.Right, b.Right - x), (6, w.Bottom, b.Bottom - y), (7, w.Left, x - b.X) })
        {
            if (width > 0 && distance < nearest)
            {
                (side, nearest) = (index, distance);
            }
        }
        return side;
    }

    private static bool InAny(Rect[] areas, Point p)
    {
        foreach (var area in areas)
        {
            if (area.Contains(p))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The solid fills <see cref="Beneath"/> composites, in paint order: sorted once per frame, not per pixel.</summary>
    private static SceneQuad[] Fills(GoldenScene scene) =>
        scene.Quads.Where(q => q.SolidBackground && !q.Background.IsTransparent).OrderBy(q => q.Order).ToArray();

    private static (double R, double G, double B) Beneath(SceneQuad[] fills, double x, double y)
    {
        double r = 0, g = 0, b = 0;
        foreach (var q in fills)
        {
            if (RoundedRectDistance(q.Bounds, q.Radii, x, y) > 0 || !q.Clip.Contains(new Point(x, y)))
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

    /// <summary>Marks the pixels inside <paramref name="areas"/> (logical px) as not compared.</summary>
    public static void Exclude(Region[] regions, int width, int height, double scale, IEnumerable<Rect>? areas)
    {
        foreach (var area in areas ?? [])
        {
            var x0 = Math.Max(0, (int)Math.Floor(area.X * scale));
            var y0 = Math.Max(0, (int)Math.Floor(area.Y * scale));
            var x1 = Math.Min(width, (int)Math.Ceiling(area.Right * scale));
            var y1 = Math.Min(height, (int)Math.Ceiling(area.Bottom * scale));
            for (var y = y0; y < y1; y++)
            {
                for (var x = x0; x < x1; x++)
                {
                    regions[y * width + x] = Region.Excluded;
                }
            }
        }
    }

    public static PixelReport Compare(RgbaImage expected, RgbaImage actual, Region[] regions, PixelTolerance tolerance, (double Expected, double Actual)? inkMass = null, IReadOnlyList<BandMass>? bandMasses = null)
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
                if (region == Region.Excluded)
                {
                    continue;
                }
                // Outlines and centered text may sit one device pixel apart (R9), so
                // edge and ink pixels are compared with the closest of GPUI's pixels
                // around them.
                double d = region is Region.Edge or Region.Ink or Region.Image or Region.ImageEdge ? NearestDiff(expected, actual, x, y) : Diff(expected, actual, x, y, x, y);
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
        foreach (var band in bandMasses ?? [])
        {
            if (band.Expected >= tolerance.BandMassFloor)
            {
                var ratio = band.Actual / band.Expected;
                Check(ratio >= tolerance.BandMassMin && ratio <= tolerance.BandMassMax,
                    $"border mass {ratio:0.##}x GPUI's at the {band.Piece} (allowed {tolerance.BandMassMin}-{tolerance.BandMassMax}): border painted over, missing or extra");
            }
        }
        Check(stats[Region.Image].Mean <= tolerance.ImageMean, $"image mean {stats[Region.Image].Mean:0.##} > {tolerance.ImageMean}");
        Check(stats[Region.ImageEdge].Mean <= tolerance.ImageEdgeMean, $"image edge mean {stats[Region.ImageEdge].Mean:0.##} > {tolerance.ImageEdgeMean}");
        Check(stats[Region.Gradient].Max <= tolerance.GradientMax, $"gradient max {stats[Region.Gradient].Max} > {tolerance.GradientMax} at ({stats[Region.Gradient].MaxX},{stats[Region.Gradient].MaxY})");
        Check(stats[Region.Gradient].Mean <= tolerance.GradientMean, $"gradient mean {stats[Region.Gradient].Mean:0.##} > {tolerance.GradientMean}");
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
                        Region.Image => ((byte)0, (byte)200, (byte)120),
                        Region.ImageEdge => ((byte)0, (byte)120, (byte)80),
                        Region.Gradient => ((byte)200, (byte)200, (byte)120),
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
