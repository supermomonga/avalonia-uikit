using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

public class SliderTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["slider"])]
    public Task Slider_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
