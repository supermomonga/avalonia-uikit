using System.Diagnostics;
using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Motion;

/// <summary>
/// GPUI motions, verified in three parts because Avalonia offers no virtual
/// clock to render an animation at a chosen time (R7):
/// (a) the motion the theme declares equals GPUI's recorded trajectory;
/// (b) a control posed at the declared values equals GPUI's frame at that time;
/// (c) running in real time, the control follows the declared motion: a
///     transition ends where GPUI's does, a repeating animation tracks its curve.
/// </summary>
public class MotionTests
{
    public static IEnumerable<Func<GoldenCase>> All() =>
        GoldenManifest.All.Where(c => c.Motion is not null && MotionProbes.Has(c)).Select(c => (Func<GoldenCase>)(() => c));

    [Test]
    [MethodDataSource(nameof(All))]
    public Task Declared_motion_matches_gpui_trajectory(GoldenCase golden)
    {
        var probe = MotionProbes.For(golden);
        using var host = CaseHost.Open(golden, Adapters.Create(golden), freezeMotion: false);
        host.Drive(golden, golden.Motion!.From);
        var declared = probe.Declared(host, golden);
        var failures = new List<string>();
        foreach (var frame in golden.Motion.Frames)
        {
            var gpui = probe.FromGpui(GoldenScene.Load(frame.Scene), golden);
            var avalonia = declared.ValueAt(TimeSpan.FromMilliseconds(frame.TimeMs));
            for (var i = 0; i < gpui.Length; i++)
            {
                if (Math.Abs(gpui[i] - avalonia[i]) > probe.Tolerance)
                {
                    failures.Add($"t={frame.TimeMs}ms [{i}]: theme {avalonia[i]:0.####} != gpui {gpui[i]:0.####}");
                }
            }
        }
        if (failures.Count > 0)
        {
            throw new VisualMismatchException($"{golden.Id}: the declared motion leaves GPUI's trajectory\n  " + string.Join("\n  ", failures));
        }
        return Task.CompletedTask;
    }

    [Test]
    [MethodDataSource(nameof(All))]
    public Task Posed_frames_match_gpui(GoldenCase golden)
    {
        var probe = MotionProbes.For(golden);
        IDeclaredMotion declared;
        using (var reader = CaseHost.Open(golden, Adapters.Create(golden), freezeMotion: false))
        {
            reader.Drive(golden, golden.Motion!.From);
            declared = probe.Declared(reader, golden);
        }
        var failures = new List<string>();
        foreach (var frame in golden.Motion.Frames)
        {
            using var host = CaseHost.Open(golden, Adapters.Create(golden));
            host.Drive(golden, golden.Motion.From);
            host.Drive(golden, golden.Motion.Trigger);
            var values = declared.ValueAt(TimeSpan.FromMilliseconds(frame.TimeMs));
            probe.Apply(host, values);
            var frameCase = golden with { Png = frame.Png, Scene = frame.Scene, Id = $"{golden.Id}/{frame.TimeMs}ms" };
            try
            {
                VisualAssert.MatchesPosed(frameCase, host, probe.PixelToleranceFor(host, values));
            }
            catch (VisualMismatchException e)
            {
                failures.Add(e.Message);
            }
        }
        if (failures.Count > 0)
        {
            throw new VisualMismatchException(string.Join("\n", failures));
        }
        return Task.CompletedTask;
    }

    [Test]
    [MethodDataSource(nameof(All))]
    public async Task Runs_in_real_time_as_declared(GoldenCase golden)
    {
        var probe = MotionProbes.For(golden);
        var clock = Stopwatch.StartNew();
        using var host = CaseHost.Open(golden, Adapters.Create(golden), freezeMotion: false);
        host.Drive(golden, golden.Motion!.From);
        var declared = probe.Declared(host, golden);
        if (declared.Settle > TimeSpan.Zero)
        {
            // A transition: after it has run, the control rests where GPUI's does.
            host.Drive(golden, golden.Motion.Trigger);
            var deadline = DateTime.UtcNow + declared.Settle + TimeSpan.FromMilliseconds(250);
            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(16);
                host.Flush();
            }
            var last = golden.Motion.Frames[^1];
            VisualAssert.MatchesPosed(golden with { Png = last.Png, Scene = last.Scene, Id = $"{golden.Id}/end" }, host);
            return;
        }

        // A repeating animation: every live sample lies on the declared curve at a
        // time near the elapsed time (the start is only known to a frame or two).
        var failures = new List<string>();
        for (var sample = 0; sample < 12; sample++)
        {
            await Task.Delay(83);
            host.Flush();
            var live = probe.Live(host);
            var elapsed = clock.Elapsed;
            var best = double.MaxValue;
            for (var dt = -150; dt <= 50; dt++)
            {
                var expected = declared.ValueAt(elapsed + TimeSpan.FromMilliseconds(dt));
                best = Math.Min(best, live.Zip(expected, (a, b) => Math.Abs(a - b)).Max());
            }
            if (best > 1.0)
            {
                failures.Add($"at {elapsed.TotalMilliseconds:0}ms the live values ({string.Join(", ", live.Select(v => v.ToString("0.##")))}) are {best:0.##} off the declared curve");
            }
        }
        if (failures.Count > 0)
        {
            throw new VisualMismatchException($"{golden.Id}:\n  " + string.Join("\n  ", failures));
        }
    }
}
