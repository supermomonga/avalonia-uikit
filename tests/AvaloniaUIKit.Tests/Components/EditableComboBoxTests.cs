using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Components;

/// <summary>
/// An editable ComboBox against GPUI's Select: the field types into the same
/// input frame, its text and placeholder where the Select's title is.
/// </summary>
public class EditableComboBoxTests
{
    public static IEnumerable<Func<GoldenCase>> Cases() =>
        GoldenManifest.All
            .Where(c => c.Component == "select" && c.Motion is null && c.Group is "closed" or "placeholder")
            .Select(c => (Func<GoldenCase>)(() => c));

    [Test]
    [MethodDataSource(nameof(Cases))]
    public Task Editable_ComboBox_matches_gpui_select(GoldenCase golden)
    {
        VisualAssert.Matches(golden, create: Adapters.EditableSelect);
        return Task.CompletedTask;
    }
}
