using Avalonia.Data.Converters;

namespace AvaloniaUIKit;

/// <summary>Converters for the Dock theme's templates.</summary>
public static class DockConverters
{
    /// <summary>
    /// A group's dockable count to whether it shows a tab bar: GPUI Kit's tab
    /// panel shows tabs for two or more panels and a title bar for one.
    /// </summary>
    public static readonly IValueConverter HasTabs = new FuncValueConverter<int, bool>(count => count > 1);

    /// <summary>A group's dockable count to whether it shows a title bar (one panel).</summary>
    public static readonly IValueConverter HasTitle = new FuncValueConverter<int, bool>(count => count == 1);

    /// <summary>
    /// A drop indicator's opacity to whether its fill shows: Dock sets the active
    /// indicator's opacity (0.5), and the theme paints GPUI's drop target color at
    /// full strength on a sibling instead.
    /// </summary>
    public static readonly IValueConverter IsShown = new FuncValueConverter<double, bool>(opacity => opacity > 0);
}
