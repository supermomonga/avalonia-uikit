using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Motion;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>
/// The tabs goldens drive a TabStrip (the motions run in MotionTests) and, the
/// same strip being part of its template, a TabControl with empty tabs.
/// </summary>
public class TabsTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["tabs"])]
    public Task TabStrip_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }

    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["tabs"])]
    public Task TabControl_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden, create: Adapters.TabControl);
        return Task.CompletedTask;
    }

    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Motions), Arguments = ["tabs"])]
    public Task TabControl_moves_as_gpui(GoldenCase golden)
    {
        MotionTests.Play(golden, Adapters.TabControl);
        return Task.CompletedTask;
    }
}
