using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaloniaUIKit;

/// <summary>
/// The colors GPUI Kit's color picker derives from the picked color
/// (theme/color.rs): its hex text, and the color darkened or lightened by
/// scaling its HSL lightness.
/// </summary>
public static class ColorPickerConverters
{
    /// <summary>The color as a brush.</summary>
    public static readonly IValueConverter Brush = new ShadeConverter(0);

    /// <summary><c>darken(0.1)</c>: a palette swatch's border.</summary>
    public static readonly IValueConverter Darken10 = new ShadeConverter(-0.1);

    /// <summary><c>darken(0.2)</c>: a pressed swatch, the preview swatch's border.</summary>
    public static readonly IValueConverter Darken20 = new ShadeConverter(-0.2);

    /// <summary><c>darken(0.3)</c>: the trigger swatch's border, a hovered swatch's border.</summary>
    public static readonly IValueConverter Darken30 = new ShadeConverter(-0.3);

    /// <summary><c>darken(0.5)</c>: a pressed swatch's border.</summary>
    public static readonly IValueConverter Darken50 = new ShadeConverter(-0.5);

    /// <summary><c>lighten(0.1)</c>: a hovered swatch.</summary>
    public static readonly IValueConverter Lighten10 = new ShadeConverter(0.1);

    /// <summary>
    /// <c>Colorize::to_hex</c>: <c>#RRGGBB</c> in capitals, <c>#RRGGBBAA</c>
    /// while the color is translucent.
    /// </summary>
    public static readonly IValueConverter Hex = new FuncValueConverter<Color, string>(color =>
        color.A < 255
            ? string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}")
            : string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}"));

    /// <summary>The first component's name in the color model.</summary>
    public static readonly IValueConverter FirstComponent = new FuncValueConverter<ColorModel, string>(model => model == ColorModel.Hsva ? "Hue" : "Red");

    /// <summary>The second component's name in the color model.</summary>
    public static readonly IValueConverter SecondComponent = new FuncValueConverter<ColorModel, string>(model => model == ColorModel.Hsva ? "Saturation" : "Green");

    /// <summary>The third component's name in the color model.</summary>
    public static readonly IValueConverter ThirdComponent = new FuncValueConverter<ColorModel, string>(model => model == ColorModel.Hsva ? "Value" : "Blue");

    /// <summary>The lightness scaled by 1 + factor (darken is a negative factor), as a brush.</summary>
    public static Color Shade(Color color, double factor)
    {
        if (factor == 0)
        {
            return color;
        }
        var hsl = color.ToHsl();
        return new HslColor(hsl.A, hsl.H, hsl.S, Math.Clamp(hsl.L * (1 + factor), 0, 1)).ToRgb();
    }

    private sealed class ShadeConverter(double factor) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is Color color ? new ImmutableSolidColorBrush(Shade(color, factor)) : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
