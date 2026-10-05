using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// The colors GPUI Kit's color picker derives from a color (color_picker.rs,
/// theme/color.rs): the color as a brush, darkened or lightened by scaling its
/// HSL lightness, and its hex text. A missing color (null) gives null.
/// </summary>
public static class ColorConverters
{
    /// <summary>The color as a brush.</summary>
    public static readonly IValueConverter Brush = new ShadeConverter(0);

    /// <summary><c>darken(0.1)</c>: a palette swatch's border.</summary>
    public static readonly IValueConverter Darken10 = new ShadeConverter(-0.1f);

    /// <summary><c>darken(0.2)</c>: a pressed swatch, the preview swatch's border.</summary>
    public static readonly IValueConverter Darken20 = new ShadeConverter(-0.2f);

    /// <summary><c>darken(0.3)</c>: the trigger swatch's border, a hovered swatch's border.</summary>
    public static readonly IValueConverter Darken30 = new ShadeConverter(-0.3f);

    /// <summary><c>darken(0.5)</c>: a pressed swatch's border.</summary>
    public static readonly IValueConverter Darken50 = new ShadeConverter(-0.5f);

    /// <summary><c>lighten(0.1)</c>: a hovered swatch.</summary>
    public static readonly IValueConverter Lighten10 = new ShadeConverter(0.1f);

    /// <summary>
    /// <c>Colorize::to_hex</c>: <c>#RRGGBB</c> in capitals, <c>#RRGGBBAA</c>
    /// while the color is translucent.
    /// </summary>
    public static readonly IValueConverter Hex = new FuncValueConverter<Color?, string?>(color => color is { } c ? GpuiColor.Hex(c) : null);

    private sealed class ShadeConverter(float factor) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is Color color ? new ImmutableSolidColorBrush(GpuiColor.Shade(color, factor)) : null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
