using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>The first of the bound values that is not null (a preview before the committed value).</summary>
public sealed class FirstNonNullConverter : IMultiValueConverter
{
    /// <summary>The shared instance.</summary>
    public static readonly FirstNonNullConverter Instance = new();

    /// <inheritdoc />
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.FirstOrDefault(v => v is not null && v is not Avalonia.AvaloniaProperty && v != Avalonia.AvaloniaProperty.UnsetValue);
}
