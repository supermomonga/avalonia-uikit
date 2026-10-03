using System.Globalization;
using AvaloniaUIKit.Tests.Infrastructure;

namespace AvaloniaUIKit.Tests.Comparison;

/// <summary>
/// Appends every comparison's region statistics to tests/artifacts/pixel-stats.csv
/// when AVALONIA_UIKIT_CALIBRATE is set, for calibrating the tolerances.
/// </summary>
public static class Calibration
{
    private static readonly bool Enabled = Environment.GetEnvironmentVariable("AVALONIA_UIKIT_CALIBRATE") is { Length: > 0 };
    private static readonly object Gate = new();

    public static void RecordInk(string id, (double Expected, double Actual) mass)
    {
        if (!Enabled)
        {
            return;
        }
        lock (Gate)
        {
            Directory.CreateDirectory(Repo.Artifacts);
            File.AppendAllText(Path.Combine(Repo.Artifacts, "ink-mass.csv"),
                string.Create(CultureInfo.InvariantCulture, $"{id},{mass.Expected:0},{mass.Actual:0}") + Environment.NewLine);
        }
    }

    public static void Record(string id, PixelReport report)
    {
        if (!Enabled)
        {
            return;
        }
        var path = Path.Combine(Repo.Artifacts, "pixel-stats.csv");
        var line = string.Join(",", new[] { id }.Concat(report.Stats.SelectMany(kv => new[]
        {
            kv.Value.Count.ToString(CultureInfo.InvariantCulture),
            kv.Value.Max.ToString("0.##", CultureInfo.InvariantCulture),
            kv.Value.Mean.ToString("0.###", CultureInfo.InvariantCulture),
            kv.Value.SignedMean.ToString("0.###", CultureInfo.InvariantCulture),
        })));
        lock (Gate)
        {
            Directory.CreateDirectory(Repo.Artifacts);
            File.AppendAllText(path, line + Environment.NewLine);
        }
    }
}
