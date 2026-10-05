using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>GPUI Kit Sidebar -> uikit:Sidebar: the sidebar cases, then the sidebar story's menu, hovered, clicked and collapsed to icons.</summary>
public class UIKitSidebarTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["uikit-sidebar"])]
    public Task Sidebar_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
