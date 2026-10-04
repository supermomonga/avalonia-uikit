using Avalonia;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Comparison;

/// <summary>Renders a golden case with the theme and compares it with GPUI's frame.</summary>
public static class VisualAssert
{
    /// <summary>Layout must agree to a quarter of a logical pixel (R9 covers rounding ties).</summary>
    public const double GeometryTolerance = 0.26;

    /// <param name="excluded">
    /// Areas (window coordinates) whose pixels are not compared, each for a
    /// relaxation the caller cites; structure is still compared there.
    /// </param>
    public static void Matches(GoldenCase golden, PixelTolerance? tolerance = null, Action<CaseHost>? configure = null, Func<GoldenCase, Avalonia.Controls.Control>? create = null, Func<CaseHost, IEnumerable<Rect>>? excluded = null)
    {
        using var host = CaseHost.Open(golden, (create ?? Adapters.Create)(golden));
        configure?.Invoke(host);
        host.Drive(golden, golden.State);
        Adapters.AfterDrive(golden, host);
        var bounds = host.ControlBounds();
        var failures = new List<string>();
        // A zero-area GPUI box (a separator's flex container) has no Avalonia counterpart to compare.
        var comparable = golden.ComponentBounds.Width > 0 && golden.ComponentBounds.Height > 0;
        if (comparable && !Near(bounds, golden.ComponentBounds) && !NearWithTextRounding(golden.ComponentBounds, bounds))
        {
            failures.Add($"bounds {Fmt(bounds)} != gpui {Fmt(golden.ComponentBounds)}");
        }
        Compare(golden, host, tolerance, failures, excluded: excluded?.Invoke(host));
    }

    /// <summary>
    /// Compares a host the caller has already put into a motion frame's state.
    /// GPUI snaps a moving edge to the nearest device pixel and Avalonia rounds
    /// the posed value its own way, so geometry may differ by a device pixel.
    /// </summary>
    public static void MatchesPosed(GoldenCase golden, CaseHost host, PixelTolerance? tolerance = null) =>
        Compare(golden, host, tolerance, [], geometryTolerance: 0.51);

    private static void Compare(GoldenCase golden, CaseHost host, PixelTolerance? tolerance, List<string> failures, double geometryTolerance = StructuralComparison.GeometryTolerance, IEnumerable<Rect>? excluded = null)
    {
        var actual = host.Capture();
        var expected = RgbaImage.Load(Path.Combine(Repo.Goldens, golden.Png!));
        var scene = GoldenScene.Load(golden.Scene!);
        var primitives = StructuralComparison.FromVisuals(host.Window);
        var regions = PixelComparison.Classify(scene, expected.Width, expected.Height, CaseHost.Scale, primitives);
        PixelComparison.Exclude(regions, expected.Width, expected.Height, CaseHost.Scale, excluded);
        var inkMass = PixelComparison.InkMass(expected, actual, regions, scene, CaseHost.Scale);
        var bandMasses = PixelComparison.BandMasses(expected, actual, regions, scene, CaseHost.Scale);
        var report = PixelComparison.Compare(expected, actual, regions, tolerance ?? PixelTolerance.Default, inkMass, bandMasses);
        Calibration.RecordInk(golden.Id, inkMass);
        Calibration.RecordBands(golden.Id, bandMasses);
        failures.AddRange(report.Failures);

        var structure = StructuralComparison.Compare(scene, primitives, geometryTolerance);
        structure.AddRange(StructuralComparison.CompareInk(scene, host.Window));
        Calibration.Record(golden.Id, report);
        failures.AddRange(structure);

        if (failures.Count > 0)
        {
            var dir = PixelComparison.WriteArtifacts(golden.Id, expected, actual, regions, report);
            throw new VisualMismatchException(
                $"{golden.Id} differs from GPUI Kit:\n  " + string.Join("\n  ", failures) +
                $"\n  pixels: {report}\n  artifacts: {dir}");
        }
    }

    private static bool Near(Rect a, Rect b) =>
        Math.Abs(a.X - b.X) <= GeometryTolerance && Math.Abs(a.Y - b.Y) <= GeometryTolerance &&
        Math.Abs(a.Width - b.Width) <= GeometryTolerance && Math.Abs(a.Height - b.Height) <= GeometryTolerance;

    /// <summary>
    /// R9: GPUI rounds a text's measured height up to whole logical pixels,
    /// Avalonia to device pixels, so a box holding text (a wrapped label with a
    /// fractional line height) may be less than a pixel shorter here. Widths
    /// match: TextLines.RoundsWidthUp rounds them as GPUI does.
    /// </summary>
    private static bool NearWithTextRounding(Rect gpui, Rect avalonia)
    {
        var dh = gpui.Height - avalonia.Height;
        return dh >= -GeometryTolerance && dh < 1 &&
            Math.Abs(gpui.X - avalonia.X) <= GeometryTolerance && Math.Abs(gpui.Y - avalonia.Y) <= GeometryTolerance &&
            Math.Abs(gpui.Width - avalonia.Width) <= GeometryTolerance;
    }

    public static string Fmt(Rect r) =>
        $"[{GoldenManifest.Format(r.X)}, {GoldenManifest.Format(r.Y)}, {GoldenManifest.Format(r.Width)}, {GoldenManifest.Format(r.Height)}]";
}

public sealed class VisualMismatchException(string message) : Exception(message);
