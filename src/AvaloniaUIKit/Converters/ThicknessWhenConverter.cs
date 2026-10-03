using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// The second bound value (a Thickness) when the first is true, otherwise none:
/// a command's padding while its label shows, none for a square icon button.
/// </summary>
public sealed class ThicknessWhenConverter : IMultiValueConverter
{
    /// <summary>The shared instance.</summary>
    public static readonly ThicknessWhenConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [true, Thickness thickness, ..] ? thickness : default(Thickness);
}
