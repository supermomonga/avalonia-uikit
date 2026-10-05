using System.Globalization;
using Avalonia.Controls;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Adapters for CalendarView, TimeField and DateField: the parameters of
/// reference/src/cases/time.rs on the controls this library adds (ADR 30).
/// </summary>
public static partial class Adapters
{
    /// <summary>"YYYY-MM-DD" (anything else is no date), as time.rs reads its date parameters.</summary>
    private static DateTime? Day(GoldenCase c, string key, string fallback) =>
        DateTime.TryParseExact(c.Str(key, fallback), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    /// <summary>The weekdays a case disables, GPUI's Matcher::DayOfWeek numbers ("0,6": Sunday and Saturday).</summary>
    private static DaysOfWeek Weekdays(GoldenCase c) =>
        c.Str("disabled_weekdays").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Aggregate(DaysOfWeek.None, (days, n) => days | (DaysOfWeek)(1 << int.Parse(n, CultureInfo.InvariantCulture)));

    /// <summary>
    /// time.rs calendar: a CalendarView whose today the case pins (R7, as the
    /// reference pins GPUI's), the date or range, the view, the months and the
    /// disabled days.
    /// </summary>
    internal static CalendarView UikitCalendar(GoldenCase c)
    {
        var date = Day(c, "date", "2025-03-14");
        var today = Day(c, "today", "2025-06-10")!.Value;
        var calendar = new CalendarView
        {
            Today = today,
            IsRange = c.Bool("range"),
            NumberOfMonths = (int)c.Num("months", 1),
            FirstDayOfWeek = c.Str("first_day", "sun") == "mon" ? DayOfWeek.Monday : DayOfWeek.Sunday,
            DisabledDaysOfWeek = Weekdays(c),
            DisplayDate = date ?? Day(c, "month", "none") ?? today,
            Date = date,
            EndDate = Day(c, "end", "none"),
        };
        ClassFrom(calendar, c, "size", "medium");
        if (Day(c, "blackout_from", "none") is { } from && Day(c, "blackout_to", "none") is { } to)
        {
            calendar.DisabledDates.Add(new DateRange(from, to));
        }
        calendar.DisplayMode = c.Str("view", "day") switch
        {
            "month" => CalendarViewMode.Month,
            "year" => CalendarViewMode.Year,
            _ => CalendarViewMode.Day,
        };
        return calendar;
    }

    /// <summary>time.rs time_field: the time, precision, hour cycle, size, disabled and invalid.</summary>
    internal static TimeField UikitTimeField(GoldenCase c)
    {
        var field = new TimeField
        {
            Precision = c.Str("precision", "minute") == "second" ? TimePrecision.Second : TimePrecision.Minute,
            HourCycle = c.Str("cycle", "h23") == "h12" ? HourCycle.H12 : HourCycle.H23,
            Time = c.Str("time", "09:30:15") is "none" ? null : TimeSpan.Parse(c.Str("time", "09:30:15"), CultureInfo.InvariantCulture),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(field, c, "size", "medium");
        if (c.Bool("invalid"))
        {
            DataValidationErrors.SetError(field, new Exception("invalid"));
        }
        return field;
    }

    /// <summary>
    /// time.rs date_picker: a DateField 220 wide with GPUI's format and
    /// placeholder by default, and the range, presets, clear button, time and
    /// months the case names.
    /// </summary>
    internal static DateField UikitDateField(GoldenCase c)
    {
        var range = c.Bool("range");
        var field = new DateField
        {
            Today = Day(c, "today", "2025-06-10")!.Value,
            Width = c.Num("width", 220),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            IsRange = range,
            NumberOfMonths = (int)c.Num("months", 1),
            IsCleanable = c.Bool("cleanable"),
            DisabledDaysOfWeek = Weekdays(c),
            IsEnabled = !c.Bool("disabled"),
        };
        if (c.Has("placeholder"))
        {
            field.PlaceholderText = c.Str("placeholder");
        }
        if (c.Has("precision"))
        {
            field.TimePrecision = c.Str("precision") == "second" ? TimePrecision.Second : TimePrecision.Minute;
            field.HourCycle = c.Str("cycle", "h23") == "h12" ? HourCycle.H12 : HourCycle.H23;
        }
        var time = c.Has("time") ? TimeSpan.Parse(c.Str("time"), CultureInfo.InvariantCulture) : TimeSpan.Zero;
        field.Date = Day(c, "date", "none") + time;
        field.EndDate = Day(c, "end", "none");
        if (c.Bool("presets"))
        {
            foreach (var preset in Presets(range))
            {
                field.Presets.Add(preset);
            }
        }
        ClassFrom(field, c, "size", "medium");
        FlagClass(field, c, "plain");
        return field;
    }

    /// <summary>The presets time.rs gives a case: GPUI's story labels, on dates fixed before the case's today.</summary>
    private static IEnumerable<DateRangePreset> Presets(bool range) => range
        ?
        [
            new("Last 7 Days", new DateTime(2025, 3, 7), new DateTime(2025, 3, 14)),
            new("Last 14 Days", new DateTime(2025, 2, 28), new DateTime(2025, 3, 14)),
            new("Last 30 Days", new DateTime(2025, 2, 12), new DateTime(2025, 3, 14)),
        ]
        :
        [
            new("Yesterday", new DateTime(2025, 3, 13)),
            new("Last Week", new DateTime(2025, 3, 7)),
            new("Last Month", new DateTime(2025, 2, 12)),
        ];
}
