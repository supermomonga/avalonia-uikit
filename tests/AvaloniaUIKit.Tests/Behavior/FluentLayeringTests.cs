using Avalonia;
using Avalonia.Themes.Fluent;
using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// The theme layered over FluentTheme (Fluent first, so the GPUI theme wins)
/// looks the same as on its own: no Fluent style leaks into a themed control.
/// One case per component, across states and both themes.
/// </summary>
public class FluentLayeringTests
{
    [Test]
    [Arguments("surface/base/normal/dark")]
    [Arguments("button/label.primary.medium/hover/light")]
    [Arguments("button/label.default.medium/focus/dark")]
    [Arguments("split/label.default.medium/normal/light")]
    [Arguments("toggle/icon.ghost.medium.checked/hover/light")]
    [Arguments("checkbox/label.medium.checked/focus/light")]
    [Arguments("radio/label.medium.checked/normal/dark")]
    [Arguments("switch/label.medium.checked/focus/light")]
    [Arguments("number/buttons.medium/at-30-24/light")]
    [Arguments("groupbox/titled.outline/normal/dark")]
    [Arguments("separator/line.dashed/normal/light")]
    [Arguments("link/text.base/hover/light")]
    [Arguments("progress/value.medium.value-40/normal/dark")]
    [Arguments("spinner/icon.medium/normal/light")]
    [Arguments("tooltip/text.base/hover+wait-800ms/light")]
    [Arguments("context/menu.base/right-click+at-110-77/light")]
    [Arguments("dropdown/menu.base/click+at-60-117/dark")]
    [Arguments("menubar/bar.base/at-20-20/light")]
    [Arguments("scroll/hover.base/at-164-20+wait-400ms/light")]
    public Task Layered_over_fluent_still_matches_gpui(string id)
    {
        Application.Current!.Styles.Insert(0, new FluentTheme());
        VisualAssert.Matches(GoldenManifest.Get(id));
        return Task.CompletedTask;
    }
}
