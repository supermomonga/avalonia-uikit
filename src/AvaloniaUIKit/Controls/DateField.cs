using System.Globalization;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// A preset of a <see cref="DateField"/>'s popup (GPUI's DateRangePreset): a
/// label, and the date, or the dates of a range, it selects. A preset keeps
/// the field's time of day. A range preset on a single-date field selects its
/// first day; a single date on a range field selects that one day.
/// </summary>
public class DateRangePreset
{
    /// <summary>Creates an empty preset; set <see cref="Label"/> and the dates.</summary>
    public DateRangePreset()
    {
    }

    /// <summary>Creates a preset selecting <paramref name="date"/>, or a range up to <paramref name="endDate"/>.</summary>
    public DateRangePreset(string label, DateTime date, DateTime? endDate = null)
    {
        Label = label;
        Date = date;
        EndDate = endDate;
    }

    /// <summary>The button's text.</summary>
    public string? Label { get; set; }

    /// <summary>The date, or the range's first day.</summary>
    public DateTime? Date { get; set; }

    /// <summary>The range's last day (null for a single date).</summary>
    public DateTime? EndDate { get; set; }

    /// <inheritdoc />
    public override string ToString() => Label ?? string.Empty;
}

/// <summary>A value the user gave a <see cref="DateField"/> (GPUI's DatePickerEvent::Change).</summary>
public class DateFieldChangedEventArgs(RoutedEvent routedEvent, DateTime? date, DateTime? endDate) : RoutedEventArgs(routedEvent)
{
    /// <summary>The date (with its time of day), the range's first day, or null once cleared.</summary>
    public DateTime? Date { get; } = date;

    /// <summary>The range's last day (null for a single date).</summary>
    public DateTime? EndDate { get; } = endDate;
}

