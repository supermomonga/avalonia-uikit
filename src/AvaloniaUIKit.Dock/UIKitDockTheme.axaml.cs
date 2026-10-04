using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Dock look for Dock.Avalonia. Add it to the application's styles
/// after <see cref="UIKitTheme"/>, instead of Dock's DockFluentTheme or
/// DockSimpleTheme.
/// </summary>
public class UIKitDockTheme : Styles
{
    /// <summary>Loads the themes.</summary>
    public UIKitDockTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
