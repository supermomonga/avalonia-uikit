using Avalonia.Media;
using Avalonia.Styling;

namespace AvaloniaUIKit.Demo.ControlCatalog.Code;

/// <summary>
/// A theme's code colors, as the site's code blocks take them (sites/app/lib/highlight.ts):
/// tags and keywords, attribute names and types, strings, comments, numbers and calls.
/// </summary>
public sealed record CodePalette(uint Background, uint Foreground, uint Keyword, uint String, uint Comment, uint Function, uint Type)
{
    /// <summary>The palette of <paramref name="variant"/>: its GPUI Kit theme's, else Default Light's or Default Dark's.</summary>
    public static CodePalette For(ThemeVariant? variant)
    {
        var name = variant?.Key as string;
        if (name is not null && Catalog.CodePalettes.TryGetValue(name, out var palette))
        {
            return palette;
        }
        return Catalog.CodePalettes[UIKitThemeVariants.IsDark(variant) ? "Default Dark" : "Default Light"];
    }

    /// <summary>The brush of a color of this palette.</summary>
    public static IBrush Brush(uint argb) => new SolidColorBrush(Color.FromUInt32(argb));

    /// <summary>The background of changed code: the attribute color, faint.</summary>
    public uint Changed => (Type & 0x00FFFFFF) | 0x33000000;
}
