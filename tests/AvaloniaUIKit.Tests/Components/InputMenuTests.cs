using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Comparison;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Components;

public class InputMenuTests
{
    [Test]
    [MethodDataSource(typeof(GoldenManifest), nameof(GoldenManifest.Cases), Arguments = ["input-menu"])]
    public Task InputMenu_matches_gpui(GoldenCase golden)
    {
        VisualAssert.Matches(golden, excluded: Shortcuts);
        return Task.CompletedTask;
    }

    /// <summary>
    /// R22: the shortcuts' modifier symbols are drawn by each renderer's fallback
    /// font (and Avalonia writes "⌘+X" where GPUI writes "⌘X"), so the end of each
    /// item from its shortcut on is not compared in pixels; its color is.
    /// </summary>
    private static IEnumerable<Rect> Shortcuts(CaseHost host)
    {
        foreach (var item in host.Window.GetVisualDescendants().OfType<MenuItem>())
        {
            var gesture = item.GetVisualDescendants().OfType<TextBlock>().FirstOrDefault(t => t.Name == "PART_InputGestureText");
            if (gesture is { IsEffectivelyVisible: true } && gesture.TranslatePoint(default, host.Window) is { } at &&
                item.TranslatePoint(default, host.Window) is { } origin)
            {
                // GPUI's text is narrower and sits at the right of the same column.
                yield return new Rect(at.X - 4, origin.Y, origin.X + item.Bounds.Width - at.X + 4, item.Bounds.Height);
            }
        }
    }
}
