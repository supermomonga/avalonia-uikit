using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

public class LabelTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["label"])]
    public Task Label_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
