using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>What a <see cref="CalendarView"/> shows (GPUI's CalendarView).</summary>
public enum CalendarViewMode
{
    /// <summary>The days of the displayed months.</summary>
    Day,
    /// <summary>The twelve months, in three columns.</summary>
    Month,
    /// <summary>A page of twenty years, in five columns.</summary>
    Year,
}

/// <summary>A completed selection of a <see cref="CalendarView"/> (GPUI's CalendarEvent::Selected).</summary>
public class CalendarSelectedEventArgs(RoutedEvent routedEvent, DateTime? date, DateTime? endDate) : RoutedEventArgs(routedEvent)
{
    /// <summary>The selected date, or the range's first day.</summary>
    public DateTime? Date { get; } = date;

    /// <summary>The range's last day (null for a single date).</summary>
    public DateTime? EndDate { get; } = endDate;
}

/// <summary>
/// GPUI Kit's Calendar (crates/component/src/time/calendar.rs over
/// crates/base/src/calendar.rs), renamed as Avalonia has a Calendar. A card
/// with a header (previous, the month and the year toggles, next) and the
/// days of <see cref="NumberOfMonths"/> months side by side, each with only
/// the weeks it needs. The month toggle opens a grid of the twelve months, the
/// year toggle a page of twenty years; picking one returns to the days.
/// With several months the header names each month instead of the toggles.
/// <para>
/// A click on a day selects it (<see cref="Date"/>). With
/// <see cref="IsRange"/>, the first click starts a range and the second ends
/// it (<see cref="EndDate"/>); a day before the start, or any click on a
/// complete range, starts over. <see cref="Selected"/> fires only once the
/// value is complete. Disabled days (<see cref="DisabledDaysOfWeek"/>,
/// <see cref="DisabledDates"/>, <see cref="DisabledMatcher"/>) are muted and
/// do not respond. Size classes: small (28px cells, 220px per month), the
/// default (32, 248), large (40, 304).
/// </para>
/// </summary>
[TemplatePart("PART_Header", typeof(Decorator))]
[TemplatePart("PART_Body", typeof(Decorator))]
public class CalendarView : TemplatedControl
{
    /// <summary>The selected date, or the first day of a range.</summary>
    public static readonly StyledProperty<DateTime?> DateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime?>(nameof(Date), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The last day of a range (unused for a single date).</summary>
    public static readonly StyledProperty<DateTime?> EndDateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime?>(nameof(EndDate), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Whether clicks select a range of days (GPUI's Date::Range) instead of one day.</summary>
    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<CalendarView, bool>(nameof(IsRange));

    /// <summary>A date in the first displayed month (today's month at first).</summary>
    public static readonly StyledProperty<DateTime> DisplayDateProperty =
        AvaloniaProperty.Register<CalendarView, DateTime>(nameof(DisplayDate), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The days, the months grid or the years grid.</summary>
    public static readonly StyledProperty<CalendarViewMode> DisplayModeProperty =
        AvaloniaProperty.Register<CalendarView, CalendarViewMode>(nameof(DisplayMode), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The number of months shown side by side (GPUI's number_of_months, 1 by default).</summary>
    public static readonly StyledProperty<int> NumberOfMonthsProperty =
        AvaloniaProperty.Register<CalendarView, int>(nameof(NumberOfMonths), 1, coerce: (_, n) => Math.Max(1, n));

    /// <summary>The day each week starts on (Sunday by default, as GPUI).</summary>
    public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
        AvaloniaProperty.Register<CalendarView, DayOfWeek>(nameof(FirstDayOfWeek));

    /// <summary>Weekdays that cannot be selected (GPUI's Matcher::DayOfWeek).</summary>
    public static readonly StyledProperty<DaysOfWeek> DisabledDaysOfWeekProperty =
        AvaloniaProperty.Register<CalendarView, DaysOfWeek>(nameof(DisabledDaysOfWeek));

    /// <summary>A predicate for days that cannot be selected (GPUI's Matcher::custom); it gets dates at midnight.</summary>
    public static readonly StyledProperty<Func<DateTime, bool>?> DisabledMatcherProperty =
        AvaloniaProperty.Register<CalendarView, Func<DateTime, bool>?>(nameof(DisabledMatcher));

    /// <summary>The width of the card per month, set by the size classes (NaN: the content's width).</summary>
    public static readonly StyledProperty<double> MonthWidthProperty =
        AvaloniaProperty.Register<CalendarView, double>(nameof(MonthWidth), double.NaN);

    /// <summary>Raised when a click completes the value: a day, or a range's second day.</summary>
    public static readonly RoutedEvent<CalendarSelectedEventArgs> SelectedEvent =
        RoutedEvent.Register<CalendarView, CalendarSelectedEventArgs>(nameof(Selected), RoutingStrategies.Bubble);

    private static readonly string[] SizeClasses = ["xsmall", "small", "large"];

    // calendar.rs year_range: fifty years either side of today, in pages of twenty.
    private const int YearsAround = 50;
    private const int YearsPerPage = 20;

    private readonly CalendarRow _headerRow = new() { Justify = CalendarJustify.Between };
    private readonly CalendarRow _months = new() { Justify = CalendarJustify.Around };
    private readonly CalendarPickerGrid _monthGrid = new() { Columns = 3 };
    private readonly CalendarPickerGrid _yearGrid = new() { Columns = 5 };
    private readonly CalendarViewItem _previous = NewItem(CalendarViewItemKind.Previous);
    private readonly CalendarViewItem _next = NewItem(CalendarViewItemKind.Next);
    private readonly CalendarViewItem _monthToggle = NewItem(CalendarViewItemKind.MonthToggle);
    private readonly CalendarViewItem _yearToggle = NewItem(CalendarViewItemKind.YearToggle);
    private Decorator? _header;
    private Decorator? _body;
    private DateTime _today;
    private int _yearPage;
    private string? _size;

    static CalendarView()
    {
        foreach (var property in new AvaloniaProperty[]
        {
            EndDateProperty, IsRangeProperty, DisplayDateProperty, NumberOfMonthsProperty,
            FirstDayOfWeekProperty, DisabledDaysOfWeekProperty, DisabledMatcherProperty,
        })
        {
            property.Changed.AddClassHandler<CalendarView>((c, _) => c.Refresh());
        }
        AffectsMeasure<CalendarView>(MonthWidthProperty, NumberOfMonthsProperty);
    }

    /// <summary>Creates a calendar showing today's month.</summary>
    public CalendarView()
    {
        _today = DateTime.Today;
        SetCurrentValue(DisplayDateProperty, _today);
        DisabledDates.CollectionChanged += (_, _) => Refresh();
        // The cells take the calendar's size class; pseudo-classes (:pointerover) change nothing.
        Classes.CollectionChanged += (_, _) =>
        {
            var size = Array.Find(SizeClasses, Classes.Contains);
            if (size != _size)
            {
                _size = size;
                Refresh();
            }
        };
        AddHandler(Button.ClickEvent, OnItemClick);
    }

    /// <inheritdoc cref="DateProperty"/>
    public DateTime? Date { get => GetValue(DateProperty); set => SetValue(DateProperty, value); }

    /// <inheritdoc cref="EndDateProperty"/>
    public DateTime? EndDate { get => GetValue(EndDateProperty); set => SetValue(EndDateProperty, value); }

    /// <inheritdoc cref="IsRangeProperty"/>
    public bool IsRange { get => GetValue(IsRangeProperty); set => SetValue(IsRangeProperty, value); }

    /// <inheritdoc cref="DisplayDateProperty"/>
    public DateTime DisplayDate { get => GetValue(DisplayDateProperty); set => SetValue(DisplayDateProperty, value); }

    /// <inheritdoc cref="DisplayModeProperty"/>
    public CalendarViewMode DisplayMode { get => GetValue(DisplayModeProperty); set => SetValue(DisplayModeProperty, value); }

    /// <inheritdoc cref="NumberOfMonthsProperty"/>
    public int NumberOfMonths { get => GetValue(NumberOfMonthsProperty); set => SetValue(NumberOfMonthsProperty, value); }

    /// <inheritdoc cref="FirstDayOfWeekProperty"/>
    public DayOfWeek FirstDayOfWeek { get => GetValue(FirstDayOfWeekProperty); set => SetValue(FirstDayOfWeekProperty, value); }

    /// <inheritdoc cref="DisabledDaysOfWeekProperty"/>
    public DaysOfWeek DisabledDaysOfWeek { get => GetValue(DisabledDaysOfWeekProperty); set => SetValue(DisabledDaysOfWeekProperty, value); }

    /// <inheritdoc cref="DisabledMatcherProperty"/>
    public Func<DateTime, bool>? DisabledMatcher { get => GetValue(DisabledMatcherProperty); set => SetValue(DisabledMatcherProperty, value); }

    /// <summary>Ranges of days that cannot be selected (GPUI's Matcher::Range and Matcher::Interval).</summary>
    public AvaloniaList<DateRange> DisabledDates { get; } = [];

    /// <inheritdoc cref="MonthWidthProperty"/>
    public double MonthWidth { get => GetValue(MonthWidthProperty); set => SetValue(MonthWidthProperty, value); }

    /// <inheritdoc cref="SelectedEvent"/>
    public event EventHandler<CalendarSelectedEventArgs>? Selected
    {
        add => AddHandler(SelectedEvent, value);
        remove => RemoveHandler(SelectedEvent, value);
    }

    /// <summary>
    /// The day marked as today, read once when the calendar is created
    /// (calendar.rs reads Local::now() in CalendarState::new). Tests pin it, as
    /// the GPUI reference pins GPUI's (R7).
    /// </summary>
    internal DateTime Today
    {
        get => _today;
        set
        {
            _today = value.Date;
            // CalendarState::new: without a value the calendar opens on today's month.
            if (Date is null)
            {
                SetCurrentValue(DisplayDateProperty, _today);
            }
            if (DisplayMode == CalendarViewMode.Year)
            {
                _yearPage = PageOf(DisplayDate.Year);
            }
            Refresh();
        }
    }

    /// <summary>Whether <paramref name="date"/> cannot be selected.</summary>
    public bool IsDateDisabled(DateTime date) =>
        DateMatcher.Of(DisabledDaysOfWeek, DisabledDates, DisabledMatcher).Matches(date);

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_header is not null)
        {
            _header.Child = null;
        }
        if (_body is not null)
        {
            _body.Child = null;
        }
        _header = e.NameScope.Find<Decorator>("PART_Header");
        _body = e.NameScope.Find<Decorator>("PART_Body");
        if (_header is not null)
        {
            _header.Child = _headerRow;
        }
        Refresh();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DateProperty)
        {
            // calendar.rs apply_date: the displayed month follows the value's start.
            if (Date is { } date)
            {
                SetCurrentValue(DisplayDateProperty, date.Date);
            }
            Refresh();
        }
        else if (change.Property == DisplayModeProperty)
        {
            if (DisplayMode == CalendarViewMode.Year)
            {
                _yearPage = PageOf(DisplayDate.Year);
            }
            Refresh();
        }
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // calendar.rs: the card is w(248) per month (220 small, 304 large); the
        // cells may overflow its content box.
        var width = MonthWidth * NumberOfMonths;
        if (double.IsNaN(width) || !double.IsNaN(Width))
        {
            return base.MeasureOverride(availableSize);
        }
        var size = base.MeasureOverride(new Size(width, availableSize.Height));
        return new Size(width, size.Height);
    }

    /// <summary>
    /// Clicks a day as GPUI's activate_date does: selects it (or a range's end)
    /// and raises <see cref="Selected"/> once the value is complete. Returns
    /// whether it did.
    /// </summary>
    internal bool ActivateDate(DateTime value)
    {
        value = value.Date;
        if (IsDateDisabled(value))
        {
            return false;
        }
        // calendar.rs select_date.
        DateTime? start = Date?.Date;
        DateTime? end = EndDate?.Date;
        if (!IsRange || start is null)
        {
            (start, end) = (value, null);
        }
        else if (end is null && value >= start)
        {
            end = value;
        }
        else
        {
            (start, end) = (value, null);
        }
        SetCurrentValue(DateProperty, start);
        SetCurrentValue(EndDateProperty, end);
        var complete = !IsRange || end is not null;
        if (complete)
        {
            RaiseEvent(new CalendarSelectedEventArgs(SelectedEvent, start, end));
        }
        return complete;
    }

    private void OnItemClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not CalendarViewItem item)
        {
            return;
        }
        e.Handled = true;
        var display = new DateTime(DisplayDate.Year, DisplayDate.Month, 1);
        switch (item.Kind)
        {
            case CalendarViewItemKind.Previous when DisplayMode == CalendarViewMode.Day:
                SetCurrentValue(DisplayDateProperty, display.AddMonths(-1));
                break;
            case CalendarViewItemKind.Next when DisplayMode == CalendarViewMode.Day:
                SetCurrentValue(DisplayDateProperty, display.AddMonths(1));
                break;
            case CalendarViewItemKind.Previous when _yearPage > 0:
                _yearPage--;
                Refresh();
                break;
            case CalendarViewItemKind.Next when _yearPage < PageCount - 1:
                _yearPage++;
                Refresh();
                break;
            // A toggle opens its grid, or returns to the days if its grid is open.
            case CalendarViewItemKind.MonthToggle:
                SetCurrentValue(DisplayModeProperty, DisplayMode == CalendarViewMode.Month ? CalendarViewMode.Day : CalendarViewMode.Month);
                break;
            case CalendarViewItemKind.YearToggle:
                SetCurrentValue(DisplayModeProperty, DisplayMode == CalendarViewMode.Year ? CalendarViewMode.Day : CalendarViewMode.Year);
                break;
            case CalendarViewItemKind.Day:
                ActivateDate(item.Date);
                break;
            // select_month / select_year: picking one returns to the days.
            case CalendarViewItemKind.Month:
                SetCurrentValue(DisplayDateProperty, new DateTime(display.Year, item.Number, 1));
                SetCurrentValue(DisplayModeProperty, CalendarViewMode.Day);
                break;
            case CalendarViewItemKind.Year:
                SetCurrentValue(DisplayDateProperty, new DateTime(item.Number, display.Month, 1));
                SetCurrentValue(DisplayModeProperty, CalendarViewMode.Day);
                break;
        }
    }

    private int FirstYear => _today.Year - YearsAround;

    private int PageCount => (2 * YearsAround + YearsPerPage - 1) / YearsPerPage;

    /// <summary>The page holding <paramref name="year"/>, or the first page (calendar.rs apply_year_range).</summary>
    private int PageOf(int year) =>
        year >= FirstYear && year < _today.Year + YearsAround ? (year - FirstYear) / YearsPerPage : 0;

    private static CalendarViewItem NewItem(CalendarViewItemKind kind) => new() { Kind = kind };

    /// <summary>The year and month <paramref name="offset"/> months after the displayed one.</summary>
    private (int Year, int Month) OffsetYearMonth(int offset)
    {
        var n = DisplayDate.Month - 1 + offset;
        return (DisplayDate.Year + n / 12, n % 12 + 1);
    }

    /// <summary>
    /// The weeks a month needs, from the week of its first day to the week of
    /// its last (calendar.rs days_in_month): four to six rows.
    /// </summary>
    internal static List<DateTime[]> WeeksOf(int year, int month, DayOfWeek firstDay)
    {
        var first = new DateTime(year, month, 1);
        var offset = ((int)first.DayOfWeek + 7 - (int)firstDay) % 7;
        var start = first.AddDays(-offset);
        var next = first.AddMonths(1);
        var count = ((next - start).Days + 6) / 7;
        var weeks = new List<DateTime[]>(count);
        for (var w = 0; w < count; w++)
        {
            var week = new DateTime[7];
            for (var d = 0; d < 7; d++)
            {
                week[d] = start.AddDays(w * 7 + d);
            }
            weeks.Add(week);
        }
        return weeks;
    }

    private static DateTimeFormatInfo Format()
    {
        // Avalonia's Calendar falls back to the Gregorian names the same way.
        var format = CultureInfo.CurrentCulture.DateTimeFormat;
        return format.Calendar is GregorianCalendar ? format : CultureInfo.InvariantCulture.DateTimeFormat;
    }

    private void Refresh()
    {
        if (_header is null || _body is null)
        {
            return;
        }
        RefreshHeader();
        switch (DisplayMode)
        {
            case CalendarViewMode.Month:
                RefreshMonthGrid();
                _body.Child = _monthGrid;
                break;
            case CalendarViewMode.Year:
                RefreshYearGrid();
                _body.Child = _yearGrid;
                break;
            default:
                RefreshDays();
                _body.Child = _months;
                break;
        }
    }

    private void Prepare(CalendarViewItem item)
    {
        // The calendar's size is every item's (calendar.rs with_size).
        foreach (var size in SizeClasses)
        {
            item.Classes.Set(size, Classes.Contains(size));
        }
    }

    private void RefreshHeader()
    {
        var format = Format();
        var count = NumberOfMonths;
        var mode = DisplayMode;
        var row = _headerRow.Children;
        if (count == 1)
        {
            if (row.Count != 4 || row[1] != _monthToggle)
            {
                row.Clear();
                row.AddRange([_previous, _monthToggle, _yearToggle, _next]);
            }
            _monthToggle.Content = format.GetMonthName(DisplayDate.Month);
            _monthToggle.SetState(active: mode == CalendarViewMode.Month);
            _yearToggle.Content = DisplayDate.Year.ToString(CultureInfo.CurrentCulture);
            _yearToggle.SetState(active: mode == CalendarViewMode.Year);
            Prepare(_monthToggle);
            Prepare(_yearToggle);
        }
        else
        {
            if (row.Count != count + 2 || row[1] == _monthToggle)
            {
                row.Clear();
                row.Add(_previous);
                for (var i = 0; i < count; i++)
                {
                    // calendar.rs: each month's name over its year, centered, in place of the toggles.
                    row.Add(new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { HorizontalAlignment = HorizontalAlignment.Center },
                            new TextBlock { HorizontalAlignment = HorizontalAlignment.Center },
                        },
                    });
                }
                row.Add(_next);
            }
            for (var i = 0; i < count; i++)
            {
                var (year, month) = OffsetYearMonth(i);
                var title = (StackPanel)row[i + 1];
                ((TextBlock)title.Children[0]).Text = format.GetMonthName(month);
                ((TextBlock)title.Children[1]).Text = year.ToString(CultureInfo.CurrentCulture);
            }
        }
        // The arrows move months, or pages of years; the months grid has nowhere to go.
        _previous.SetState(disabled: mode == CalendarViewMode.Month || (mode == CalendarViewMode.Year && _yearPage <= 0));
        _next.SetState(disabled: mode == CalendarViewMode.Month || (mode == CalendarViewMode.Year && _yearPage >= PageCount - 1));
        Prepare(_previous);
        Prepare(_next);
    }

    private void RefreshDays()
    {
        var count = NumberOfMonths;
        var blocks = _months.Children;
        while (blocks.Count > count)
        {
            blocks.RemoveAt(blocks.Count - 1);
        }
        while (blocks.Count < count)
        {
            blocks.Add(new StackPanel());
        }
        var format = Format();
        var matcher = DateMatcher.Of(DisabledDaysOfWeek, DisabledDates, DisabledMatcher);
        DateTime? start = Date?.Date;
        DateTime? end = IsRange ? EndDate?.Date : null;
        for (var offset = 0; offset < count; offset++)
        {
            var (year, month) = OffsetYearMonth(offset);
            var weeks = WeeksOf(year, month, FirstDayOfWeek);
            var rows = ((StackPanel)blocks[offset]).Children;
            while (rows.Count > weeks.Count + 1)
            {
                rows.RemoveAt(rows.Count - 1);
            }
            while (rows.Count < weeks.Count + 1)
            {
                var week = new StackPanel { Orientation = Orientation.Horizontal };
                for (var d = 0; d < 7; d++)
                {
                    week.Children.Add(NewItem(rows.Count == 0 ? CalendarViewItemKind.Weekday : CalendarViewItemKind.Day));
                }
                rows.Add(week);
            }
            for (var d = 0; d < 7; d++)
            {
                var title = (CalendarViewItem)((StackPanel)rows[0]).Children[d];
                title.Content = format.ShortestDayNames[((int)FirstDayOfWeek + d) % 7];
                title.SetState(muted: true, disabled: true);
                Prepare(title);
            }
            for (var w = 0; w < weeks.Count; w++)
            {
                var cells = ((StackPanel)rows[w + 1]).Children;
                for (var d = 0; d < 7; d++)
                {
                    var date = weeks[w][d];
                    var item = (CalendarViewItem)cells[d];
                    var disabled = matcher.Matches(date);
                    item.Date = date;
                    item.Content = date.Day.ToString(CultureInfo.CurrentCulture);
                    item.SetState(
                        active: date == start || date == end,
                        inRange: start is { } a && end is { } b && date >= a && date <= b,
                        muted: date.Month != month || disabled,
                        today: date == _today,
                        disabled: disabled);
                    Prepare(item);
                }
            }
        }
    }

    private void RefreshMonthGrid()
    {
        var format = Format();
        var items = _monthGrid.Children;
        while (items.Count < 12)
        {
            items.Add(NewItem(CalendarViewItemKind.Month));
        }
        for (var m = 1; m <= 12; m++)
        {
            var item = (CalendarViewItem)items[m - 1];
            item.Number = m;
            item.Content = format.GetMonthName(m);
            item.SetState(active: m == DisplayDate.Month);
            Prepare(item);
        }
    }

    private void RefreshYearGrid()
    {
        var first = FirstYear + _yearPage * YearsPerPage;
        var count = Math.Min(YearsPerPage, _today.Year + YearsAround - first);
        var items = _yearGrid.Children;
        while (items.Count > count)
        {
            items.RemoveAt(items.Count - 1);
        }
        while (items.Count < count)
        {
            items.Add(NewItem(CalendarViewItemKind.Year));
        }
        for (var i = 0; i < count; i++)
        {
            var item = (CalendarViewItem)items[i];
            item.Number = first + i;
            item.Content = item.Number.ToString(CultureInfo.CurrentCulture);
            item.SetState(active: item.Number == DisplayDate.Year);
            Prepare(item);
        }
    }
}
