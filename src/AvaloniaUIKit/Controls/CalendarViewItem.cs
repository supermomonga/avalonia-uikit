using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;

namespace AvaloniaUIKit;

/// <summary>What a <see cref="CalendarViewItem"/> stands for (GPUI's CalendarItemKind).</summary>
public enum CalendarViewItemKind
{
    /// <summary>The previous month (or page of years).</summary>
    Previous,
    /// <summary>The header's month, which opens the months grid.</summary>
    MonthToggle,
    /// <summary>The header's year, which opens the years grid.</summary>
    YearToggle,
    /// <summary>The next month (or page of years).</summary>
    Next,
    /// <summary>A weekday title.</summary>
    Weekday,
    /// <summary>A day.</summary>
    Day,
    /// <summary>A month of the months grid.</summary>
    Month,
    /// <summary>A year of the years grid.</summary>
    Year,
}

/// <summary>
/// One cell of a <see cref="CalendarView"/>, built by the calendar (GPUI's
/// CalendarItem). The kind is also a style class (<c>previous</c>,
/// <c>month-toggle</c>, <c>year-toggle</c>, <c>next</c>, <c>weekday</c>,
/// <c>day</c>, <c>month</c>, <c>year</c>), and the calendar's size class is
/// copied onto it. Pseudo-classes: :active (selected, or the open grid's
/// toggle), :in-range (between a range's ends), :muted (outside the month,
/// disabled, or a weekday title) and :today. A disabled day, the weekday titles
/// and a previous or next button that cannot move are disabled.
/// </summary>
[PseudoClasses(":active", ":in-range", ":muted", ":today")]
public class CalendarViewItem : Button
{
    /// <summary>The kind of cell.</summary>
    public static readonly DirectProperty<CalendarViewItem, CalendarViewItemKind> KindProperty =
        AvaloniaProperty.RegisterDirect<CalendarViewItem, CalendarViewItemKind>(nameof(Kind), i => i.Kind);

    private CalendarViewItemKind _kind;

    /// <inheritdoc cref="KindProperty"/>
    public CalendarViewItemKind Kind
    {
        get => _kind;
        internal set
        {
            if (_kind != value || !Classes.Contains(ClassOf(value)))
            {
                Classes.Remove(ClassOf(_kind));
                SetAndRaise(KindProperty, ref _kind, value);
                Classes.Add(ClassOf(value));
            }
        }
    }

    /// <summary>The day of a day cell.</summary>
    internal DateTime Date { get; set; }

    /// <summary>The month or year of a month or year cell.</summary>
    internal int Number { get; set; }

    internal void SetState(bool active = false, bool inRange = false, bool muted = false, bool today = false, bool disabled = false)
    {
        PseudoClasses.Set(":active", active);
        PseudoClasses.Set(":in-range", inRange);
        PseudoClasses.Set(":muted", muted);
        PseudoClasses.Set(":today", today);
        IsEnabled = !disabled;
    }

    private static string ClassOf(CalendarViewItemKind kind) => kind switch
    {
        CalendarViewItemKind.Previous => "previous",
        CalendarViewItemKind.MonthToggle => "month-toggle",
        CalendarViewItemKind.YearToggle => "year-toggle",
        CalendarViewItemKind.Next => "next",
        CalendarViewItemKind.Weekday => "weekday",
        CalendarViewItemKind.Day => "day",
        CalendarViewItemKind.Month => "month",
        _ => "year",
    };
}
