using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;
using Editor = AvaloniaUIKit.TimeField.SegmentEditor;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What CalendarView, TimeField and DateField do beyond their look: the
/// rules of GPUI Kit's calendar.rs, time_field.rs and date_picker.rs, most of
/// them GPUI's own unit tests played through the controls.
/// </summary>
public class DatesBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    private static DateTime D(int year, int month, int day) => new(year, month, day);

    private static TimeSpan T(int h, int m, int s = 0) => new(h, m, s);

    // ---- CalendarView (calendar.rs) ----

    /// <summary>A March 2025 calendar (medium): day (row r, column c) at (45 + 32c, 111 + 32r).</summary>
    private static (CaseHost Host, CalendarView Calendar, List<CalendarSelectedEventArgs> Events) March(bool range = false, DateTime? date = null)
    {
        var golden = Case("uikit-calendar/day.medium/normal/light");
        var calendar = new CalendarView { Today = D(2025, 6, 10), IsRange = range, DisplayDate = D(2025, 3, 1), Date = date };
        var host = CaseHost.Open(golden, calendar);
        var events = new List<CalendarSelectedEventArgs>();
        calendar.Selected += (_, e) => events.Add(e);
        return (host, calendar, events);
    }

    private static string Day(int row, int column) => $"click-at-{45 + 32 * column}-{111 + 32 * row}";

    // range_selection_restarts_and_completes, activation_emits_only_for_complete_enabled_values.
    [Test]
    public async Task A_range_completes_on_its_second_day_and_restarts_after()
    {
        var (host, calendar, events) = March(range: true);
        using var _ = host;
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(1, 2)); // Mar 4
        await Assert.That(calendar.Date).IsEqualTo(D(2025, 3, 4));
        await Assert.That(calendar.EndDate).IsNull();
        await Assert.That(events).IsEmpty();
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(2, 3)); // Mar 12
        await Assert.That(calendar.EndDate).IsEqualTo(D(2025, 3, 12));
        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].Date).IsEqualTo(D(2025, 3, 4));
        await Assert.That(events[0].EndDate).IsEqualTo(D(2025, 3, 12));
        // A click on a complete range starts a new one.
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(1, 2));
        await Assert.That(calendar.Date).IsEqualTo(D(2025, 3, 4));
        await Assert.That(calendar.EndDate).IsNull();
        await Assert.That(events.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_day_before_the_start_restarts_the_range()
    {
        var (host, calendar, events) = March(range: true);
        using var _ = host;
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(2, 3)); // Mar 12
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(1, 2)); // Mar 4
        await Assert.That(calendar.Date).IsEqualTo(D(2025, 3, 4));
        await Assert.That(calendar.EndDate).IsNull();
        await Assert.That(events).IsEmpty();
        // The same day ends a one-day range.
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(1, 2));
        await Assert.That(calendar.EndDate).IsEqualTo(D(2025, 3, 4));
        await Assert.That(events.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_single_date_is_selected_and_reported_at_once()
    {
        var (host, calendar, events) = March();
        using var _ = host;
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(1, 2));
        await Assert.That(calendar.Date).IsEqualTo(D(2025, 3, 4));
        await Assert.That(events.Count).IsEqualTo(1);
        await Assert.That(events[0].EndDate).IsNull();
    }

    // disabled_date_is_rejected: each of the three matchers.
    [Test]
    [Arguments("range")]
    [Arguments("weekday")]
    [Arguments("custom")]
    public async Task A_disabled_day_does_not_respond(string matcher)
    {
        var (host, calendar, events) = March(date: D(2025, 3, 14));
        using var _ = host;
        switch (matcher)
        {
            case "range":
                calendar.DisabledDates.Add(new DateRange(D(2025, 3, 1), D(2025, 3, 8)));
                break;
            case "weekday":
                calendar.DisabledDaysOfWeek = DaysOfWeek.Tuesday;
                break;
            default:
                calendar.DisabledMatcher = d => d.Day < 5;
                break;
        }
        await Assert.That(calendar.IsDateDisabled(D(2025, 3, 4))).IsTrue();
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(1, 2)); // Mar 4 (a Tuesday)
        await Assert.That(calendar.Date).IsEqualTo(D(2025, 3, 14));
        await Assert.That(events).IsEmpty();
    }

    [Test]
    public async Task Open_ended_ranges_disable_before_and_after()
    {
        var calendar = new CalendarView { DisabledDates = { new DateRange(null, D(2025, 3, 9)), new DateRange(D(2025, 3, 21), null) } };
        await Assert.That(calendar.IsDateDisabled(D(1990, 1, 1))).IsTrue();
        await Assert.That(calendar.IsDateDisabled(D(2025, 3, 9))).IsTrue();
        await Assert.That(calendar.IsDateDisabled(D(2025, 3, 10))).IsFalse();
        await Assert.That(calendar.IsDateDisabled(D(2025, 3, 20))).IsFalse();
        await Assert.That(calendar.IsDateDisabled(D(2025, 3, 21))).IsTrue();
    }

    // month_navigation_crosses_year.
    [Test]
    public async Task The_arrows_cross_the_year()
    {
        var (host, calendar, _) = March(date: D(2025, 1, 1));
        using var _h = host;
        var golden = Case("uikit-calendar/day.medium/normal/light");
        host.Drive(golden, "click-at-45-45");
        await Assert.That((calendar.DisplayDate.Year, calendar.DisplayDate.Month)).IsEqualTo((2024, 12));
        host.Drive(golden, "click-at-235-45");
        await Assert.That((calendar.DisplayDate.Year, calendar.DisplayDate.Month)).IsEqualTo((2025, 1));
    }

    // apply_date: the displayed month follows the value, so a day of the next month turns the page.
    [Test]
    public async Task A_day_outside_the_month_turns_to_its_month()
    {
        var (host, calendar, _) = March();
        using var _h = host;
        host.Drive(Case("uikit-calendar/day.medium/normal/light"), Day(5, 3)); // Apr 2
        await Assert.That(calendar.Date).IsEqualTo(D(2025, 4, 2));
        await Assert.That(calendar.DisplayDate.Month).IsEqualTo(4);
    }

    // day_month_and_year_views_have_complete_transitions.
    [Test]
    public async Task The_toggles_open_their_grids_and_a_pick_returns_to_the_days()
    {
        var (host, calendar, _) = March(date: D(2025, 3, 14));
        using var _h = host;
        var golden = Case("uikit-calendar/day.medium/normal/light");
        host.Drive(golden, "click-at-106-45");
        await Assert.That(calendar.DisplayMode).IsEqualTo(CalendarViewMode.Month);
        host.Drive(golden, "click-at-106-45");
        await Assert.That(calendar.DisplayMode).IsEqualTo(CalendarViewMode.Day);
        host.Drive(golden, "click-at-106-45+click-at-140-203"); // November
        await Assert.That((calendar.DisplayMode, calendar.DisplayDate.Month)).IsEqualTo((CalendarViewMode.Day, 11));
        host.Drive(golden, "click-at-177-45");
        await Assert.That(calendar.DisplayMode).IsEqualTo(CalendarViewMode.Year);
        host.Drive(golden, "click-at-140-203"); // 2032
        await Assert.That((calendar.DisplayMode, calendar.DisplayDate.Year)).IsEqualTo((CalendarViewMode.Day, 2032));
        // Picking a month or year moves the view, not the value.
        await Assert.That(calendar.Date).IsEqualTo(D(2025, 3, 14));
    }

    // year_page_navigation_respects_both_bounds: fifty years either side of today.
    [Test]
    public async Task The_year_pages_stop_at_both_ends()
    {
        var (host, calendar, _) = March(date: D(2025, 3, 14));
        using var _h = host;
        var golden = Case("uikit-calendar/day.medium/normal/light");
        calendar.DisplayMode = CalendarViewMode.Year;
        CalendarViewItem Arrow(CalendarViewItemKind kind) =>
            calendar.GetVisualDescendants().OfType<CalendarViewItem>().Single(i => i.Kind == kind);
        int[] Years() => [.. calendar.GetVisualDescendants().OfType<CalendarViewItem>().Where(i => i.Kind == CalendarViewItemKind.Year).Select(i => int.Parse((string)i.Content!))];
        host.Flush();
        await Assert.That(Years().First()).IsEqualTo(2015);
        for (var i = 0; i < 4; i++)
        {
            host.Drive(golden, "click-at-45-45");
        }
        await Assert.That(Years().First()).IsEqualTo(1975);
        await Assert.That(Arrow(CalendarViewItemKind.Previous).IsEnabled).IsFalse();
        for (var i = 0; i < 6; i++)
        {
            host.Drive(golden, "click-at-235-45");
        }
        await Assert.That(Years().Last()).IsEqualTo(2074);
        await Assert.That(Arrow(CalendarViewItemKind.Next).IsEnabled).IsFalse();
    }

    // six_week_month_is_not_truncated, and the weeks a month needs (utils.rs test_days).
    [Test]
    [Arguments(2025, 8, DayOfWeek.Sunday, 6, "2025-07-27")]
    [Arguments(2024, 8, DayOfWeek.Sunday, 5, "2024-07-28")]
    [Arguments(2026, 2, DayOfWeek.Sunday, 4, "2026-02-01")]
    [Arguments(2023, 2, DayOfWeek.Monday, 5, "2023-01-30")]
    [Arguments(2025, 1, DayOfWeek.Sunday, 5, "2024-12-29")]
    public async Task A_month_shows_the_weeks_it_needs(int year, int month, DayOfWeek first, int weeks, string start)
    {
        var grid = CalendarView.WeeksOf(year, month, first);
        await Assert.That(grid.Count).IsEqualTo(weeks);
        await Assert.That(grid[0][0].ToString("yyyy-MM-dd")).IsEqualTo(start);
        await Assert.That(grid[^1][6] >= new DateTime(year, month, 1).AddMonths(1).AddDays(-1)).IsTrue();
    }

    // ---- TimeField (time_field.rs) ----

    private static Editor Editor(TimePrecision precision, HourCycle cycle)
    {
        var editor = new Editor();
        editor.SetFormat(precision, cycle);
        return editor;
    }

    private static void Type(ref Editor editor, string keys)
    {
        foreach (var ch in keys)
        {
            _ = ch switch
            {
                'a' => editor.InputPeriod(false),
                'p' => editor.InputPeriod(true),
                _ => editor.InputDigit(ch - '0'),
            };
        }
    }

    [Test]
    public async Task Typing_fills_segments_and_advances()
    {
        var editor = Editor(TimePrecision.Second, HourCycle.H23);
        Type(ref editor, "093015");
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(9, 30, 15));
        // The last segment stays selected once it is complete.
        await Assert.That(editor.Segment).IsEqualTo(TimeSegment.Second);
    }

    [Test]
    public async Task A_digit_that_cannot_start_two_digits_completes_the_segment()
    {
        var editor = Editor(TimePrecision.Minute, HourCycle.H23);
        Type(ref editor, "7");
        await Assert.That((editor.Time, editor.Segment)).IsEqualTo((new TimeOnly(7, 0), TimeSegment.Minute));
        Type(ref editor, "8");
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(7, 8));
    }

    [Test]
    public async Task An_out_of_range_pair_restarts_from_the_second_digit()
    {
        var editor = Editor(TimePrecision.Minute, HourCycle.H23);
        Type(ref editor, "25");
        await Assert.That((editor.Time, editor.Segment)).IsEqualTo((new TimeOnly(5, 0), TimeSegment.Minute));
    }

    [Test]
    public async Task Stepping_wraps_without_carry()
    {
        var editor = Editor(TimePrecision.Minute, HourCycle.H23);
        editor.SetTime(new TimeOnly(23, 59));
        editor.Step(1);
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(0, 59));
        editor.SelectSegment(TimeSegment.Minute);
        editor.Step(1);
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(0, 0));
        editor.Step(-1);
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(0, 59));
    }

    [Test]
    public async Task Segment_movement_stops_at_the_ends()
    {
        var editor = Editor(TimePrecision.Minute, HourCycle.H23);
        await Assert.That(editor.MoveSegment(-1)).IsFalse();
        await Assert.That(editor.MoveSegment(1)).IsTrue();
        await Assert.That(editor.MoveSegment(1)).IsFalse();
        await Assert.That(editor.SelectSegment(TimeSegment.Second)).IsFalse();
        await Assert.That(editor.SelectSegment(TimeSegment.Period)).IsFalse();
    }

    [Test]
    public async Task Precision_truncates_seconds()
    {
        var editor = Editor(TimePrecision.Second, HourCycle.H23);
        editor.SetTime(new TimeOnly(9, 30, 15));
        editor.SelectSegment(TimeSegment.Second);
        editor.SetFormat(TimePrecision.Minute, HourCycle.H23);
        await Assert.That((editor.Time, editor.Segment)).IsEqualTo((new TimeOnly(9, 30), TimeSegment.Hour));
    }

    [Test]
    public async Task Clearing_resets_only_the_selected_segment()
    {
        var editor = Editor(TimePrecision.Minute, HourCycle.H23);
        editor.SetTime(new TimeOnly(9, 30));
        editor.SelectSegment(TimeSegment.Minute);
        await Assert.That(editor.ClearSegment()).IsTrue();
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(9, 0));
    }

    [Test]
    [Arguments(0, "12", "AM")]
    [Arguments(9, "09", "AM")]
    [Arguments(12, "12", "PM")]
    [Arguments(23, "11", "PM")]
    public async Task Twelve_hour_labels_map_midnight_and_noon_to_twelve(int hour, string label, string period)
    {
        var editor = Editor(TimePrecision.Minute, HourCycle.H12);
        editor.SetTime(new TimeOnly(hour, 0));
        await Assert.That(editor.Label(TimeSegment.Hour)).IsEqualTo(label);
        await Assert.That(editor.Label(TimeSegment.Period)).IsEqualTo(period);
    }

    [Test]
    public async Task Twelve_hour_typing_keeps_the_period_until_it_is_typed()
    {
        var editor = Editor(TimePrecision.Minute, HourCycle.H12);
        // "1" may start 10-12, so it waits; "2" makes 12, which is midnight in AM.
        Type(ref editor, "12");
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(0, 0));
        Type(ref editor, "30p");
        await Assert.That((editor.Time, editor.Segment)).IsEqualTo((new TimeOnly(12, 30), TimeSegment.Period));
        editor.SelectSegment(TimeSegment.Hour);
        Type(ref editor, "9");
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(21, 30));
        // "00" is not an hour on a 12-hour clock.
        editor.SelectSegment(TimeSegment.Hour);
        Type(ref editor, "00");
        await Assert.That(editor.Segment).IsEqualTo(TimeSegment.Hour);
    }

    [Test]
    public async Task Twelve_hour_stepping_wraps_within_the_period()
    {
        var editor = Editor(TimePrecision.Minute, HourCycle.H12);
        editor.SetTime(new TimeOnly(11, 0));
        editor.Step(1);
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(0, 0));
        editor.SelectSegment(TimeSegment.Period);
        editor.Step(1);
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(12, 0));
        editor.Step(1);
        await Assert.That(editor.Time).IsEqualTo(new TimeOnly(0, 0));
        await Assert.That(editor.InputDigit(1)).IsFalse();
    }

    /// <summary>A medium 09:30 field and a button after it, for Tab to leave to.</summary>
    private static (CaseHost Host, TimeField Field, Button After, List<TimeSpan> Changes) Field(TimeSpan? time = null, TimePrecision precision = TimePrecision.Minute)
    {
        var golden = Case("uikit-timefield/hover.base/hover/light");
        var field = new TimeField { Time = time, Precision = precision };
        var after = new Button { Content = "After" };
        var host = CaseHost.Open(golden, new StackPanel { Spacing = 8, Children = { field, after } });
        var changes = new List<TimeSpan>();
        field.Changed += (_, e) => changes.Add(e.Time);
        return (host, field, after, changes);
    }

    [Test]
    public async Task Keys_edit_the_field_and_report_only_edits()
    {
        var (host, field, _, changes) = Field(T(9, 30));
        using var _h = host;
        field.Time = T(10, 15);
        await Assert.That(changes).IsEmpty();
        host.Drive(Case("uikit-timefield/hover.base/hover/light"), "focus+key-up+key-right+key-4+key-5");
        await Assert.That(field.Time).IsEqualTo(T(11, 45));
        await Assert.That(field.SelectedSegment).IsEqualTo(TimeSegment.Minute);
        await Assert.That(changes.Select(t => t.ToString(@"hh\:mm"))).IsEquivalentTo(["11:15", "11:04", "11:45"]);
    }

    [Test]
    public async Task Tab_walks_the_segments_then_leaves_the_field()
    {
        var (host, field, after, _) = Field(T(9, 30));
        using var _h = host;
        var golden = Case("uikit-timefield/hover.base/hover/light");
        host.Drive(golden, "focus");
        await Assert.That(field.IsFocused).IsTrue();
        host.Drive(golden, "key-tab");
        await Assert.That((field.IsFocused, field.SelectedSegment)).IsEqualTo((true, TimeSegment.Minute));
        host.Drive(golden, "key-tab");
        await Assert.That(after.IsFocused).IsTrue();
    }

    [Test]
    public async Task A_click_focuses_the_field_and_selects_the_segment()
    {
        var (host, field, _, _) = Field(T(9, 30));
        using var _h = host;
        var minute = field.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "PART_Minute");
        var center = minute.TranslatePoint(new Avalonia.Point(minute.Bounds.Width / 2, minute.Bounds.Height / 2), host.Window)!.Value;
        host.Drive(Case("uikit-timefield/hover.base/hover/light"), $"click-at-{(int)center.X}-{(int)center.Y}");
        await Assert.That((field.IsFocused, field.SelectedSegment)).IsEqualTo((true, TimeSegment.Minute));
    }

    [Test]
    public async Task An_empty_field_shows_dashes_until_a_digit_is_typed()
    {
        var (host, field, _, changes) = Field();
        using var _h = host;
        string[] Labels() => [.. field.GetVisualDescendants().OfType<TextBlock>().Where(t => t.Name is "PART_HourText" or "PART_MinuteText").Select(t => t.Text!)];
        await Assert.That(Labels()).IsEquivalentTo(["--", "--"]);
        // Backspace resets a segment; it does not invent a time.
        host.Drive(Case("uikit-timefield/hover.base/hover/light"), "focus+key-backspace");
        await Assert.That(field.Time).IsNull();
        host.Drive(Case("uikit-timefield/hover.base/hover/light"), "key-0");
        await Assert.That(field.Time).IsEqualTo(T(0, 0));
        await Assert.That(changes.Count).IsEqualTo(1);
        await Assert.That(Labels()).IsEquivalentTo(["00", "00"]);
    }

    [Test]
    public async Task A_disabled_field_ignores_keys()
    {
        var (host, field, _, changes) = Field(T(9, 30));
        using var _h = host;
        field.IsEnabled = false;
        field.Focus();
        host.Drive(Case("uikit-timefield/hover.base/hover/light"), "key-up+key-1");
        await Assert.That(field.Time).IsEqualTo(T(9, 30));
        await Assert.That(changes).IsEmpty();
    }

    // ---- DateField (date_picker.rs) ----

    /// <summary>
    /// A 220px field at (16, 8) whose popup calendar (medium) puts day (row r,
    /// column c) at (44 + 32c, 140 + 32r).
    /// </summary>
    private static (CaseHost Host, DateField Field, List<DateFieldChangedEventArgs> Changes) Picker(Action<DateField>? configure = null, string id = "uikit-datepicker/select.base/click+wait-200ms+click-at-172-236/light")
    {
        var golden = Case(id);
        var field = Adapters.UikitDateField(golden);
        configure?.Invoke(field);
        var host = CaseHost.Open(golden, field);
        var changes = new List<DateFieldChangedEventArgs>();
        field.Changed += (_, e) => changes.Add(e);
        return (host, field, changes);
    }

    private static string PopupDay(int row, int column) => $"click-at-{44 + 32 * column}-{140 + 32 * row}";

    private const string Open = "click+wait-200ms";

    [Test]
    public async Task Picking_a_day_closes_the_popup_and_reports_it()
    {
        var (host, field, changes) = Picker();
        using var _h = host;
        var golden = Case("uikit-datepicker/select.base/click+wait-200ms+click-at-172-236/light");
        host.Drive(golden, Open);
        await Assert.That(field.IsDropDownOpen).IsTrue();
        host.Drive(golden, PopupDay(3, 4)); // Mar 20
        await Assert.That(field.IsDropDownOpen).IsFalse();
        await Assert.That(field.Date).IsEqualTo(D(2025, 3, 20));
        await Assert.That(field.Text).IsEqualTo("2025/03/20");
        await Assert.That(field.IsFocused).IsTrue();
        await Assert.That(changes.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_range_changes_the_field_once_both_days_are_picked()
    {
        var (host, field, changes) = Picker(id: "uikit-datepicker/range-select.base/click+wait-200ms+click-at-108-172/light");
        using var _h = host;
        var golden = Case("uikit-datepicker/range-select.base/click+wait-200ms+click-at-108-172/light");
        host.Drive(golden, Open + "+" + PopupDay(1, 2)); // Mar 4
        await Assert.That(field.IsDropDownOpen).IsTrue();
        await Assert.That(field.Date).IsNull();
        await Assert.That(changes).IsEmpty();
        host.Drive(golden, PopupDay(2, 3)); // Mar 12
        await Assert.That(field.IsDropDownOpen).IsFalse();
        await Assert.That((field.Date, field.EndDate)).IsEqualTo(((DateTime?)D(2025, 3, 4), (DateTime?)D(2025, 3, 12)));
        await Assert.That(field.Text).IsEqualTo("2025/03/04 - 2025/03/12");
        await Assert.That(changes.Count).IsEqualTo(1);
    }

    [Test]
    public async Task With_the_time_a_pick_keeps_the_popup_open_and_a_second_pick_closes_it()
    {
        var (host, field, changes) = Picker(id: "uikit-datepicker/time-select.base/click+wait-200ms+click-at-172-236/light");
        using var _h = host;
        var golden = Case("uikit-datepicker/time-select.base/click+wait-200ms+click-at-172-236/light");
        await Assert.That(field.Text).IsEqualTo("2025/03/14 09:30");
        host.Drive(golden, Open + "+" + PopupDay(3, 4));
        await Assert.That(field.IsDropDownOpen).IsTrue();
        await Assert.That(field.Date).IsEqualTo(D(2025, 3, 20) + T(9, 30));
        await Assert.That(changes.Count).IsEqualTo(1);
        // Editing the time applies at once.
        // The popup lives in the window's overlay layer, outside the field's visual tree.
        var time = host.Part<TimeField>("PART_TimeField");
        time.Focus();
        host.PressKey("up");
        await Assert.That(field.Date).IsEqualTo(D(2025, 3, 20) + T(10, 30));
        await Assert.That(changes.Count).IsEqualTo(2);
        await Assert.That(changes[^1].Date).IsEqualTo(D(2025, 3, 20) + T(10, 30));
        host.Drive(golden, PopupDay(3, 4));
        await Assert.That(field.IsDropDownOpen).IsFalse();
        await Assert.That(changes.Count).IsEqualTo(2);
    }

    [Test]
    public async Task The_clear_button_clears_without_taking_the_focus()
    {
        var (host, field, changes) = Picker(id: "uikit-datepicker/clear.base/click-at-219-24/light");
        using var _h = host;
        host.Drive(Case("uikit-datepicker/clear.base/click-at-219-24/light"), "click-at-219-24");
        await Assert.That(field.Date).IsNull();
        await Assert.That(field.Text).IsNull();
        await Assert.That(field.IsKeyboardFocusWithin).IsFalse();
        await Assert.That(field.IsDropDownOpen).IsFalse();
        await Assert.That(changes.Count).IsEqualTo(1);
        await Assert.That(changes[0].Date).IsNull();
    }

    [Test]
    [Arguments("backspace")]
    [Arguments("delete")]
    public async Task Delete_and_backspace_clear_the_focused_field(string key)
    {
        var (host, field, changes) = Picker();
        using var _h = host;
        host.Drive(Case("uikit-datepicker/select.base/click+wait-200ms+click-at-172-236/light"), "focus+key-" + key);
        await Assert.That(field.Date).IsNull();
        await Assert.That(changes.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Enter_toggles_the_popup_and_escape_closes_it()
    {
        var (host, field, _) = Picker();
        using var _h = host;
        var golden = Case("uikit-datepicker/select.base/click+wait-200ms+click-at-172-236/light");
        host.Drive(golden, "focus+key-enter");
        await Assert.That(field.IsDropDownOpen).IsTrue();
        host.Drive(golden, "key-enter");
        await Assert.That(field.IsDropDownOpen).IsFalse();
        host.Drive(golden, "key-enter+key-escape");
        await Assert.That(field.IsDropDownOpen).IsFalse();
        await Assert.That(field.IsFocused).IsTrue();
    }

    [Test]
    public async Task A_preset_picks_its_dates_and_closes()
    {
        var (host, field, changes) = Picker(id: "uikit-datepicker/range-presets.base/click+wait-200ms/light");
        using var _h = host;
        host.Drive(Case("uikit-datepicker/range-presets.base/click+wait-200ms/light"), Open);
        var preset = host.Window.GetVisualDescendants().OfType<Button>().Single(b => b.Content as string == "Last 14 Days");
        var center = preset.TranslatePoint(new Avalonia.Point(preset.Bounds.Width / 2, preset.Bounds.Height / 2), host.Window)!.Value;
        host.Drive(Case("uikit-datepicker/range-presets.base/click+wait-200ms/light"), $"click-at-{(int)center.X}-{(int)center.Y}");
        await Assert.That((field.Date, field.EndDate)).IsEqualTo(((DateTime?)D(2025, 2, 28), (DateTime?)D(2025, 3, 14)));
        await Assert.That(field.IsDropDownOpen).IsFalse();
        await Assert.That(changes.Count).IsEqualTo(1);
    }

    [Test]
    public async Task A_disabled_field_does_not_open()
    {
        var (host, field, _) = Picker(f => f.IsEnabled = false);
        using var _h = host;
        host.Drive(Case("uikit-datepicker/select.base/click+wait-200ms+click-at-172-236/light"), Open);
        await Assert.That(field.IsDropDownOpen).IsFalse();
    }

    [Test]
    public async Task An_invalid_field_has_the_danger_border()
    {
        var (host, field, _) = Picker();
        using var _h = host;
        DataValidationErrors.SetError(field, new Exception("invalid"));
        host.Flush();
        var border = (Avalonia.Media.ISolidColorBrush)host.Part<Border>("PART_Frame").BorderBrush!;
        var danger = (Avalonia.Media.ISolidColorBrush)host.Window.FindResource("UIKit.Danger")!;
        await Assert.That(border.Color).IsEqualTo(danger.Color);
    }

    // date_picker.rs display_format: "%Y/%m/%d", with the edited time in its precision and clock.
    [Test]
    [Arguments(null, HourCycle.H23, null, "2025/03/14")]
    [Arguments(TimePrecision.Minute, HourCycle.H23, null, "2025/03/14 21:05")]
    [Arguments(TimePrecision.Second, HourCycle.H23, null, "2025/03/14 21:05:09")]
    [Arguments(TimePrecision.Minute, HourCycle.H12, null, "2025/03/14 09:05 PM")]
    [Arguments(TimePrecision.Second, HourCycle.H12, null, "2025/03/14 09:05:09 PM")]
    [Arguments(null, HourCycle.H23, "yyyy-MM-dd", "2025-03-14")]
    public async Task The_text_follows_the_format(TimePrecision? precision, HourCycle cycle, string? format, string text)
    {
        var field = new DateField { TimePrecision = precision, HourCycle = cycle, DateFormat = format, Date = D(2025, 3, 14) + T(21, 5, 9) };
        await Assert.That(field.Text).IsEqualTo(text);
    }
}
