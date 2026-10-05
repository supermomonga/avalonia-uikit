using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaUIKit;

/// <summary>The direction of a <see cref="NumberInput"/> step (GPUI's StepAction).</summary>
public enum StepAction
{
    /// <summary>The value should go down (the − button, ↓).</summary>
    Decrement,

    /// <summary>The value should go up (the + button, ↑).</summary>
    Increment,
}

/// <summary>
/// GPUI Kit's NumberInput (crates/component/src/input/number_input.rs, gpui-base
/// number_input.rs): a NumericUpDown that keeps the text a number while it is
/// typed and steps the way GPUI does. It has NumericUpDown's look.
/// </summary>
/// <remarks>
/// <para>
/// Typing, pasting and IME input are held to a <see cref="NumberMask"/> (an
/// optional sign, digits and one point; full-width digits and signs become
/// ASCII), or to the mask, pattern and predicate set with
/// <see cref="Inputs"/> on the NumberInput. <c>uikit:Inputs.MaskPattern</c>
/// set to <see cref="MaskPattern.None"/> lifts the restriction.
/// </para>
/// <para>
/// A step (the buttons, ↑ ↓, the wheel) moves the typed value by the
/// <see cref="NumericUpDown.Increment"/>, or by <see cref="StepBy"/> when it is
/// set, keeps the fraction digits of the value and the step ("0.1" + 0.2 is
/// "0.3"), clamps to <see cref="NumericUpDown.Minimum"/> and
/// <see cref="NumericUpDown.Maximum"/>, and does nothing when it cannot move
/// the value that way. With <see cref="StepsValue"/> false a step only raises
/// <see cref="Step"/> and the app sets the value (GPUI's set_step(None)).
/// NumberInput raises <see cref="Step"/> instead of NumericUpDown's Spinned.
/// </para>
/// </remarks>
[TemplatePart("PART_TextBox", typeof(TextBox))]
public class NumberInput : NumericUpDown
{
    /// <summary>
    /// The step for a value and a direction (GPUI's step_by), in place of
    /// <see cref="NumericUpDown.Increment"/>: <c>(v, a) => a == StepAction.Increment ? (v &lt; 1 ? 0.1m : 0.5m) : (v &lt;= 1 ? 0.1m : 0.5m)</c>.
    /// </summary>
    public static readonly StyledProperty<Func<decimal, StepAction, decimal>?> StepByProperty =
        AvaloniaProperty.Register<NumberInput, Func<decimal, StepAction, decimal>?>(nameof(StepBy));

    /// <summary>Whether a step changes the value (true, the default); false only raises <see cref="Step"/>.</summary>
    public static readonly StyledProperty<bool> StepsValueProperty =
        AvaloniaProperty.Register<NumberInput, bool>(nameof(StepsValue), true);

    /// <summary>
    /// Raised for a step the NumberInput leaves to the app (GPUI's
    /// NumberInputEvent::Step): every step when <see cref="StepsValue"/> is
    /// false, and a step whose value the input's rules reject.
    /// </summary>
    public static readonly RoutedEvent<NumberInputStepEventArgs> StepEvent =
        RoutedEvent.Register<NumberInput, NumberInputStepEventArgs>(nameof(Step), RoutingStrategies.Bubble);

    // number_input.rs ensure_number_mask: a number unless the app picks a mask.
    private static readonly NumberMask DefaultMask = new();

    private TextBox? _textBox;

    static NumberInput()
    {
        foreach (var property in new AvaloniaProperty[] { Inputs.PatternProperty, Inputs.ValidateProperty, Inputs.MaskPatternProperty, Inputs.CleanOnEscapeProperty })
        {
            property.Changed.AddClassHandler<NumberInput>((input, _) => input.ForwardRules());
        }
    }

