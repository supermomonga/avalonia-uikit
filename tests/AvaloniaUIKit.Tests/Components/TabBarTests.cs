using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>
/// The tab bar's parts (the menu class, uikit:Tabs.Prefix and Suffix, the
/// scrolling row) on a TabStrip and, the same strip being part of its
/// template, a TabControl with empty tabs.
/// </summary>
public class TabBarTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["tabs-bar"])]
    public Task TabStrip_bar_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }

    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["tabs-bar"])]
    public Task TabControl_bar_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden, create: c => Adapters.TabBarParts(Adapters.TabControl(c), c));
        return Task.CompletedTask;
    }
}
