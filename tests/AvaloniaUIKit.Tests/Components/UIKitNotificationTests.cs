using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

public class UIKitNotificationTests
{
    // R3: where the shadows of stacked cards overlap, the differences of the
    // blur approximation add up (13 measured where one card's stay within 12).
    private static readonly PixelTolerance Stacked = PixelTolerance.Default with { ShadowMax = 14 };

    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["uikit-notification"])]
    public Task NotificationList_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden, golden.Num("stack", 0) > 1 ? Stacked : null);
        return Task.CompletedTask;
    }
}
