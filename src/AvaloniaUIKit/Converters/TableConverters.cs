using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>Converters for GPUI Kit's DataTable look on TableView.</summary>
public static class TableConverters
{
    /// <summary>The rows' height: their count times the row height (rows are as tall as each other).</summary>
    public static readonly IMultiValueConverter RowsHeight = new RowsHeightConverter();

    /// <summary>
    /// The striped table's filler rows (state.rs, calculate_extra_rows_needed):
    /// from the row count, the row height and the body's height, the indexes
    /// of the whole empty rows that fit below the last row.
    /// </summary>
    public static readonly IMultiValueConverter FillerRows = new FillerRowsConverter();

    /// <summary>Whether a row index is odd: the rows a stripe fills.</summary>
    public static readonly IValueConverter IsOdd = new FuncValueConverter<int, bool>(index => index % 2 == 1);

    private sealed class RowsHeightConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [int count, double row, ..] && row > 0 ? count * row : 0d;
    }

    private sealed class FillerRowsConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is not [int count, double row, double body, ..] || !(row > 0) || !(body > count * row))
            {
                return Array.Empty<int>();
            }
            var filler = (int)Math.Floor((body - count * row) / row + 1e-6);
            return Enumerable.Range(count, filler).ToArray();
        }
    }
}
