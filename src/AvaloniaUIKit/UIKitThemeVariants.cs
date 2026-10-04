using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// The color themes GPUI Kit bundles besides its default one (Aurora, Ayu, Catppuccin,
/// Tokyo Night and the rest), as theme variants of <see cref="UIKitTheme"/>. Avalonia's
/// <see cref="ThemeVariant.Light"/> and <see cref="ThemeVariant.Dark"/> are GPUI Kit's
/// Default Light and Default Dark.
/// </summary>
/// <remarks>
/// Set a variant as the <c>RequestedThemeVariant</c> of the application, a window or a
/// <see cref="Avalonia.Controls.ThemeVariantScope"/>. Each variant inherits Light or Dark by
/// its mode, so resources that other themes (FluentTheme) define for those still apply.
/// </remarks>
public static partial class UIKitThemeVariants
{
    /// <summary>Every bundled variant, ordered by name.</summary>
    public static IReadOnlyList<ThemeVariant> All => field ??= [.. Bundled.Select(b => b.Variant)];

    /// <summary>
    /// Finds a variant by its GPUI Kit name, ignoring case: a bundled theme (<c>Ayu Dark</c>),
    /// or <c>Default Light</c> and <c>Default Dark</c> for <see cref="ThemeVariant.Light"/> and
    /// <see cref="ThemeVariant.Dark"/>.
    /// </summary>
    /// <returns>The variant, or null when no theme has the name.</returns>
    public static ThemeVariant? Find(string name)
    {
        if (string.Equals(name, "Default Light", StringComparison.OrdinalIgnoreCase))
        {
            return ThemeVariant.Light;
        }
        if (string.Equals(name, "Default Dark", StringComparison.OrdinalIgnoreCase))
        {
            return ThemeVariant.Dark;
        }
        return All.FirstOrDefault(v => string.Equals((string)v.Key, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Whether a variant is dark: <see cref="ThemeVariant.Dark"/>, or one that inherits it.</summary>
    public static bool IsDark(ThemeVariant? variant)
    {
        for (var v = variant; v is not null; v = v.InheritVariant)
        {
            if (v == ThemeVariant.Dark)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>The palette of each bundled variant.</summary>
    internal static IEnumerable<(ThemeVariant Variant, Palette Palette)> Palettes => Bundled;
}
