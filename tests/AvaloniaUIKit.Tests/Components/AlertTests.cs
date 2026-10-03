using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

public class AlertTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["alert"])]
    public Task Alert_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
