using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// Whether every bound number is above <see cref="Threshold"/>: a shrinking
/// shape GPUI stops drawing once it is that small (a resize handle's pill at
/// half a pixel).
/// </summary>
public sealed class AboveConverter : IMultiValueConverter
{
    /// <summary>The size every value must exceed.</summary>
    public double Threshold { get; set; }

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.All(v => v is double d && (double.IsNaN(d) || d > Threshold));
}
