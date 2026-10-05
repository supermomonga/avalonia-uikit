using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>Values the TabStrip and TabControl templates take from the bar.</summary>
public static class TabsConverters
{
    /// <summary>
    /// The columns of the tab row the tabs span, from uikit:Tabs.NewTabFactory:
    /// without an add button they fill the row (a TabBar's flex_1 tabs); with
    /// one, they take their own width and the button follows the last tab.
    /// </summary>
    public static readonly IValueConverter ItemsSpan =
        new FuncValueConverter<object?, int>(factory => factory is null ? 2 : 1);
}
