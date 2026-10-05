using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Adapters for the navigation work: the slots added to existing controls
/// (uikit:GroupBoxes, Separators, Spinners, ProgressCircles), uikit:Pagination,
/// the carousel's buttons and pointer input, and the tab bar's parts.
/// </summary>
public static partial class Adapters
{
    /// <summary>
    /// Fills the slots a case names (reference/src/cases/display.rs and
    /// progress.rs): a group's footer, a separator's label, a spinner's icon
    /// and the content of a progress circle.
    /// </summary>
    private static T Slotted<T>(T control, GoldenCase c) where T : Control
    {
        switch (control)
        {
            case GroupBox group when c.Has("footer"):
                GroupBoxes.SetFooter(group, c.Str("footer"));
                break;
            case Separator separator when c.Has("label"):
                Separators.SetLabel(separator, c.Str("label"));
                break;
            case ProgressBar spinner when c.Component == "spinner" && c.Has("icon"):
                Spinners.SetIcon(spinner, Icon(c.Str("icon")).Data);
                break;
            case ProgressBar circle when c.Has("content"):
                ProgressCircles.SetContent(circle, new TextBlock { Text = c.Str("content"), FontWeight = FontWeight.SemiBold });
                break;
        }
        return control;
    }

    /// <summary>
    /// uikit:Pagination for the uikit-pagination cases (reference/src/cases/pagination.rs):
    /// `total` pages, `current`, `visible` pages before ellipses, the size and compact classes.
    /// </summary>
    private static global::AvaloniaUIKit.Pagination PaginationCase(GoldenCase c)
    {
        var pagination = new global::AvaloniaUIKit.Pagination
        {
            TotalPages = (int)c.Num("total", 5),
            CurrentPage = (int)c.Num("current", 3),
            VisiblePages = (int)c.Num("visible", 5),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(pagination, c, "size", "medium");
        FlagClass(pagination, c, "compact");
        return pagination;
    }
}
