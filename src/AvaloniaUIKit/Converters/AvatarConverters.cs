using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>An avatar's corner radius for the parts inside and on its 1px ring.</summary>
public static class AvatarConverters
{
    /// <summary>The radius of the fill inside the ring: one pixel less.</summary>
    public static readonly IValueConverter Inner = new Shrink(1);

    /// <summary>The radius of the ring's center line: half a pixel less.</summary>
    public static readonly IValueConverter Ring = new Shrink(0.5);

    private sealed class Shrink(double by) : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            value is CornerRadius r ? new CornerRadius(Math.Max(0, r.TopLeft - by)) : value;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
