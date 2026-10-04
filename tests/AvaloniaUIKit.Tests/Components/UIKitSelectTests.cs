using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>uikit:Select against GPUI Kit's Select (select.rs).</summary>
public class UIKitSelectTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["uikit-select"])]
    public Task UIKitSelect_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}

/// <summary>uikit:Select with the combobox class against GPUI Kit's Combobox (combobox.rs).</summary>
public class UIKitComboboxTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["uikit-combobox"])]
    public Task UIKitCombobox_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
