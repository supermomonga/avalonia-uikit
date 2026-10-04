using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// The features the standard controls already have that the theme now carries:
/// they still work through the theme's parts, with the GPUI Kit rules the look
/// follows.
/// </summary>
public class ThemeFixBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    private static T Part<T>(Visual root, string name) where T : Visual =>
        root.GetVisualDescendants().OfType<T>().First(v => (v as StyledElement)?.Name == name);

    [Test]
    public async Task A_drop_down_button_opens_its_flyout_and_shows_as_selected()
    {
        var golden = Case("dropdownbutton/open.base/click/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var button = (DropDownButton)host.Control;
        await Assert.That(Part<PathIcon>(button, "PART_Caret").IsEffectivelyVisible).IsTrue();
        host.Drive(golden, "click");
        await Assert.That(button.Flyout!.IsOpen).IsTrue();
        await Assert.That(button.Classes.Contains(":flyout-open")).IsTrue();
    }
}
