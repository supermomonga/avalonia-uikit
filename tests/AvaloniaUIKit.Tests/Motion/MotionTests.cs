using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Motion;

/// <summary>
/// GPUI motions, frame by frame: the control is put in the motion's starting
/// state, the trigger is played, and the virtual clock is moved to each moment
/// GPUI recorded, where Avalonia's own animator must paint the same frame.
/// </summary>
public class MotionTests
{
    public static IEnumerable<Func<GoldenCase>> All() =>
        GoldenManifest.All.Where(c => c.Motion is not null).Select(c => (Func<GoldenCase>)(() => c));

    [Test]
    [MethodDataSource(nameof(All))]
    public Task Frames_match_gpui(GoldenCase golden)
    {
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, golden.Motion!.From);
        host.Drive(golden, golden.Motion.Trigger);
        Adapters.AfterDrive(golden, host);
        var failures = new List<string>();
        var elapsed = 0;
        foreach (var frame in golden.Motion.Frames)
        {
            VirtualTime.Advance(TimeSpan.FromMilliseconds(frame.TimeMs - elapsed));
            elapsed = frame.TimeMs;
            var frameCase = golden with { Png = frame.Png, Scene = frame.Scene, Id = $"{golden.Id}/{frame.TimeMs}ms" };
            try
            {
                VisualAssert.MatchesPosed(frameCase, host, MotionTolerance.For(golden, GoldenScene.Load(frame.Scene)));
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
}
