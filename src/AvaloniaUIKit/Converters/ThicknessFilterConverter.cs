using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>The sides of a <see cref="Thickness"/> to keep.</summary>
[Flags]
public enum Sides
{
    /// <summary>No side.</summary>
    None = 0,
    /// <summary>The left side.</summary>
    Left = 1,
    /// <summary>The top side.</summary>
    Top = 2,
    /// <summary>The right side.</summary>
    Right = 4,
    /// <summary>The bottom side.</summary>
    Bottom = 8,
}

/// <summary>
/// Keeps only the <see cref="Filter"/> sides of a thickness and zeroes the
/// others, as CornerRadiusFilterConverter does for corners: a grouped button
/// drops the edge it shares with the button before it.
/// </summary>
public sealed class ThicknessFilterConverter : IValueConverter
{
    /// <summary>The sides to keep.</summary>
    public Sides Filter { get; set; }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is Thickness t
            ? new Thickness(
                Filter.HasFlag(Sides.Left) ? t.Left : 0,
                Filter.HasFlag(Sides.Top) ? t.Top : 0,
                Filter.HasFlag(Sides.Right) ? t.Right : 0,
                Filter.HasFlag(Sides.Bottom) ? t.Bottom : 0)
            : value;

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
