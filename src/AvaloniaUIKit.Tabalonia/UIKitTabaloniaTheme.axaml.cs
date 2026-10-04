using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Tabs look for Tabalonia's TabsControl. Add it to the
/// application's styles after <see cref="UIKitTheme"/>, instead of Tabalonia's
/// own FluentTheme or CustomTheme.
/// </summary>
public class UIKitTabaloniaTheme : Styles
{
    /// <summary>Loads the themes.</summary>
    public UIKitTabaloniaTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
