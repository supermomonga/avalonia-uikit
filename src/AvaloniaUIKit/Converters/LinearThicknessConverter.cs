using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// A number as a Thickness: <see cref="Base"/> plus <see cref="Step"/> times
/// the number, side by side. A badge moves out by a few pixels per character.
/// </summary>
public sealed class LinearThicknessConverter : IValueConverter
{
    /// <summary>The thickness at zero.</summary>
    public Thickness Base { get; set; }

    /// <summary>What each unit adds.</summary>
    public Thickness Step { get; set; }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var n = value switch
        {
            int i => i,
            double d => d,
            _ => 0,
        };
        return new Thickness(Base.Left + Step.Left * n, Base.Top + Step.Top * n, Base.Right + Step.Right * n, Base.Bottom + Step.Bottom * n);
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
