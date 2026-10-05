using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Adapters for uikit:Icon (reference/src/cases/icon.rs).</summary>
public static partial class Adapters
{
    /// <summary>The IconName of a GPUI icon file name ("chevron-down" is ChevronDown).</summary>
    public static IconName IconKind(string name) =>
        Enum.Parse<IconName>(string.Concat(name.Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..])));

    /// <summary>
    /// The uikit-icon cases: a uikit:Icon by Kind with the icon cases' size
    /// class, color and rotation, or every IconName in a grid of 12 a row.
    /// </summary>
    public static Control IconKindCase(GoldenCase c)
    {
        if (c.Bool("grid"))
        {
            var side = c.Str("size", "medium") switch { "xsmall" => 12, "small" => 14, "large" => 24, _ => 16 };
            var grid = new WrapPanel { Width = 12 * side + 11 * 8, ItemSpacing = 8, LineSpacing = 8 };
            foreach (var kind in Enum.GetValues<IconName>())
            {
                var item = new AvaloniaUIKit.Icon { Kind = kind };
                ClassFrom(item, c, "size", "medium");
                grid.Children.Add(item);
            }
            return grid;
        }
        var icon = new AvaloniaUIKit.Icon { Kind = IconKind(c.Str("icon")) };
        if (c.Has("size"))
        {
            ClassFrom(icon, c, "size", "medium");
        }
        if (c.Has("color"))
        {
            icon.Foreground = ThemeBrush(c, "UIKit." + string.Concat(c.Str("color").Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..])));
        }
        if (c.Has("rotate"))
        {
            icon.RenderTransform = new RotateTransform(c.Num("rotate", 0));
        }
        return icon;
    }
}
