using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// Themes the standard Avalonia controls to look and move like GPUI Kit's
/// Default Light and Default Dark themes.
/// </summary>
/// <remarks>
/// The theme works on its own. To keep a look for controls it does not cover,
/// add another theme such as FluentTheme before it: the later theme wins.
/// </remarks>
public class GpuiTheme : Styles
{
    /// <summary>Creates the theme.</summary>
    /// <param name="serviceProvider">The XAML service provider, if any.</param>
    public GpuiTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
