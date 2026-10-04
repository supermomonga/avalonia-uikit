using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>The smallest unit a <see cref="TimeField"/> edits (GPUI's TimePrecision).</summary>
public enum TimePrecision
{
    /// <summary>Hours and minutes, e.g. 09:30.</summary>
    Minute,
    /// <summary>Hours, minutes and seconds, e.g. 09:30:15.</summary>
    Second,
}

/// <summary>How a <see cref="TimeField"/> counts the hours of a day (GPUI's HourCycle, after Unicode's hourCycle).</summary>
public enum HourCycle
{
    /// <summary>A 24-hour clock from 00 to 23.</summary>
    H23,
    /// <summary>A 12-hour clock from 12 to 11, with an AM/PM segment.</summary>
    H12,
}

/// <summary>One editable part of a <see cref="TimeField"/> (GPUI's TimeSegment).</summary>
public enum TimeSegment
{
    /// <summary>The hour.</summary>
    Hour,
    /// <summary>The minute.</summary>
    Minute,
    /// <summary>The second, with <see cref="TimePrecision.Second"/>.</summary>
    Second,
    /// <summary>AM or PM, with <see cref="HourCycle.H12"/>.</summary>
    Period,
}

/// <summary>A time the user typed into a <see cref="TimeField"/> (GPUI's TimeFieldEvent::Change).</summary>
public class TimeFieldChangedEventArgs(RoutedEvent routedEvent, TimeSpan time) : RoutedEventArgs(routedEvent)
{
    /// <summary>The new time of day.</summary>
    public TimeSpan Time { get; } = time;
}

/// <summary>
/// GPUI Kit's TimeField (crates/component/src/time/time_field.rs over
/// crates/base/src/time_field.rs): a time of day edited segment by segment
/// in an input frame, e.g. 09:30, 09:30:15 or 09:30 PM.
/// <para>
/// The field is one Tab stop; one segment is selected at a time and shows the
/// selection while the field has the focus. ↑/↓ step the selected segment and
/// wrap within it (no carry into the next unit); ←/→ and Tab/Shift+Tab move
/// between segments (Tab leaves the field from the last one); digits fill the
/// segment with a two-digit buffer that moves on once no further digit could
/// fit; a/p set the period; Backspace/Delete reset the segment to its first
/// value. A click selects the segment under the pointer. Edits raise
/// <see cref="Changed"/>; setting <see cref="Time"/> does not. Without a time
/// the segments show "--" (typing starts from 00:00:00). Invalid
/// (<c>DataValidationErrors</c>) turns the border to the danger color. Size
/// classes: xsmall, small, large (medium is the default).
/// </para>
/// </summary>
[TemplatePart("PART_Hour", typeof(Control))]
[TemplatePart("PART_Minute", typeof(Control))]
[TemplatePart("PART_Second", typeof(Control))]
[TemplatePart("PART_Period", typeof(Control))]
[TemplatePart("PART_HourText", typeof(TextBlock))]
[TemplatePart("PART_MinuteText", typeof(TextBlock))]
[TemplatePart("PART_SecondText", typeof(TextBlock))]
[TemplatePart("PART_PeriodText", typeof(TextBlock))]
[PseudoClasses(":empty", ":seconds", ":twelve-hour")]
public class TimeField : TemplatedControl
{
    /// <summary>The time of day (00:00 to 23:59:59), or null for none.</summary>
    public static readonly StyledProperty<TimeSpan?> TimeProperty =
        AvaloniaProperty.Register<TimeField, TimeSpan?>(nameof(Time), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Minutes (the default) or seconds.</summary>
    public static readonly StyledProperty<TimePrecision> PrecisionProperty =
        AvaloniaProperty.Register<TimeField, TimePrecision>(nameof(Precision));

    /// <summary>A 24-hour clock (the default) or a 12-hour clock with AM/PM.</summary>
    public static readonly StyledProperty<HourCycle> HourCycleProperty =
        AvaloniaProperty.Register<TimeField, HourCycle>(nameof(HourCycle));

    /// <summary>The segment keyboard editing applies to.</summary>
    public static readonly DirectProperty<TimeField, TimeSegment> SelectedSegmentProperty =
        AvaloniaProperty.RegisterDirect<TimeField, TimeSegment>(nameof(SelectedSegment), f => f.SelectedSegment, (f, v) => f.SelectedSegment = v);

    /// <summary>Raised when an edit changes the time.</summary>
    public static readonly RoutedEvent<TimeFieldChangedEventArgs> ChangedEvent =
        RoutedEvent.Register<TimeField, TimeFieldChangedEventArgs>(nameof(Changed), RoutingStrategies.Bubble);

    private SegmentEditor _editor = new();
    private bool _syncing;
    private Control? _hour, _minute, _second, _period;
    private TextBlock? _hourText, _minuteText, _secondText, _periodText;

    static TimeField()
    {
        FocusableProperty.OverrideDefaultValue<TimeField>(true);
    }

    /// <summary>Creates an empty field.</summary>
    public TimeField()
    {
        UpdateSegments();
    }

    /// <inheritdoc cref="TimeProperty"/>
    public TimeSpan? Time { get => GetValue(TimeProperty); set => SetValue(TimeProperty, value); }

    /// <inheritdoc cref="PrecisionProperty"/>
    public TimePrecision Precision { get => GetValue(PrecisionProperty); set => SetValue(PrecisionProperty, value); }

    /// <inheritdoc cref="HourCycleProperty"/>
    public HourCycle HourCycle { get => GetValue(HourCycleProperty); set => SetValue(HourCycleProperty, value); }

    /// <inheritdoc cref="SelectedSegmentProperty"/>
    public TimeSegment SelectedSegment
    {
        get => _editor.Segment;
        set
        {
            var old = _editor.Segment;
            if (_editor.SelectSegment(value))
            {
                RaisePropertyChanged(SelectedSegmentProperty, old, _editor.Segment);
                UpdateSegments();
            }
        }
    }

    /// <inheritdoc cref="ChangedEvent"/>
    public event EventHandler<TimeFieldChangedEventArgs>? Changed
    {
        add => AddHandler(ChangedEvent, value);
        remove => RemoveHandler(ChangedEvent, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _hour = e.NameScope.Find<Control>("PART_Hour");
        _minute = e.NameScope.Find<Control>("PART_Minute");
        _second = e.NameScope.Find<Control>("PART_Second");
        _period = e.NameScope.Find<Control>("PART_Period");
        _hourText = e.NameScope.Find<TextBlock>("PART_HourText");
        _minuteText = e.NameScope.Find<TextBlock>("PART_MinuteText");
        _secondText = e.NameScope.Find<TextBlock>("PART_SecondText");
        _periodText = e.NameScope.Find<TextBlock>("PART_PeriodText");
        UpdateSegments();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TimeProperty && !_syncing)
        {
            // set_time: the owner's value, without an event; a half-typed segment survives an echo.
            if (Time is { } time)
            {
                _editor.SetTime(TimeOnly.FromTimeSpan(Wrap(time)));
            }
            else
            {
                _editor.SetTime(TimeOnly.MinValue);
                _editor.PendingDigit = null;
            }
            UpdateSegments();
        }
        else if (change.Property == PrecisionProperty || change.Property == HourCycleProperty)
        {
            var old = _editor.Segment;
            _editor.SetFormat(Precision, HourCycle);
            if (old != _editor.Segment)
            {
                RaisePropertyChanged(SelectedSegmentProperty, old, _editor.Segment);
            }
            UpdateSegments();
        }
    }

    /// <inheritdoc />
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        UpdateSegments();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        UpdateSegments();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            return;
        }
        // time_field.rs: a press on a segment focuses the field and selects it.
        Focus(NavigationMethod.Pointer);
        if (SegmentAt(e.Source) is { } segment)
        {
            SelectedSegment = segment;
        }
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !IsEffectivelyEnabled)
        {
            return;
        }
        var modified = (e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Meta)) != 0;
        switch (e.Key)
        {
            case Key.Up when !modified:
                Edit(static (ref SegmentEditor ed) => ed.Step(1), materializes: true);
                break;
            case Key.Down when !modified:
                Edit(static (ref SegmentEditor ed) => ed.Step(-1), materializes: true);
                break;
            case Key.Left when !modified:
                Move(-1);
                break;
            case Key.Right when !modified:
                Move(1);
                break;
            // Tab walks the segments first and leaves the field only from the last one.
            case Key.Tab when !modified:
                if (!Move(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -1 : 1))
                {
                    return;
                }
                break;
            case Key.Back or Key.Delete when !modified:
                Edit(static (ref SegmentEditor ed) => ed.ClearSegment(), materializes: false);
                break;
            default:
                if (modified || Typed(e) is not { } ch)
                {
                    return;
                }
                if (ch is >= '0' and <= '9')
                {
                    var digit = ch - '0';
                    Edit((ref SegmentEditor ed) => ed.InputDigit(digit), materializes: _editor.Segment != TimeSegment.Period);
                }
                else if (ch is 'a' or 'A' or 'p' or 'P')
                {
                    var pm = ch is 'p' or 'P';
                    Edit((ref SegmentEditor ed) => ed.InputPeriod(pm), materializes: _editor.Segment == TimeSegment.Period);
                }
                else
                {
                    return;
                }
                break;
        }
        e.Handled = true;
    }

    private static char? Typed(KeyEventArgs e)
    {
        if (e.KeySymbol is { Length: 1 } symbol)
        {
            return symbol[0];
        }
        return e.Key switch
        {
            >= Key.D0 and <= Key.D9 => (char)('0' + (e.Key - Key.D0)),
            >= Key.NumPad0 and <= Key.NumPad9 => (char)('0' + (e.Key - Key.NumPad0)),
            Key.A => 'a',
            Key.P => 'p',
            _ => null,
        };
    }

    private delegate bool EditAction(ref SegmentEditor editor);

    /// <summary>Applies an edit; a change (or any edit of an empty field that types into it) sets the time and raises <see cref="Changed"/>.</summary>
    private void Edit(EditAction edit, bool materializes)
    {
        var old = _editor.Segment;
        var changed = edit(ref _editor);
        if (changed || (materializes && Time is null))
        {
            _syncing = true;
            try
            {
                SetCurrentValue(TimeProperty, _editor.Time.ToTimeSpan());
            }
            finally
            {
                _syncing = false;
            }
            RaiseEvent(new TimeFieldChangedEventArgs(ChangedEvent, _editor.Time.ToTimeSpan()));
        }
        if (old != _editor.Segment)
        {
            RaisePropertyChanged(SelectedSegmentProperty, old, _editor.Segment);
        }
        UpdateSegments();
    }

    private bool Move(int offset)
    {
        var old = _editor.Segment;
        var moved = _editor.MoveSegment(offset);
        if (moved)
        {
            RaisePropertyChanged(SelectedSegmentProperty, old, _editor.Segment);
            UpdateSegments();
        }
        return moved;
    }

    private TimeSegment? SegmentAt(object? source)
    {
        foreach (var visual in (source as Visual)?.GetSelfAndVisualAncestors() ?? [])
        {
            if (visual == this)
            {
                break;
            }
            if (visual == _hour)
            {
                return TimeSegment.Hour;
            }
            if (visual == _minute)
            {
                return TimeSegment.Minute;
            }
            if (visual == _second)
            {
                return TimeSegment.Second;
            }
            if (visual == _period)
            {
                return TimeSegment.Period;
            }
        }
        return null;
    }

    private static TimeSpan Wrap(TimeSpan time) =>
        TimeSpan.FromTicks(((time.Ticks % TimeSpan.TicksPerDay) + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay);

    private void UpdateSegments()
    {
        var empty = Time is null;
        PseudoClasses.Set(":empty", empty);
        PseudoClasses.Set(":seconds", _editor.Precision == TimePrecision.Second);
        PseudoClasses.Set(":twelve-hour", _editor.HourCycle == HourCycle.H12);
        string Label(TimeSegment segment) => empty ? "--" : _editor.Label(segment);
        if (_hourText is not null)
        {
            _hourText.Text = Label(TimeSegment.Hour);
        }
        if (_minuteText is not null)
        {
            _minuteText.Text = Label(TimeSegment.Minute);
        }
        if (_secondText is not null)
        {
            _secondText.Text = Label(TimeSegment.Second);
        }
        if (_periodText is not null)
        {
            _periodText.Text = Label(TimeSegment.Period);
        }
        // The selected segment shows the selection while the field has the focus (the theme's :focus).
        _hour?.Classes.Set("selected", _editor.Segment == TimeSegment.Hour);
        _minute?.Classes.Set("selected", _editor.Segment == TimeSegment.Minute);
        _second?.Classes.Set("selected", _editor.Segment == TimeSegment.Second);
        _period?.Classes.Set("selected", _editor.Segment == TimeSegment.Period);
    }

    /// <summary>
    /// The editing rules of time_field.rs (SegmentEditor), kept apart from
    /// focus and input as GPUI keeps them.
    /// </summary>
    internal struct SegmentEditor
    {
        public SegmentEditor()
        {
        }

        public TimeOnly Time { get; private set; } = TimeOnly.MinValue;
        public TimePrecision Precision { get; private set; }
        public HourCycle HourCycle { get; private set; }
        public TimeSegment Segment { get; private set; }

        /// <summary>The first digit typed into the selected segment, awaiting a second.</summary>
        public int? PendingDigit { get; set; }

        /// <summary>The segments shown, in reading order.</summary>
        public readonly TimeSegment[] Segments => (Precision, HourCycle) switch
        {
            (TimePrecision.Minute, HourCycle.H23) => [TimeSegment.Hour, TimeSegment.Minute],
            (TimePrecision.Second, HourCycle.H23) => [TimeSegment.Hour, TimeSegment.Minute, TimeSegment.Second],
            (TimePrecision.Minute, _) => [TimeSegment.Hour, TimeSegment.Minute, TimeSegment.Period],
            _ => [TimeSegment.Hour, TimeSegment.Minute, TimeSegment.Second, TimeSegment.Period],
        };

        /// <summary>The inclusive range of a segment's displayed value.</summary>
        public readonly (int Min, int Max) Bounds(TimeSegment segment) => (segment, HourCycle) switch
        {
            (TimeSegment.Hour, HourCycle.H23) => (0, 23),
            (TimeSegment.Hour, _) => (1, 12),
            (TimeSegment.Minute or TimeSegment.Second, _) => (0, 59),
            _ => (0, 1),
        };

        /// <summary>A segment's displayed value: the hour on the clock, the minute or second, 0 (AM) or 1 (PM).</summary>
        public readonly int Value(TimeSegment segment) => (segment, HourCycle) switch
        {
            (TimeSegment.Hour, HourCycle.H23) => Time.Hour,
            (TimeSegment.Hour, _) => (Time.Hour + 11) % 12 + 1,
            (TimeSegment.Minute, _) => Time.Minute,
            (TimeSegment.Second, _) => Time.Second,
            _ => Time.Hour / 12,
        };

        public readonly string Label(TimeSegment segment)
        {
            if (segment != TimeSegment.Period)
            {
                return Value(segment).ToString("00", CultureInfo.InvariantCulture);
            }
            var format = CultureInfo.CurrentCulture.DateTimeFormat;
            var text = Value(segment) == 0 ? format.AMDesignator : format.PMDesignator;
            return string.IsNullOrEmpty(text) ? (Value(segment) == 0 ? "AM" : "PM") : text;
        }

        private readonly TimeOnly WithValue(TimeSegment segment, int value)
        {
            var pmOffset = Time.Hour / 12 * 12;
            var (h, m, s) = (Time.Hour, Time.Minute, Time.Second);
            switch (segment, HourCycle)
            {
                case (TimeSegment.Hour, HourCycle.H23):
                    h = value;
                    break;
                // 12 AM is midnight and 12 PM is noon.
                case (TimeSegment.Hour, _):
                    h = value % 12 + pmOffset;
                    break;
                case (TimeSegment.Minute, _):
                    m = value;
                    break;
                case (TimeSegment.Second, _):
                    s = value;
                    break;
                default:
                    h = Time.Hour % 12 + value * 12;
                    break;
            }
            return h is >= 0 and < 24 && m is >= 0 and < 60 && s is >= 0 and < 60 ? new TimeOnly(h, m, s) : Time;
        }

        private static TimeOnly Truncate(TimeOnly time, TimePrecision precision) =>
            new(time.Hour, time.Minute, precision == TimePrecision.Second ? time.Second : 0);

        public void SetFormat(TimePrecision precision, HourCycle hourCycle)
        {
            Precision = precision;
            HourCycle = hourCycle;
            Time = Truncate(Time, precision);
            ResetSegment();
        }

        private void ResetSegment()
        {
            if (Array.IndexOf(Segments, Segment) < 0)
            {
                Segment = TimeSegment.Hour;
            }
            PendingDigit = null;
        }

        public bool SetTime(TimeOnly time)
        {
            time = Truncate(time, Precision);
            if (Time == time)
            {
                // Keep a half-typed segment when the owner echoes the same value back.
                return false;
            }
            Time = time;
            PendingDigit = null;
            return true;
        }

        public bool SelectSegment(TimeSegment segment)
        {
            if (Array.IndexOf(Segments, segment) < 0)
            {
                return false;
            }
            Segment = segment;
            PendingDigit = null;
            return true;
        }

        /// <summary>Moves the selected segment by <paramref name="offset"/>; false at either end.</summary>
        public bool MoveSegment(int offset)
        {
            var segments = Segments;
            var index = Math.Max(0, Array.IndexOf(segments, Segment)) + offset;
            return index >= 0 && index < segments.Length && SelectSegment(segments[index]);
        }

        /// <summary>Steps the selected segment by <paramref name="delta"/>, wrapping within it.</summary>
        public bool Step(int delta)
        {
            PendingDigit = null;
            var (min, max) = Bounds(Segment);
            var span = max - min + 1;
            var value = ((Value(Segment) - min + delta) % span + span) % span;
            return ReplaceSegment(value + min);
        }

        public bool InputDigit(int digit)
        {
            if (Segment == TimeSegment.Period)
            {
                return false;
            }
            var (min, max) = Bounds(Segment);
            int value;
            bool complete;
            if (PendingDigit is { } first && first * 10 + digit >= min && first * 10 + digit <= max)
            {
                (value, complete) = (first * 10 + digit, true);
            }
            else
            {
                // The two digits cannot form a valid value, so the new digit starts over.
                (value, complete) = (digit, digit * 10 > max);
            }
            PendingDigit = complete ? null : digit;
            var changed = ReplaceSegment(value);
            if (complete)
            {
                MoveSegment(1);
            }
            return changed;
        }

        /// <summary>Types a or p into the period segment.</summary>
        public bool InputPeriod(bool pm) => Segment == TimeSegment.Period && ReplaceSegment(pm ? 1 : 0);

        /// <summary>Resets the selected segment to its first value: zero, or 12 / AM on a 12-hour clock.</summary>
        public bool ClearSegment()
        {
            PendingDigit = null;
            return ReplaceSegment(0);
        }

        private bool ReplaceSegment(int value)
        {
            var time = WithValue(Segment, value);
            var changed = time != Time;
            Time = time;
            return changed;
        }
    }
}
