using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>The dock goldens drive Dock.Avalonia's DockControl with the dock story's groups.</summary>
public class DockTests
{
    // R5: the drag preview is a card at 75%. GPUI fades its fill and border one by
    // one, Avalonia the card as a whole, so the border over the fill reads lighter.
    private static readonly PixelTolerance TranslucentCard = PixelTolerance.Default with { FlatMax = 13, BandMassFloor = double.PositiveInfinity };

    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["dock"])]
    public Task DockControl_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden, golden.Group == "drop" ? TranslucentCard : null);
        return Task.CompletedTask;
    }
}
