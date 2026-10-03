using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

public class EmptyStateTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["empty"])]
    public Task EmptyState_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
