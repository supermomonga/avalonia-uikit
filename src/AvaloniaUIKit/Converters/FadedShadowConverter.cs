using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// <see cref="Shadows"/> with their ink scaled by the product of the bound
/// opacities raised to <see cref="Power"/>: GPUI's toast shadow ramp
/// (toast_shadow(opacity^3)), which keeps a fading card's shadow out of sight.
/// </summary>
public sealed class FadedShadowConverter : IMultiValueConverter
{
    /// <summary>The shadows at full strength.</summary>
    public BoxShadows Shadows { get; set; }

    /// <summary>The power the opacity is raised to.</summary>
    public double Power { get; set; } = 1;

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var opacity = 1d;
        foreach (var value in values)
        {
            opacity *= value is double d ? Math.Clamp(d, 0, 1) : 1;
        }
        var strength = Math.Pow(opacity, Power);
        if (strength >= 1 || Shadows.Count == 0)
        {
            return Shadows;
        }
        var faded = new BoxShadow[Shadows.Count];
        for (var i = 0; i < faded.Length; i++)
        {
            var s = Shadows[i];
            var color = s.Color;
            faded[i] = s with { Color = Color.FromArgb((byte)Math.Round(color.A * strength), color.R, color.G, color.B) };
        }
        return new BoxShadows(faded[0], faded[1..]);
    }
}
