using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Adapters for the theme's support of features the standard controls already
/// have: DropDownButton, and the new parameters of the existing cases.
/// </summary>
public static partial class Adapters
{
    /// <summary>
    /// reference/src/cases/button.rs with `dropdown_caret`: a DropDownButton with the
    /// Button's classes, a fixed width, and the standard menu below it.
    /// </summary>
    private static DropDownButton DropDownButtonCase(GoldenCase c)
    {
        var button = new DropDownButton
        {
            Content = c.Str("label", "Open"),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(button, c, "variant", "default");
        ClassFrom(button, c, "size", "medium");
        FlagClass(button, c, "outline");
        FlagClass(button, c, "selected");
        if (c.Has("width"))
        {
            button.Width = c.Num("width", 0);
        }
        if (c.Bool("menu"))
        {
            button.Flyout = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft, ItemsSource = StandardMenu() };
        }
        return button;
    }
}
