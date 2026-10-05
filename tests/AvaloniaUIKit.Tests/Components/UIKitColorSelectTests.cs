using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>
/// GPUI Kit ColorPicker / ColorSelect -> uikit:ColorSelect: the color_picker
/// cases, no color, an icon, the open popover's palette and HSLA panels, the
/// preview and a committed color.
/// </summary>
public class UIKitColorSelectTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["uikit-colorselect"])]
    public Task ColorSelect_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
