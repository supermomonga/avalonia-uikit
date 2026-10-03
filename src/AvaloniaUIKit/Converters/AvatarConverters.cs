using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// Corner radii that make a part a circle (GPUI's rounded_full) from its own
/// bounds, whatever size the app gives it.
/// </summary>
public static class AvatarConverters
{
    /// <summary>Half the shorter side.</summary>
    public static readonly IValueConverter Circle = new Half(0);

    /// <summary>The center line of a 1px ring: half a pixel less.</summary>
    public static readonly IValueConverter Ring = new Half(0.5);

    /// <summary>The center line of a border as thick as the converter parameter: half of it less.</summary>
    public static readonly IValueConverter Border = new Half(double.NaN);

    private sealed class Half(double by) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var inset = double.IsNaN(by) ? System.Convert.ToDouble(parameter, CultureInfo.InvariantCulture) / 2 : by;
            return value is Rect r ? new CornerRadius(Math.Max(0, Math.Min(r.Width, r.Height) / 2 - inset)) : value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
