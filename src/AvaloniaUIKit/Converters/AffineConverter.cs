using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// A number times <see cref="Scale"/> plus <see cref="Offset"/>, as a double,
/// a uniform Thickness or a uniform CornerRadius (the target's type): a spring's
/// progress as a ring's width, inset and corner radius.
/// </summary>
public sealed class AffineConverter : IValueConverter
{
    /// <summary>What the value is multiplied by.</summary>
    public double Scale { get; set; } = 1;

    /// <summary>What is added after scaling.</summary>
    public double Offset { get; set; }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not double x || double.IsNaN(x))
        {
            return Avalonia.Data.BindingOperations.DoNothing;
        }
        var y = x * Scale + Offset;
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
