using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// Themes the standard Avalonia controls in the Nova style of shadcn/ui, in
/// light and dark.
/// </summary>
/// <remarks>
/// The theme works on its own. To keep a look for controls it does not cover,
/// add another theme such as FluentTheme before it: the later theme wins.
/// </remarks>
public class NovaTheme : Styles
{
    /// <summary>Creates the theme.</summary>
    /// <param name="serviceProvider">The XAML service provider, if any.</param>
    public NovaTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
    }
}
