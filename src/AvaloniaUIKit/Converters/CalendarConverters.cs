using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>
/// The texts GPUI Kit's calendar shows where Avalonia's shows others: the
/// header's month and year as two items, and full month names in the months
/// grid (Avalonia abbreviates them). Names come from the current culture's
/// calendar, falling back to the Gregorian one as Avalonia's own calendar does.
/// </summary>
public static class CalendarConverters
{
    /// <summary>A date's full month name ("March").</summary>
    public static readonly IValueConverter MonthName = new FuncValueConverter<DateTime, string>(date => Format().GetMonthName(date.Month));

    /// <summary>A date's year ("2025").</summary>
    public static readonly IValueConverter Year = new FuncValueConverter<DateTime, string>(date => date.Year.ToString(CultureInfo.CurrentCulture));

    /// <summary>
    /// A months-or-years grid button's text from its date and the calendar's
    /// mode: the full month name in Year mode, the year in Decade mode.
    /// </summary>
    public static readonly IMultiValueConverter GridButtonText = new GridButtonTextConverter();

    private static DateTimeFormatInfo Format()
    {
        var format = CultureInfo.CurrentCulture.DateTimeFormat;
        return format.Calendar is GregorianCalendar ? format : CultureInfo.InvariantCulture.DateTimeFormat;
    }

    private sealed class GridButtonTextConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
            values is [DateTime date, CalendarMode mode, ..]
                ? mode == CalendarMode.Decade ? date.Year.ToString(CultureInfo.CurrentCulture) : Format().GetMonthName(date.Month)
                : null;
    }
}
