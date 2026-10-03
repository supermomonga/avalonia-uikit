using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// Converters for a measured reveal (GPUI Kit's MotionReveal): a panel whose
/// layout height is its content's natural height times a spring's progress.
/// </summary>
public static class RevealConverters
{
    /// <summary>Whether something is open, as the spring's target: 1 or 0.</summary>
    public static readonly IValueConverter Unit = new FuncValueConverter<bool, double>(open => open ? 1 : 0);

    /// <summary>The product of the bound numbers: the natural height times the progress.</summary>
    public static readonly IMultiValueConverter Product = new ProductConverter();

    /// <summary>Shown while open (the first value) or while the progress (the second) is above 0.</summary>
    public static readonly IMultiValueConverter Shown = new ShownConverter();

    private sealed class ProductConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            var product = 1d;
            foreach (var value in values)
            {
                if (value is not double d || double.IsNaN(d))
                {
                    return 0d;
                }
                product *= d;
            }
            return product;
        }
    }

    private sealed class ShownConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [true, ..] || values is [_, double progress, ..] && progress > 0;
    }
}
