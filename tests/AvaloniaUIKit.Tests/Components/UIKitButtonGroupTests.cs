using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>GPUI Kit ButtonGroup -> uikit:ButtonGroup: the buttongroup cases, selected buttons and a selecting click.</summary>
public class UIKitButtonGroupTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["uikit-buttongroup"])]
    public Task ButtonGroup_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
