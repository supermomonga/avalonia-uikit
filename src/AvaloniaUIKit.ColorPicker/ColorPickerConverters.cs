using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AvaloniaUIKit.Converters;

namespace AvaloniaUIKit;

/// <summary>
/// The colors GPUI Kit's color picker derives from the picked color
/// (theme/color.rs): its hex text, and the color darkened or lightened by
/// scaling its HSL lightness. The core's <see cref="ColorConverters"/>, which
/// uikit:ColorSelect uses, does the math; the component names are ColorView's.
/// </summary>
public static class ColorPickerConverters
{
    /// <summary>The color as a brush.</summary>
    public static readonly IValueConverter Brush = ColorConverters.Brush;

    /// <summary><c>darken(0.1)</c>: a palette swatch's border.</summary>
    public static readonly IValueConverter Darken10 = ColorConverters.Darken10;

    /// <summary><c>darken(0.2)</c>: a pressed swatch, the preview swatch's border.</summary>
    public static readonly IValueConverter Darken20 = ColorConverters.Darken20;

    /// <summary><c>darken(0.3)</c>: the trigger swatch's border, a hovered swatch's border.</summary>
    public static readonly IValueConverter Darken30 = ColorConverters.Darken30;

    /// <summary><c>darken(0.5)</c>: a pressed swatch's border.</summary>
    public static readonly IValueConverter Darken50 = ColorConverters.Darken50;

    /// <summary><c>lighten(0.1)</c>: a hovered swatch.</summary>
    public static readonly IValueConverter Lighten10 = ColorConverters.Lighten10;

    /// <summary>
    /// <c>Colorize::to_hex</c>: <c>#RRGGBB</c> in capitals, <c>#RRGGBBAA</c>
    /// while the color is translucent.
    /// </summary>
    public static readonly IValueConverter Hex = ColorConverters.Hex;

    /// <summary>The first component's name in the color model.</summary>
    public static readonly IValueConverter FirstComponent = new FuncValueConverter<ColorModel, string>(model => model == ColorModel.Hsva ? "Hue" : "Red");

    /// <summary>The second component's name in the color model.</summary>
    public static readonly IValueConverter SecondComponent = new FuncValueConverter<ColorModel, string>(model => model == ColorModel.Hsva ? "Saturation" : "Green");

    /// <summary>The third component's name in the color model.</summary>
    public static readonly IValueConverter ThirdComponent = new FuncValueConverter<ColorModel, string>(model => model == ColorModel.Hsva ? "Value" : "Blue");

    /// <summary>The lightness scaled by 1 + factor (darken is a negative factor).</summary>
    public static Color Shade(Color color, double factor) => ColorConverters.Shade(color, factor);
}
