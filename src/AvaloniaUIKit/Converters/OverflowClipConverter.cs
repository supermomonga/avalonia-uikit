using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// A scroll presenter's clip: its bounds, widened by <see cref="Inflate"/> on
/// every side where no content is scrolled out of view. Content that overflows
/// is cut at the viewport's edge, as GPUI's overflow_x_scroll cuts it, while
/// what is drawn just outside the content (a focus ring) shows where nothing
/// is hidden. The values are the presenter's Bounds, Offset, Extent and
/// Viewport.
/// </summary>
public sealed class OverflowClipConverter : IMultiValueConverter
{
    /// <summary>How far the clip reaches past an edge with nothing hidden beyond it.</summary>
    public double Inflate { get; set; }

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 4 || values[0] is not Rect bounds || values[1] is not Vector offset ||
            values[2] is not Size extent || values[3] is not Size viewport)
        {
            return null;
        }
        const double Epsilon = 0.01;
        var left = offset.X > Epsilon ? 0 : Inflate;
        var top = offset.Y > Epsilon ? 0 : Inflate;
        var right = offset.X + viewport.Width < extent.Width - Epsilon ? 0 : Inflate;
        var bottom = offset.Y + viewport.Height < extent.Height - Epsilon ? 0 : Inflate;
        return new RectangleGeometry(new Rect(-left, -top, bounds.Width + left + right, bounds.Height + top + bottom));
    }
}