/// <summary>
/// GPUI Kit's DatePicker (crates/component/src/time/date_picker.rs), renamed
/// as Avalonia has a DatePicker: an input-styled field showing the date (or
/// the placeholder) and a calendar icon, which opens a
/// <see cref="CalendarView"/> in a popup below it.
/// <para>
/// Picking a day sets <see cref="Date"/>, closes the popup and raises
/// <see cref="Changed"/>. With <see cref="IsRange"/> the popup selects a
/// range: the field changes once the second day is picked. With
/// <see cref="TimePrecision"/> the popup also edits the time of day in a
/// <see cref="TimeField"/> below the calendar; picking a day then keeps the
/// popup open and every edit applies at once, and picking the selected day
/// again closes it (a range edits dates only). <see cref="Presets"/> are
/// buttons beside the calendar. <see cref="IsCleanable"/> replaces the icon
/// with a clear button while there is a value; Delete or Backspace clears it
/// too. Enter opens and closes the popup, Escape closes it. The text is
/// <see cref="DateFormat"/> (GPUI's "%Y/%m/%d", with the time when it is
/// edited); without a value the field shows <see cref="PlaceholderText"/>.
/// Size classes: xsmall, small, large (medium is the default; the popup's
/// calendar is small for small, large for large); <c>plain</c> drops the
/// field's frame (GPUI's appearance(false)).
/// </para>
/// </summary>
[TemplatePart("PART_Frame", typeof(Control))]
[TemplatePart("PART_Popup", typeof(Popup))]
[TemplatePart("PART_Calendar", typeof(CalendarView))]
[TemplatePart("PART_TimeField", typeof(TimeField))]
[TemplatePart("PART_ClearButton", typeof(Button))]
[TemplatePart("PART_Presets", typeof(ItemsControl))]
[PseudoClasses(":open", ":empty", ":cleanable", ":time", ":presets")]
public class DateField : TemplatedControl
{
    /// <summary>The date (with its time of day), or the first day of a range.</summary>
    public static readonly StyledProperty<DateTime?> DateProperty =
        AvaloniaProperty.Register<DateField, DateTime?>(nameof(Date), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The last day of a range.</summary>
    public static readonly StyledProperty<DateTime?> EndDateProperty =
        AvaloniaProperty.Register<DateField, DateTime?>(nameof(EndDate), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Whether the field holds a range of days (GPUI's DatePickerState::range).</summary>
    public static readonly StyledProperty<bool> IsRangeProperty =
        AvaloniaProperty.Register<DateField, bool>(nameof(IsRange));

    /// <summary>Whether the calendar popup is open.</summary>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<DateField, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>The .NET format of the text (null: GPUI's year/month/day, with the time when it is edited).</summary>
    public static readonly StyledProperty<string?> DateFormatProperty =
        AvaloniaProperty.Register<DateField, string?>(nameof(DateFormat));

    /// <summary>The text without a value (GPUI's "Select date").</summary>
    public static readonly StyledProperty<string?> PlaceholderTextProperty =
        AvaloniaProperty.Register<DateField, string?>(nameof(PlaceholderText), "Select date");

    /// <summary>Whether a clear button replaces the icon while there is a value (GPUI's cleanable).</summary>
    public static readonly StyledProperty<bool> IsCleanableProperty =
        AvaloniaProperty.Register<DateField, bool>(nameof(IsCleanable));

    /// <summary>The number of months the popup shows side by side.</summary>
    public static readonly StyledProperty<int> NumberOfMonthsProperty =
        CalendarView.NumberOfMonthsProperty.AddOwner<DateField>();

    /// <summary>The day each week starts on.</summary>
    public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty =
        CalendarView.FirstDayOfWeekProperty.AddOwner<DateField>();

    /// <summary>Weekdays that cannot be picked.</summary>
    public static readonly StyledProperty<DaysOfWeek> DisabledDaysOfWeekProperty =
        CalendarView.DisabledDaysOfWeekProperty.AddOwner<DateField>();

    /// <summary>A predicate for days that cannot be picked.</summary>
    public static readonly StyledProperty<Func<DateTime, bool>?> DisabledMatcherProperty =
        CalendarView.DisabledMatcherProperty.AddOwner<DateField>();

    /// <summary>Edit the time of day down to this precision (null: dates only).</summary>
    public static readonly StyledProperty<TimePrecision?> TimePrecisionProperty =
        AvaloniaProperty.Register<DateField, TimePrecision?>(nameof(TimePrecision));

    /// <summary>The clock of the time field and the text.</summary>
    public static readonly StyledProperty<HourCycle> HourCycleProperty =
        TimeField.HourCycleProperty.AddOwner<DateField>();

    /// <summary>The time given to a picked day before the user edits it (00:00 by default).</summary>
    public static readonly StyledProperty<TimeSpan> DefaultTimeProperty =
        AvaloniaProperty.Register<DateField, TimeSpan>(nameof(DefaultTime));

    /// <summary>The value as the field shows it (null without a complete value).</summary>
    public static readonly DirectProperty<DateField, string?> TextProperty =
        AvaloniaProperty.RegisterDirect<DateField, string?>(nameof(Text), f => f.Text);

    /// <summary>Raised when the user picks, edits, or clears the value.</summary>
    public static readonly RoutedEvent<DateFieldChangedEventArgs> ChangedEvent =
        RoutedEvent.Register<DateField, DateFieldChangedEventArgs>(nameof(Changed), RoutingStrategies.Bubble);

    private static readonly string[] SizeClasses = ["xsmall", "small", "large"];

    private Control? _frame;
    private Popup? _popup;
    private CalendarView? _calendar;
    private TimeField? _timeField;
    private Button? _clearButton;
    private ItemsControl? _presets;
    private TimeSpan _startTime;
    private TimeSpan _endTime;
    private DateTime _today = DateTime.Today;
    private bool _syncing;
    private bool _pressed;
    private bool _clearPressed;
    private string? _text;

    static DateField()
    {
        FocusableProperty.OverrideDefaultValue<DateField>(true);
        foreach (var property in new AvaloniaProperty[]
        {
            NumberOfMonthsProperty, FirstDayOfWeekProperty, DisabledDaysOfWeekProperty, DisabledMatcherProperty, IsRangeProperty,
        })
        {
            property.Changed.AddClassHandler<DateField>((f, _) => f.SyncCalendar());
        }
    }

    /// <summary>Creates an empty field.</summary>
    public DateField()
    {
        DisabledDates.CollectionChanged += (_, _) => SyncCalendar();
        Presets.CollectionChanged += (_, _) => UpdateState();
        Classes.CollectionChanged += (_, _) => SyncSizes();
        AddHandler(Button.ClickEvent, OnPresetClick);
        AddHandler(PointerPressedEvent, OnClearPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnClearReleased, RoutingStrategies.Tunnel);
        UpdateState();
    }

    /// <inheritdoc cref="DateProperty"/>
    public DateTime? Date { get => GetValue(DateProperty); set => SetValue(DateProperty, value); }

    /// <inheritdoc cref="EndDateProperty"/>
    public DateTime? EndDate { get => GetValue(EndDateProperty); set => SetValue(EndDateProperty, value); }

    /// <inheritdoc cref="IsRangeProperty"/>
    public bool IsRange { get => GetValue(IsRangeProperty); set => SetValue(IsRangeProperty, value); }

    /// <inheritdoc cref="IsDropDownOpenProperty"/>
    public bool IsDropDownOpen { get => GetValue(IsDropDownOpenProperty); set => SetValue(IsDropDownOpenProperty, value); }

    /// <inheritdoc cref="DateFormatProperty"/>
    public string? DateFormat { get => GetValue(DateFormatProperty); set => SetValue(DateFormatProperty, value); }

    /// <inheritdoc cref="PlaceholderTextProperty"/>
    public string? PlaceholderText { get => GetValue(PlaceholderTextProperty); set => SetValue(PlaceholderTextProperty, value); }

    /// <inheritdoc cref="IsCleanableProperty"/>
    public bool IsCleanable { get => GetValue(IsCleanableProperty); set => SetValue(IsCleanableProperty, value); }

    /// <inheritdoc cref="NumberOfMonthsProperty"/>
    public int NumberOfMonths { get => GetValue(NumberOfMonthsProperty); set => SetValue(NumberOfMonthsProperty, value); }

    /// <inheritdoc cref="FirstDayOfWeekProperty"/>
    public DayOfWeek FirstDayOfWeek { get => GetValue(FirstDayOfWeekProperty); set => SetValue(FirstDayOfWeekProperty, value); }

    /// <inheritdoc cref="DisabledDaysOfWeekProperty"/>
    public DaysOfWeek DisabledDaysOfWeek { get => GetValue(DisabledDaysOfWeekProperty); set => SetValue(DisabledDaysOfWeekProperty, value); }

    /// <inheritdoc cref="DisabledMatcherProperty"/>
    public Func<DateTime, bool>? DisabledMatcher { get => GetValue(DisabledMatcherProperty); set => SetValue(DisabledMatcherProperty, value); }

    /// <summary>Ranges of days that cannot be picked.</summary>
    public AvaloniaList<DateRange> DisabledDates { get; } = [];

    /// <inheritdoc cref="TimePrecisionProperty"/>
    public TimePrecision? TimePrecision { get => GetValue(TimePrecisionProperty); set => SetValue(TimePrecisionProperty, value); }

    /// <inheritdoc cref="HourCycleProperty"/>
    public HourCycle HourCycle { get => GetValue(HourCycleProperty); set => SetValue(HourCycleProperty, value); }

    /// <inheritdoc cref="DefaultTimeProperty"/>
    public TimeSpan DefaultTime { get => GetValue(DefaultTimeProperty); set => SetValue(DefaultTimeProperty, value); }

    /// <summary>Buttons beside the calendar that pick a date or a range (GPUI's presets).</summary>
    public AvaloniaList<DateRangePreset> Presets { get; } = [];

    /// <inheritdoc cref="TextProperty"/>
    public string? Text { get => _text; private set => SetAndRaise(TextProperty, ref _text, value); }

    /// <inheritdoc cref="ChangedEvent"/>
    public event EventHandler<DateFieldChangedEventArgs>? Changed
    {
        add => AddHandler(ChangedEvent, value);
        remove => RemoveHandler(ChangedEvent, value);
    }

    /// <summary>The popup calendar's today (see <see cref="CalendarView.Today"/>).</summary>
    internal DateTime Today
    {
        get => _today;
        set
        {
            _today = value.Date;
            if (_calendar is not null)
            {
                _calendar.Today = _today;
            }
        }
    }

    /// <summary>Whether the popup edits the time of day (date_picker.rs edited_time_precision: single dates only).</summary>
    private bool EditsTime => TimePrecision is not null && !IsRange;

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_calendar is not null)
        {
            _calendar.Selected -= OnCalendarSelected;
        }
        if (_timeField is not null)
        {
            _timeField.Changed -= OnTimeChanged;
        }
        if (_clearButton is not null)
        {
            _clearButton.Click -= OnClearClick;
        }
        _frame = e.NameScope.Find<Control>("PART_Frame");
        _popup = e.NameScope.Find<Popup>("PART_Popup");
        _calendar = e.NameScope.Find<CalendarView>("PART_Calendar");
        _timeField = e.NameScope.Find<TimeField>("PART_TimeField");
        _clearButton = e.NameScope.Find<Button>("PART_ClearButton");
        _presets = e.NameScope.Find<ItemsControl>("PART_Presets");
        if (_calendar is not null)
        {
            _calendar.Today = _today;
            _calendar.Selected += OnCalendarSelected;
        }
        if (_timeField is not null)
        {
            _timeField.Changed += OnTimeChanged;
        }
        if (_clearButton is not null)
        {
            _clearButton.Click += OnClearClick;
        }
        if (_presets is not null)
        {
            _presets.ItemsSource = Presets;
        }
        SyncSizes();
        SyncCalendar();
        SyncTimeField();
        UpdateState();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DateProperty || change.Property == EndDateProperty)
        {
            if (!_syncing)
            {
                // set_date_time: the value's times are the field's from now on.
                if (change.Property == DateProperty && Date is { } date)
                {
                    _startTime = Truncate(date.TimeOfDay);
                    SyncTimeField();
                }
                else if (change.Property == EndDateProperty && EndDate is { } end)
                {
                    _endTime = Truncate(end.TimeOfDay);
                }
                SyncCalendar();
            }
            UpdateState();
        }
        else if (change.Property == IsDropDownOpenProperty)
        {
            var open = change.GetNewValue<bool>();
            PseudoClasses.Set(":open", open);
            if (!open && TopLevel.GetTopLevel(this) is { } top && top.FocusManager?.GetFocusedElement() is Visual focused &&
                _popup?.Child is Visual content && content.IsVisualAncestorOf(focused))
            {
                // date_picker.rs focus_back_if_need: closing from inside the popup returns the focus.
                Focus();
            }
        }
        else if (change.Property == TimePrecisionProperty || change.Property == HourCycleProperty)
        {
            _startTime = Truncate(_startTime);
            _endTime = Truncate(_endTime);
            SyncTimeField();
            UpdateState();
        }
        else if (change.Property == DefaultTimeProperty && Date is null)
        {
            _startTime = _endTime = Truncate(DefaultTime);
            SyncTimeField();
        }
        else if (change.Property == DateFormatProperty || change.Property == PlaceholderTextProperty ||
                 change.Property == IsCleanableProperty || change.Property == IsEffectivelyEnabledProperty)
        {
            UpdateState();
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pressed = false;
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && InFrame(e.Source))
        {
            Focus(NavigationMethod.Pointer);
            _pressed = true;
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        // date_picker.rs: a click on the field opens the calendar; while it is open the click closes it (outside the popup).
        if (_pressed && InFrame(e.Source) && !IsDropDownOpen)
        {
            SetCurrentValue(IsDropDownOpenProperty, true);
            e.Handled = true;
        }
        _pressed = false;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.KeyModifiers != KeyModifiers.None)
        {
            return;
        }
        switch (e.Key)
        {
            // Enter opens the picker, and closes it again once the value shown is the one wanted.
            case Key.Enter:
                SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
                break;
            case Key.Escape when IsDropDownOpen:
                SetCurrentValue(IsDropDownOpenProperty, false);
                break;
            case Key.Back or Key.Delete:
                Clean();
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    private bool InFrame(object? source) =>
        source is Visual visual && _frame is not null && (visual == _frame || _frame.IsVisualAncestorOf(visual));

    private TimeSpan Truncate(TimeSpan time)
    {
        var t = TimeSpan.FromSeconds(Math.Floor(((time.TotalSeconds % 86400) + 86400) % 86400));
        return TimePrecision == AvaloniaUIKit.TimePrecision.Minute ? TimeSpan.FromMinutes(Math.Floor(t.TotalMinutes)) : t;
    }

    private void OnCalendarSelected(object? sender, CalendarSelectedEventArgs e)
    {
        e.Handled = true;
        if (EditsTime)
        {
            // Clicking the selected day again confirms it, so picking a date and closing is a double-click.
            if (e.Date == Date?.Date)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
                return;
            }
            // Keep the popup open so the time can be adjusted next.
            SetValueCore(e.Date, null);
            RaiseChanged();
        }
        else
        {
            Update(e.Date, e.EndDate);
            Focus();
        }
    }

    private void OnTimeChanged(object? sender, TimeFieldChangedEventArgs e)
    {
        e.Handled = true;
        _startTime = e.Time;
        if (Date is { } date)
        {
            SetValueCore(date.Date, EndDate);
            RaiseChanged();
        }
    }

    private void OnClearClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        Clean();
    }

