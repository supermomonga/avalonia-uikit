using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// Themes the standard Avalonia controls in the look of GPUI Kit, in its Default Light
/// and Default Dark colors and in the color themes it bundles (<see cref="UIKitThemeVariants"/>).
/// </summary>
/// <remarks>
/// The theme works on its own. To keep a look for controls it does not cover,
/// add another theme such as FluentTheme before it: the later theme wins.
/// </remarks>
public class UIKitTheme : Styles
{
    /// <summary>Creates the theme.</summary>
    /// <param name="serviceProvider">The XAML service provider, if any.</param>
    public UIKitTheme(IServiceProvider? serviceProvider = null)
    {
        AvaloniaXamlLoader.Load(serviceProvider, this);
        Resources.MergedDictionaries.Insert(0, Colors());
    }

    // The color resources (Palettes.g.cs): Default Light under Default, the fallback of every
    // variant as in Avalonia's own themes, Default Dark under Dark, each bundled theme under its
    // variant. They come first among the merged dictionaries, so they are looked up last.
    private static ResourceDictionary Colors()
    {
        var colors = new ResourceDictionary();
        colors.ThemeDictionaries[ThemeVariant.Default] = new PaletteResources(Palette.DefaultLight);
        colors.ThemeDictionaries[ThemeVariant.Dark] = new PaletteResources(Palette.DefaultDark);
        foreach (var (variant, palette) in UIKitThemeVariants.Palettes)
        {
            colors.ThemeDictionaries[variant] = new PaletteResources(palette);
        }
        return colors;
    }
}
