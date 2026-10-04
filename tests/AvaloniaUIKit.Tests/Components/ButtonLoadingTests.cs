using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>
/// GPUI Kit Button::loading -> Button with uikit:Buttons.IsLoading, a
/// DropdownButton's loading Button -> the SplitButton's action half, and
/// InputGroupButton::loading -> the same in a TextBox.group, whose pressed
/// button leaves the focus in the input (uikit:Buttons.TakesFocusOnPointer).
/// </summary>
public class ButtonLoadingTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["button-loading"])]
    public Task Loading_button_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }

    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["split-loading"])]
    public Task Loading_split_button_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }

    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["input-group-loading"])]
    public Task Loading_input_group_button_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden);
        return Task.CompletedTask;
    }
}
