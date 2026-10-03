using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// An element's bounds as a rounded clip in its own space, the radius clamped
/// to half the shorter side as GPUI clamps an image's corner radii.
/// </summary>
public sealed class RoundedClipConverter : IValueConverter
{
    /// <summary>The corner radius.</summary>
    public double Radius { get; set; }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Rect bounds || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return null;
        }
        var r = Math.Min(Radius, Math.Min(bounds.Width, bounds.Height) / 2);
        return new RectangleGeometry(new Rect(bounds.Size), r, r);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