    /// <summary>Creates a NumberInput.</summary>
    public NumberInput()
    {
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc cref="StepByProperty"/>
    public Func<decimal, StepAction, decimal>? StepBy
    {
        get => GetValue(StepByProperty);
        set => SetValue(StepByProperty, value);
    }

    /// <inheritdoc cref="StepsValueProperty"/>
    public bool StepsValue
    {
        get => GetValue(StepsValueProperty);
        set => SetValue(StepsValueProperty, value);
    }

    /// <inheritdoc cref="StepEvent"/>
    public event EventHandler<NumberInputStepEventArgs>? Step
    {
        add => AddHandler(StepEvent, value);
        remove => RemoveHandler(StepEvent, value);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(NumericUpDown);

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        ForwardRules();
    }

    /// <inheritdoc />
    protected override void OnSpin(SpinEventArgs e) =>
        StepOnce(e.Direction == SpinDirection.Increase ? StepAction.Increment : StepAction.Decrement);

    // gpui-base number_input.rs: ↑ and ↓ are Increment and Decrement, before the text field moves its caret.
    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Up or Key.Down && e.KeyModifiers == KeyModifiers.None && AllowSpin && !IsReadOnly && IsEffectivelyEnabled)
        {
            StepOnce(e.Key == Key.Up ? StepAction.Increment : StepAction.Decrement);
            e.Handled = true;
        }
    }

    private void StepOnce(StepAction action)
    {
        if (!StepsValue)
        {
            RaiseEvent(new NumberInputStepEventArgs(StepEvent, action));
            return;
        }
        // gpui-base number_input.rs apply_number_step: step the typed value.
        var text = _textBox is { } box ? Inputs.UnmaskValue(box) : Text ?? string.Empty;
        var current = Parse(text) ?? 0;
        var step = StepBy?.Invoke(current, action) ?? Increment;
        if (StepValue(text, action, step, Minimum, Maximum) is not { } next)
        {
            return;
        }
        if (_textBox is not null && !Inputs.IsValidText(_textBox, next))
        {
            RaiseEvent(new NumberInputStepEventArgs(StepEvent, action));
            return;
        }
        SetCurrentValue(ValueProperty, decimal.Parse(next, NumberStyles.Float, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// gpui-base number_input.rs step_value: the text of <paramref name="value"/>
    /// moved by <paramref name="step"/>, clamped to the range, with the fraction
    /// digits of the value, the step or the bound it was clamped to; null when
    /// the step cannot move the value in its direction.
    /// </summary>
    internal static string? StepValue(string value, StepAction action, decimal step, decimal minimum, decimal maximum)
    {
        static int FractionDigits(string text) => text.IndexOf('.', StringComparison.Ordinal) is var dot and >= 0 ? text.Length - dot - 1 : 0;
        static string Plain(decimal d) => d.ToString("0.############################", CultureInfo.InvariantCulture);

        var current = Parse(value);
        var next = action == StepAction.Increment ? (current ?? 0) + step : (current ?? 0) - step;
        var digits = Math.Max(FractionDigits(value.Trim()), FractionDigits(Plain(step)));
        if (next < minimum)
        {
            next = minimum;
            digits = Math.Max(digits, FractionDigits(Plain(minimum)));
        }
        if (next > maximum)
        {
            next = maximum;
            digits = Math.Max(digits, FractionDigits(Plain(maximum)));
        }
        if (current is { } from && (action == StepAction.Increment ? next <= from : next >= from))
        {
            return null;
        }
        return next.ToString("F" + digits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
    }

    private static decimal? Parse(string text) =>
        decimal.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;

    /// <summary>The Inputs rules set on the NumberInput go to its text field.</summary>
    private void ForwardRules()
    {
        if (_textBox is not { } box)
        {
            return;
        }
        box.SetCurrentValue(Inputs.MaskPatternProperty, GetValue(Inputs.MaskPatternProperty) ?? DefaultMask);
        box.SetCurrentValue(Inputs.PatternProperty, GetValue(Inputs.PatternProperty));
        box.SetCurrentValue(Inputs.ValidateProperty, GetValue(Inputs.ValidateProperty));
        box.SetCurrentValue(Inputs.CleanOnEscapeProperty, GetValue(Inputs.CleanOnEscapeProperty));
    }
}

/// <summary>A step a <see cref="NumberInput"/> leaves to the app.</summary>
public sealed class NumberInputStepEventArgs : RoutedEventArgs
{
    /// <summary>Creates the arguments of a step.</summary>
    public NumberInputStepEventArgs(RoutedEvent routedEvent, StepAction action) : base(routedEvent) => Action = action;

    /// <summary>The direction of the step.</summary>
    public StepAction Action { get; }
}
