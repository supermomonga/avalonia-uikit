using Avalonia.Collections;

namespace AvaloniaUIKit;

/// <summary>
/// Days of the week as flags, for the disabled weekdays of a
/// <see cref="CalendarView"/> or a DateField (GPUI's
/// <c>Matcher::DayOfWeek</c>). XAML takes them comma-separated:
/// <c>DisabledDaysOfWeek="Sunday, Saturday"</c>.
/// </summary>
[Flags]
public enum DaysOfWeek
{
    /// <summary>No day.</summary>
    None = 0,
    /// <summary>Sunday.</summary>
    Sunday = 1 << DayOfWeek.Sunday,
    /// <summary>Monday.</summary>
    Monday = 1 << DayOfWeek.Monday,
    /// <summary>Tuesday.</summary>
    Tuesday = 1 << DayOfWeek.Tuesday,
    /// <summary>Wednesday.</summary>
    Wednesday = 1 << DayOfWeek.Wednesday,
    /// <summary>Thursday.</summary>
    Thursday = 1 << DayOfWeek.Thursday,
    /// <summary>Friday.</summary>
    Friday = 1 << DayOfWeek.Friday,
    /// <summary>Saturday.</summary>
    Saturday = 1 << DayOfWeek.Saturday,
    /// <summary>Saturday and Sunday.</summary>
    Weekend = Saturday | Sunday,
}

/// <summary>
/// The dates from <see cref="Start"/> to <see cref="End"/>, both included
/// (GPUI's <c>Matcher::Range</c>). A missing end has no bound, so a range
/// with only an end disables every date up to it and one with only a start
/// every date from it: two such ranges are GPUI's <c>Matcher::Interval</c>.
/// </summary>
public sealed class DateRange
{
    /// <summary>Creates an empty (unbounded) range; set <see cref="Start"/> and <see cref="End"/>.</summary>
    public DateRange()
    {
    }

    /// <summary>Creates the range from <paramref name="start"/> to <paramref name="end"/>.</summary>
    public DateRange(DateTime? start, DateTime? end)
    {
        Start = start;
        End = end;
    }

    /// <summary>The first date of the range, or null for no lower bound.</summary>
    public DateTime? Start { get; set; }

    /// <summary>The last date of the range, or null for no upper bound.</summary>
    public DateTime? End { get; set; }

    /// <summary>Whether <paramref name="date"/> (its date part) lies in the range.</summary>
    public bool Contains(DateTime date)
    {
        // calendar.rs Matcher::Range: !(date < from) && !(date > to).
        var day = date.Date;
        return !(Start is { } from && day < from.Date) && !(End is { } to && day > to.Date);
    }
}

/// <summary>The disabled dates of a calendar: GPUI's disabled_matcher, as the three forms the controls accept.</summary>
internal readonly record struct DateMatcher(DaysOfWeek Days, IReadOnlyList<DateRange>? Ranges, Func<DateTime, bool>? Custom)
{
    public bool Matches(DateTime date)
    {
        if (Days != DaysOfWeek.None && Days.HasFlag((DaysOfWeek)(1 << (int)date.DayOfWeek)))
        {
            return true;
        }
        if (Ranges is not null)
        {
            foreach (var range in Ranges)
            {
                if (range.Contains(date))
                {
                    return true;
                }
            }
        }
        return Custom?.Invoke(date.Date) == true;
    }

    public static DateMatcher Of(DaysOfWeek days, AvaloniaList<DateRange> ranges, Func<DateTime, bool>? custom) =>
        new(days, ranges.Count > 0 ? ranges : null, custom);
}
