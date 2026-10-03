using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

public class TagTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["tag"])]
    public Task Tag_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
