using Avalonia;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Comparison;

/// <summary>Renders a golden case with the theme and compares it with GPUI's frame.</summary>
public static class VisualAssert
{
    /// <summary>Layout must agree to a quarter of a logical pixel (R9 covers rounding ties).</summary>
    public const double GeometryTolerance = 0.26;

    public static void Matches(GoldenCase golden, PixelTolerance? tolerance = null, Action<CaseHost>? configure = null)
    {
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        configure?.Invoke(host);
        host.Drive(golden, golden.State);
        Adapters.AfterDrive(golden, host);
        var bounds = host.ControlBounds();
        var failures = new List<string>();
        var textRuns = host.Control.GetSelfAndVisualDescendants().OfType<Avalonia.Controls.TextBlock>().Count(t => !string.IsNullOrEmpty(t.Text));
        if (!Near(bounds, golden.ComponentBounds) && !NearWithTextRounding(golden.ComponentBounds, bounds, Math.Max(1, textRuns)))
        {
            failures.Add($"bounds {Fmt(bounds)} != gpui {Fmt(golden.ComponentBounds)}");
        }
        Compare(golden, host, tolerance, failures);
    }

    /// <summary>Compares a host the caller has already put into the frame's state.</summary>
    public static void MatchesPosed(GoldenCase golden, CaseHost host, PixelTolerance? tolerance = null) =>
        Compare(golden, host, tolerance, []);

    private static void Compare(GoldenCase golden, CaseHost host, PixelTolerance? tolerance, List<string> failures)
    {

        var actual = host.Capture();
        var expected = RgbaImage.Load(Path.Combine(Repo.Goldens, golden.Png!));
        var scene = GoldenScene.Load(golden.Scene!);
        var primitives = StructuralComparison.FromVisuals(host.Window);
        var regions = PixelComparison.Classify(scene, expected.Width, expected.Height, CaseHost.Scale, primitives);
        var report = PixelComparison.Compare(expected, actual, regions, tolerance ?? PixelTolerance.Default);
        failures.AddRange(report.Failures);

        var structure = StructuralComparison.Compare(scene, primitives);
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

    /// <summary>R9: GPUI rounds text widths up to whole logical pixels, Avalonia to device pixels.</summary>
    private static bool NearWithTextRounding(Rect gpui, Rect avalonia, int textRuns)
    {
        var dw = gpui.Width - avalonia.Width;
        return dw >= -GeometryTolerance && dw < textRuns &&
            Math.Abs(gpui.X - avalonia.X) <= GeometryTolerance + dw &&
            Math.Abs(gpui.Y - avalonia.Y) <= GeometryTolerance &&
            Math.Abs(gpui.Height - avalonia.Height) <= GeometryTolerance;
    }

    public static string Fmt(Rect r) =>
        $"[{GoldenManifest.Format(r.X)}, {GoldenManifest.Format(r.Y)}, {GoldenManifest.Format(r.Width)}, {GoldenManifest.Format(r.Height)}]";
}

public sealed class VisualMismatchException(string message) : Exception(message);
