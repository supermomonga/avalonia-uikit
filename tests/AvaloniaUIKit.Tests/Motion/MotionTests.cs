using Avalonia.Threading;
using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Motion;

/// <summary>
/// GPUI motions, verified in three parts because Avalonia offers no virtual
/// clock to render a transition at a chosen time (R7):
/// (a) the curve the theme declares equals GPUI's recorded trajectory;
/// (b) a control posed at the curve's value equals GPUI's frame at that time;
/// (c) after the motion has run in real time, the end state equals GPUI's.
/// </summary>
public class MotionTests
{
    public static IEnumerable<Func<GoldenCase>> All() =>
        GoldenManifest.All.Where(c => c.Motion is not null && IsProbed(c.Component)).Select(c => (Func<GoldenCase>)(() => c));

    private static bool IsProbed(string component) => component is "switch" or "checkbox";

    private static bool TowardOn(GoldenCase golden) => !golden.Bool("checked");

    [Test]
    [MethodDataSource(nameof(All))]
    public Task Declared_curve_matches_gpui_trajectory(GoldenCase golden)
    {
        var probe = MotionProbes.For(golden.Component);
        using var host = CaseHost.Open(golden, Adapters.Create(golden), freezeMotion: false);
        host.Drive(golden, golden.Motion!.From);
        var declared = probe.Declared(host, TowardOn(golden));
        var failures = new List<string>();
        foreach (var frame in golden.Motion.Frames)
        {
            var gpui = probe.FromGpui(GoldenScene.Load(frame.Scene), golden) + declared.From;
            var avalonia = declared.ValueAt(TimeSpan.FromMilliseconds(frame.TimeMs));
            if (Math.Abs(gpui - avalonia) > probe.Tolerance)
            {
                failures.Add($"t={frame.TimeMs}ms: theme {avalonia:0.####} != gpui {gpui:0.####}");
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
        var probe = MotionProbes.For(golden.Component);
        var failures = new List<string>();
        foreach (var frame in golden.Motion!.Frames)
        {
            using var host = CaseHost.Open(golden, Adapters.Create(golden), freezeMotion: false);
            host.Drive(golden, golden.Motion.From);
            var declared = probe.Declared(host, TowardOn(golden));
            host.FreezeMotion();
            host.Drive(golden, golden.Motion.Trigger);
            probe.Apply(host, declared.ValueAt(TimeSpan.FromMilliseconds(frame.TimeMs)));
            var frameCase = golden with { Png = frame.Png, Scene = frame.Scene, Id = $"{golden.Id}/{frame.TimeMs}ms" };
            try
            {
                VisualAssert.MatchesPosed(frameCase, host);
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
    public async Task End_state_after_real_time_matches_gpui(GoldenCase golden)
    {
        var probe = MotionProbes.For(golden.Component);
        using var host = CaseHost.Open(golden, Adapters.Create(golden), freezeMotion: false);
        host.Drive(golden, golden.Motion!.From);
        var declared = probe.Declared(host, TowardOn(golden));
        host.Drive(golden, golden.Motion.Trigger);
        var deadline = DateTime.UtcNow + declared.Duration + TimeSpan.FromMilliseconds(250);
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(16);
            host.Flush();
        }
        var last = golden.Motion.Frames[^1];
        VisualAssert.MatchesPosed(golden with { Png = last.Png, Scene = last.Scene, Id = $"{golden.Id}/end" }, host);
    }
}
