using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// A number times <see cref="Scale"/> plus <see cref="Offset"/>, at most
/// <see cref="Maximum"/>, as a double, a uniform Thickness or a uniform
/// CornerRadius (the target's type): a spring's progress as a ring's width,
/// inset and corner radius, or a percentage as degrees.
/// </summary>
public sealed class AffineConverter : IValueConverter
{
    /// <summary>What the value is multiplied by.</summary>
    public double Scale { get; set; } = 1;

    /// <summary>What is added after scaling.</summary>
    public double Offset { get; set; }

    /// <summary>The largest result.</summary>
    public double Maximum { get; set; } = double.PositiveInfinity;

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double x || double.IsNaN(x))
        {
            return Avalonia.Data.BindingOperations.DoNothing;
        }
        var y = Math.Min(x * Scale + Offset, Maximum);
        if (targetType == typeof(Thickness))
        {
            return new Thickness(y);
        }
        if (targetType == typeof(CornerRadius))
        {
            return new CornerRadius(y);
        }
        return y;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