    private bool InClearButton(object? source) =>
        source is Visual visual && _clearButton is not null && (visual == _clearButton || _clearButton.IsVisualAncestorOf(visual));

    // clear_button is not a tab stop and GPUI's button prevents the press from
    // focusing anything: the press stops before Avalonia's focus manager and
    // the button, and the click is the press and release on it.
    private void OnClearPressed(object? sender, PointerPressedEventArgs e)
    {
        if (InClearButton(e.Source) && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _clearPressed = true;
            e.Handled = true;
        }
    }

    private void OnClearReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_clearPressed && InClearButton(e.Source))
        {
            e.Handled = true;
            Clean();
        }
        _clearPressed = false;
    }

    private void OnPresetClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { DataContext: DateRangePreset preset } button || _presets is null || !_presets.IsVisualAncestorOf(button))
        {
            return;
        }
        e.Handled = true;
        if (preset.Date is not { } start)
        {
            return;
        }
        var end = IsRange ? preset.EndDate ?? start : (DateTime?)null;
        Update(start.Date, end?.Date);
        Focus();
    }

    /// <summary>Clears the value and resets the times (date_picker.rs clean).</summary>
    private void Clean()
    {
        _startTime = _endTime = Truncate(DefaultTime);
        SyncTimeField();
        Update(null, null);
    }

    /// <summary>update_date: sets the value, shows it in the calendar, closes the popup and raises <see cref="Changed"/>.</summary>
    private void Update(DateTime? date, DateTime? end)
    {
        SetValueCore(date, end);
        SyncCalendar();
        SetCurrentValue(IsDropDownOpenProperty, false);
        RaiseChanged();
    }

    private void SetValueCore(DateTime? date, DateTime? end)
    {
        _syncing = true;
        try
        {
            SetCurrentValue(DateProperty, date?.Date + _startTime);
            SetCurrentValue(EndDateProperty, IsRange ? end?.Date + _endTime : null);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void RaiseChanged()
    {
        // emit_change: only a complete value (or a cleared one) is reported.
        if (Date is null || !IsRange || EndDate is not null)
        {
            RaiseEvent(new DateFieldChangedEventArgs(ChangedEvent, Date, IsRange ? EndDate : null));
        }
    }

    private void SyncSizes()
    {
        // The field's size is the popup's calendar and time field's (with_size).
        foreach (var size in SizeClasses)
        {
            _calendar?.Classes.Set(size, Classes.Contains(size));
            _timeField?.Classes.Set(size, Classes.Contains(size));
        }
    }

    private void SyncCalendar()
    {
        if (_calendar is null)
        {
            return;
        }
        _calendar.IsRange = IsRange;
        _calendar.NumberOfMonths = NumberOfMonths;
        _calendar.FirstDayOfWeek = FirstDayOfWeek;
        _calendar.DisabledDaysOfWeek = DisabledDaysOfWeek;
        _calendar.DisabledMatcher = DisabledMatcher;
        _calendar.DisabledDates.Clear();
        _calendar.DisabledDates.AddRange(DisabledDates);
        _calendar.Date = Date?.Date;
        _calendar.EndDate = IsRange ? EndDate?.Date : null;
    }

    private void SyncTimeField()
    {
        if (_timeField is null)
        {
            return;
        }
        _timeField.Precision = TimePrecision ?? AvaloniaUIKit.TimePrecision.Minute;
        _timeField.HourCycle = HourCycle;
        _timeField.Time = _startTime;
    }

    /// <summary>The text format: the app's, or GPUI's "%Y/%m/%d" and the edited time.</summary>
    private string DisplayFormat()
    {
        if (DateFormat is { } format)
        {
            return format;
        }
        const string date = "yyyy'/'MM'/'dd";
        return (EditsTime ? TimePrecision : null, HourCycle) switch
        {
            (AvaloniaUIKit.TimePrecision.Minute, HourCycle.H23) => date + " HH:mm",
            (AvaloniaUIKit.TimePrecision.Second, HourCycle.H23) => date + " HH:mm:ss",
            (AvaloniaUIKit.TimePrecision.Minute, HourCycle.H12) => date + " hh:mm tt",
            (AvaloniaUIKit.TimePrecision.Second, HourCycle.H12) => date + " hh:mm:ss tt",
            _ => date,
        };
    }

    private void UpdateState()
    {
        var format = DisplayFormat();
        string Show(DateTime d) => d.ToString(format, CultureInfo.CurrentCulture);
        Text = (Date, IsRange ? EndDate : null) switch
        {
            ({ } a, { } b) => $"{Show(a)} - {Show(b)}",
            ({ } a, null) when !IsRange => Show(a),
            _ => null,
        };
        PseudoClasses.Set(":empty", Text is null);
        PseudoClasses.Set(":cleanable", IsCleanable && Date is not null);
        PseudoClasses.Set(":time", EditsTime);
        PseudoClasses.Set(":presets", Presets.Count > 0);
    }
}
