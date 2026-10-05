using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What uikit:Inputs, NumberInput, InputGroup and RangeSlider do beyond their
/// look: GPUI Kit's editing rules (state.rs, mask_pattern.rs, indent.rs), the
/// NumberInput's typing and stepping (number_input.rs), the InputGroup's focus
/// and propagation (group.rs) and the range slider's pointer rules (slider.rs).
/// </summary>
public class InputsBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    // A 200px single-line field at (16, 12) and a 220px textarea (uikit-input).
    private static readonly GoldenCase Field = Case("uikit-input/validate.base/focus+key-a+key-3+key-b+key-4/light");
    private static readonly GoldenCase Area = Case("uikit-input/inline.base/focus+key-tab/light");

    private static (CaseHost Host, TextBox Box) Open(TextBox box, GoldenCase? golden = null)
    {
        box.Width = 200;
        var host = CaseHost.Open(golden ?? Field, box);
        box.Focus();
        host.Flush();
        return (host, box);
    }

    private static void Type(CaseHost host, string keys) => host.Drive(Field, string.Join('+', keys.Split(' ').Select(k => "key-" + k)));

    private static void CaretAt(TextBox box, int index)
    {
        box.SelectionStart = box.SelectionEnd = index;
        box.CaretIndex = index;
    }

    private static string Command => (Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control) == KeyModifiers.Meta ? "cmd" : "ctrl";

    // MARK: Validation

    // state.rs replace_text_in_range: the whole text the edit leaves must pass.
    [Test]
    public async Task A_pattern_rejects_an_edit_whose_text_it_does_not_match()
    {
        var box = new TextBox { Text = "12" };
        Inputs.SetPattern(box, "^[0-9]*$");
        var (host, _) = Open(box);
        using var _h = host;
        CaretAt(box, 2);
        Type(host, "a 3 b 4");
        await Assert.That(box.Text).IsEqualTo("1234");
        await Assert.That(box.CaretIndex).IsEqualTo(4);
    }

    [Test]
    public async Task A_predicate_rejects_an_edit_by_the_whole_text()
    {
        var box = new TextBox();
        Inputs.SetValidate(box, s => s.Length <= 3);
        var (host, _) = Open(box);
        using var _h = host;
        Type(host, "a b c d");
        await Assert.That(box.Text).IsEqualTo("abc");
    }

    // Only a valid text is kept from going invalid: a text the app set stays editable.
    [Test]
    public async Task A_text_that_does_not_pass_stays_editable()
    {
        var box = new TextBox { Text = "ab" };
        Inputs.SetPattern(box, "^[0-9]*$");
        var (host, _) = Open(box);
        using var _h = host;
        CaretAt(box, 2);
        Type(host, "c");
        await Assert.That(box.Text).IsEqualTo("abc");
        // Deleting back to an empty text (which always passes) is an edit like any other.
        Type(host, "backspace backspace backspace 1");
        await Assert.That(box.Text).IsEqualTo("1");
    }

    [Test]
    public async Task A_deletion_that_would_leave_an_invalid_text_is_undone()
    {
        var box = new TextBox { Text = "-1" };
        Inputs.SetPattern(box, "^-?[0-9]+$");
        var (host, _) = Open(box);
        using var _h = host;
        CaretAt(box, 2);
        Type(host, "backspace");
        await Assert.That(box.Text).IsEqualTo("-1");
        await Assert.That(box.CaretIndex).IsEqualTo(2);
    }

    // A paste is one edit: checked whole, then masked.
    [Test]
    public async Task A_paste_is_checked_as_one_edit()
    {
        var box = new TextBox { Text = "12" };
        Inputs.SetPattern(box, "^[0-9]*$");
        var (host, _) = Open(box);
        using var _h = host;
        var clipboard = TopLevel.GetTopLevel(box)!.Clipboard!;
        CaretAt(box, 2);
        await clipboard.SetTextAsync("3a");
        box.Paste();
        host.Flush();
        await Assert.That(box.Text).IsEqualTo("12");
        await clipboard.SetTextAsync("34");
        box.Paste();
        host.Flush();
        await Assert.That(box.Text).IsEqualTo("1234");
    }

    // MARK: Masks

    [Test]
    public async Task A_number_mask_groups_the_digits_and_keeps_the_caret_from_the_end()
    {
        var box = new TextBox();
        Inputs.SetMaskPattern(box, new NumberMask { Separator = ',' });
        var (host, _) = Open(box);
        using var _h = host;
        Type(host, "1 2 3 4 5 6 7");
        await Assert.That(box.Text).IsEqualTo("1,234,567");
        await Assert.That(box.CaretIndex).IsEqualTo(9);
        // A digit typed after "1," regroups the text; the caret stays before the "2".
        CaretAt(box, 2);
        Type(host, "0");
        await Assert.That(box.Text).IsEqualTo("10,234,567");
        await Assert.That(box.CaretIndex).IsEqualTo(3);
        // Deleting regroups too, the caret at the end.
        CaretAt(box, 10);
        Type(host, "backspace");
        await Assert.That(box.Text).IsEqualTo("1,023,456");
        await Assert.That(box.CaretIndex).IsEqualTo(9);
        await Assert.That(Inputs.UnmaskValue(box)).IsEqualTo("1023456");
    }

    [Test]
    public async Task A_number_mask_limits_the_fraction_and_types_full_width_digits_as_ascii()
    {
        var box = new TextBox();
        Inputs.SetMaskPattern(box, new NumberMask { Separator = ',', Fraction = 2 });
        var (host, _) = Open(box);
        using var _h = host;
        host.Window.KeyTextInput("１２３４．５６７");
        host.Flush();
        await Assert.That(box.Text).IsEqualTo("1,234.56");
        // Another point, or a sign after the first character, is rejected.
        Type(host, ".");
        await Assert.That(box.Text).IsEqualTo("1,234.56");
    }

    [Test]
    public async Task A_pattern_mask_writes_its_separators_and_shows_its_placeholder()
    {
        var box = new TextBox();
        Inputs.SetMaskPattern(box, MaskPattern.Parse("(999)999-9999"));
        var (host, _) = Open(box);
        using var _h = host;
        await Assert.That(box.PlaceholderText).IsEqualTo("(___)___-____");
        Type(host, "1 2 3 4 a");
        await Assert.That(box.Text).IsEqualTo("(123)4");
        Type(host, "5 6 7 8 9 0 1");
        await Assert.That(box.Text).IsEqualTo("(123)456-7890");
        await Assert.That(Inputs.UnmaskValue(box)).IsEqualTo("1234567890");
    }

    // state.rs set_value masks the text too; the mask set later leaves the text as it is.
    [Test]
    public async Task Text_the_app_sets_is_masked()
    {
        var box = new TextBox { Text = "1234.5" };
        Inputs.SetMaskPattern(box, new NumberMask { Separator = ',' });
        var (host, _) = Open(box);
        using var _h = host;
        await Assert.That(box.Text).IsEqualTo("1234.5");
        box.Text = "7654321.50";
        await Assert.That(box.Text).IsEqualTo("7,654,321.50");
        // mask_pattern.rs unmask trims a fraction's trailing zeros.
        await Assert.That(Inputs.UnmaskValue(box)).IsEqualTo("7654321.5");
    }

    // GPUI's mask_pattern.rs tests.
    [Test]
    public async Task Masks_match_gpui_tests()
    {
        var phone = MaskPattern.Parse("(AA)999-999");
        await Assert.That(phone.Mask("AB123456")).IsEqualTo("(AB)123-456");
        await Assert.That(phone.Mask("(AB123456")).IsEqualTo("(AB)123-456");
        await Assert.That(phone.Mask("AB123--")).IsEqualTo("(AB)123-");
        await Assert.That(phone.Unmask("(AB)123-456")).IsEqualTo("AB123456");
        await Assert.That(phone.IsValid("(AB)123456")).IsTrue();
        await Assert.That(phone.IsValid("12AB345")).IsFalse();
        var any = MaskPattern.Parse("999-999-******");
        await Assert.That(any.Mask("123456A(111)")).IsEqualTo("123-456-A(111)");
        var number = new NumberMask { Separator = ',', Fraction = 4 };
        await Assert.That(number.Mask("1234567.891234")).IsEqualTo("1,234,567.8912");
        await Assert.That(number.Mask("-1234567.")).IsEqualTo("-1,234,567.");
        await Assert.That(number.Mask("-.5")).IsEqualTo("-.5");
        await Assert.That(number.Unmask("1,234,567.890")).IsEqualTo("1234567.89");
        await Assert.That(new NumberMask { Separator = ',', Fraction = 0 }.Mask("1234567.12")).IsEqualTo("1,234,567");
        await Assert.That(new NumberMask { Separator = ' ' }.Mask("1234567")).IsEqualTo("1 234 567");
        await Assert.That(new NumberMask().Mask("1234567.89")).IsEqualTo("1234567.89");
        await Assert.That(number.IsValid("-")).IsTrue();
        await Assert.That(number.IsValid("+-1")).IsFalse();
        await Assert.That(number.IsValid("12-34")).IsFalse();
        await Assert.That(number.IsValid("1.2.3")).IsFalse();
        await Assert.That(number.Normalize("＋1．5")).IsEqualTo("+1.5");
        await Assert.That(number.Normalize("−1，234。5")).IsEqualTo("-1,234.5");
    }

    // MARK: Escape

    [Test]
    [Arguments(false, "Hello")]
    [Arguments(true, "")]
    public async Task Escape_clears_the_text_only_when_asked(bool clean, string expected)
    {
        var box = new TextBox { Text = "Hello" };
        Inputs.SetCleanOnEscape(box, clean);
        var (host, _) = Open(box);
        using var _h = host;
        Type(host, "escape");
        await Assert.That(box.Text ?? string.Empty).IsEqualTo(expected);
        if (clean)
        {
            // state.rs clean is an edit: undo brings the text back.
            box.Undo();
            await Assert.That(box.Text).IsEqualTo("Hello");
        }
    }

    // MARK: Indentation

    private static (CaseHost Host, TextBox Box) OpenArea(string text, int tabSize = 2, bool hardTabs = false)
    {
        var box = new TextBox { AcceptsReturn = true, Text = text, MinLines = 4, MaxLines = 4 };
        Inputs.SetTabSize(box, tabSize);
        Inputs.SetHardTabs(box, hardTabs);
        return Open(box, Area);
    }

    // indent.rs compute_inline_indent: the indent goes in at the caret; Shift+Tab takes it off the line's start.
    [Test]
    public async Task Tab_indents_at_the_caret_and_shift_tab_outdents_its_line()
    {
        var (host, box) = OpenArea("One\nTwo");
        using var _h = host;
        CaretAt(box, 5);
        Type(host, "tab");
        await Assert.That(box.Text).IsEqualTo("One\nT  wo");
        await Assert.That(box.CaretIndex).IsEqualTo(7);
        Type(host, "shift-tab");
        // The line does not start with the indent: nothing to take off.
        await Assert.That(box.Text).IsEqualTo("One\nT  wo");
        CaretAt(box, 0);
        Type(host, "tab shift-tab");
        await Assert.That(box.Text).IsEqualTo("One\nT  wo");
        await Assert.That(box.CaretIndex).IsEqualTo(0);
    }

    // compute_block_indent: every selected line, and the selection moves with its lines.
    [Test]
    public async Task Tab_indents_the_selected_lines()
    {
        var (host, box) = OpenArea("One\nTwo\nThree");
        using var _h = host;
        box.SelectionStart = 1;
        box.SelectionEnd = 5;
        Type(host, "tab");
        await Assert.That(box.Text).IsEqualTo("  One\n  Two\nThree");
        await Assert.That((box.SelectionStart, box.SelectionEnd)).IsEqualTo((3, 9));
        Type(host, "shift-tab");
        await Assert.That(box.Text).IsEqualTo("One\nTwo\nThree");
        await Assert.That((box.SelectionStart, box.SelectionEnd)).IsEqualTo((1, 5));
    }

    // Cmd+] / Cmd+[ (Ctrl+] / Ctrl+[ off macOS) indent and outdent the caret's line.
    [Test]
    public async Task The_bracket_keys_indent_and_outdent_the_caret_line()
    {
        var (host, box) = OpenArea("One\nTwo");
        using var _h = host;
        CaretAt(box, 6);
        Type(host, $"{Command}-]");
        await Assert.That(box.Text).IsEqualTo("One\n  Two");
        await Assert.That(box.CaretIndex).IsEqualTo(8);
        Type(host, $"{Command}-] {Command}-[ {Command}-[");
        await Assert.That(box.Text).IsEqualTo("One\nTwo");
        await Assert.That(box.CaretIndex).IsEqualTo(6);
    }

    [Test]
    public async Task Hard_tabs_indent_with_a_tab_character()
    {
        var (host, box) = OpenArea("One", tabSize: 4, hardTabs: true);
        using var _h = host;
        CaretAt(box, 0);
        Type(host, $"{Command}-]");
        await Assert.That(box.Text).IsEqualTo("\tOne");
    }

    // Tab stays a focus key without a TabSize, on a read-only textarea, and on a single line.
    [Test]
    [Arguments(0, false)]
    [Arguments(2, true)]
    public async Task Tab_moves_the_focus_when_the_textarea_does_not_indent(int tabSize, bool readOnly)
    {
        var box = new TextBox { AcceptsReturn = true, Text = "One", IsReadOnly = readOnly };
        Inputs.SetTabSize(box, tabSize);
        var next = new TextBox { Text = "Next" };
        var panel = new StackPanel { Width = 200, Children = { box, next } };
        using var host = CaseHost.Open(Area, panel);
        box.Focus();
        host.Flush();
        Type(host, "tab");
        await Assert.That(box.Text).IsEqualTo("One");
        await Assert.That(next.IsFocused).IsTrue();
    }

    // MARK: NumberInput

    private static readonly GoldenCase Number = Case("uikit-number/typing.base/focus+key-5/light");

    private static (CaseHost Host, NumberInput Input) OpenNumber(decimal? value, Action<NumberInput>? configure = null)
    {
        var input = new NumberInput { Value = value, Width = 160 };
        configure?.Invoke(input);
        var host = CaseHost.Open(Number, input);
        host.Part<TextBox>("PART_TextBox").Focus();
        host.Flush();
        return (host, input);
    }

    private static TextBox NumberField(CaseHost host) => host.Part<TextBox>("PART_TextBox");

    // number_input.rs ensure_number_mask: the text stays a number; full-width digits are typed as ASCII.
    [Test]
    public async Task A_number_input_keeps_its_text_a_number()
    {
        var (host, _) = OpenNumber(42);
        using var _h = host;
        var box = NumberField(host);
        CaretAt(box, 2);
        Type(host, "a");
        await Assert.That(box.Text).IsEqualTo("42");
        host.Window.KeyTextInput("５");
        host.Flush();
        await Assert.That(box.Text).IsEqualTo("425");
        Type(host, ".");
        await Assert.That(box.Text).IsEqualTo("425.");
    }

    // gpui-base number_input.rs test_step_value.
    [Test]
    [Arguments("", StepAction.Increment, "1", null, null, "1")]
    [Arguments("", StepAction.Decrement, "1", null, null, "-1")]
    [Arguments("-", StepAction.Increment, "1", null, null, "1")]
    [Arguments("-2", StepAction.Increment, "1", null, null, "-1")]
    [Arguments("0.1", StepAction.Increment, "0.2", null, null, "0.3")]
    [Arguments("0.3", StepAction.Decrement, "0.1", null, null, "0.2")]
    [Arguments("1.25", StepAction.Increment, "1", null, null, "2.25")]
    [Arguments("", StepAction.Increment, "1", "10", null, "10")]
    [Arguments("", StepAction.Decrement, "1", "10", null, "10")]
    [Arguments("99.5", StepAction.Increment, "1", null, "100", "100.0")]
    [Arguments("1000", StepAction.Decrement, "1", null, "100", "100")]
    [Arguments("1", StepAction.Decrement, "1", "0.25", null, "0.25")]
    [Arguments("10", StepAction.Decrement, "1", "10", null, null)]
    [Arguments("100", StepAction.Increment, "1", null, "100", null)]
    [Arguments("5", StepAction.Decrement, "1", "10", null, null)]
    [Arguments("1000", StepAction.Increment, "1", null, "100", null)]
    public async Task Steps_match_gpui_step_value(string value, StepAction action, string step, string? min, string? max, string? expected)
    {
        static decimal D(string s) => decimal.Parse(s, System.Globalization.CultureInfo.InvariantCulture);
        var result = NumberInput.StepValue(value, action, D(step), min is null ? decimal.MinValue : D(min), max is null ? decimal.MaxValue : D(max));
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task Up_and_down_step_the_typed_value_with_its_precision()
    {
        var (host, input) = OpenNumber(0.1m, n => n.Increment = 0.2m);
        using var _h = host;
        Type(host, "up");
        await Assert.That(NumberField(host).Text).IsEqualTo("0.3");
        Type(host, "down down");
        await Assert.That(input.Value).IsEqualTo(-0.1m);
    }

    // number_input_story: at 1.0 the step is 0.1 going down and 0.5 going up.
    [Test]
    public async Task A_step_can_depend_on_the_value_and_the_direction()
    {
        var (host, input) = OpenNumber(0.9m, n => n.StepBy = (value, action) => action == StepAction.Increment
            ? (value < 1 ? 0.1m : 0.5m)
            : (value <= 1 ? 0.1m : 0.5m));
        using var _h = host;
        Type(host, "up");
        await Assert.That(input.Value).IsEqualTo(1.0m);
        Type(host, "up");
        await Assert.That(input.Value).IsEqualTo(1.5m);
        Type(host, "down down");
        await Assert.That(input.Value).IsEqualTo(0.9m);
    }

    // set_step(None): a step raises Step and the app sets the value.
    [Test]
    public async Task Without_stepping_a_step_only_asks_the_app()
    {
        var (host, input) = OpenNumber(1, n => n.StepsValue = false);
        using var _h = host;
        var steps = new List<StepAction>();
        input.Step += (_, e) => steps.Add(e.Action);
        Type(host, "up down");
        await Assert.That(string.Join(",", steps)).IsEqualTo("Increment,Decrement");
        await Assert.That(input.Value).IsEqualTo(1m);
        // The buttons too: the + button is the frame's last 32px.
        var plus = host.Part<RepeatButton>("PART_IncreaseButton");
        var at = plus.TranslatePoint(new Point(16, 10), host.Window)!.Value;
        host.Drive(Number, $"click-at-{at.X:0.##}-{at.Y:0.##}");
        await Assert.That(steps.Count).IsEqualTo(3);
        await Assert.That(steps[^1]).IsEqualTo(StepAction.Increment);
    }

    // apply_number_step: a stepped value the rules reject falls back to Step.
    [Test]
    public async Task A_step_the_rules_reject_raises_step()
    {
        var (host, input) = OpenNumber(0, n => Inputs.SetPattern(n, "^[0-9]+$"));
        using var _h = host;
        var steps = 0;
        input.Step += (_, _) => steps++;
        Type(host, "down");
        await Assert.That(input.Value).IsEqualTo(0m);
        await Assert.That(steps).IsEqualTo(1);
        Type(host, "up");
        await Assert.That(input.Value).IsEqualTo(1m);
        await Assert.That(steps).IsEqualTo(1);
    }

    // MARK: InputGroup

    private static readonly GoldenCase Group = Case("uikit-inputgroup/addonclick.base/click/light");

    private static (CaseHost Host, InputGroup Group, TextBox Box, Button Button) OpenGroup(Action<InputGroup, TextBox>? configure = null)
    {
        var box = new TextBox { PlaceholderText = "Search..." };
        var button = new Button { Content = "Go" };
        var group = new InputGroup
        {
            Width = 240,
            HorizontalAlignment = HorizontalAlignment.Left,
            Items =
            {
                new InputGroupAddon { Items = { Adapters.Icon("search") } },
                box,
                new InputGroupAddon { Alignment = InputGroupAddonAlignment.InlineEnd, Items = { button } },
            },
        };
        configure?.Invoke(group, box);
        return (CaseHost.Open(Group, group), group, box, button);
    }

    // group.rs: a press on an addon focuses the input; a button in an addon keeps its press.
    [Test]
    public async Task A_press_on_an_addon_focuses_the_input()
    {
        var (host, group, box, button) = OpenGroup();
        using var _h = host;
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        host.Drive(Group, "click-at-26-28");
        await Assert.That(box.IsFocused).IsTrue();
        await Assert.That(group.Classes.Contains(":input-focused")).IsTrue();
        var at = button.TranslatePoint(new Point(8, 8), host.Window)!.Value;
        host.Drive(Group, $"click-at-{at.X:0.##}-{at.Y:0.##}");
        await Assert.That(clicks).IsEqualTo(1);
        await Assert.That(box.IsFocused).IsFalse();
        await Assert.That(group.Classes.Contains(":input-focused")).IsFalse();
    }

    // group.rs: a disabled input disables the group and its buttons.
    [Test]
    public async Task A_disabled_input_disables_the_group()
    {
        var (host, group, _, button) = OpenGroup((_, box) => box.IsEnabled = false);
        using var _h = host;
        await Assert.That(group.Classes.Contains(":input-disabled")).IsTrue();
        await Assert.That(button.IsEffectivelyEnabled).IsFalse();
        await Assert.That(group.Opacity).IsEqualTo(0.5);
    }

    [Test]
    public async Task A_read_only_group_makes_its_input_read_only()
    {
        var (host, _, box, button) = OpenGroup((g, _) => g.IsReadOnly = true);
        using var _h = host;
        await Assert.That(box.IsReadOnly).IsTrue();
        await Assert.That(button.IsEffectivelyEnabled).IsTrue();
    }

    [Test]
    public async Task An_input_validation_error_shows_the_group_invalid()
    {
        var (host, group, box, _) = OpenGroup();
        using var _h = host;
        await Assert.That(group.Classes.Contains(":invalid")).IsFalse();
        DataValidationErrors.SetError(box, new InvalidOperationException("invalid"));
        await Assert.That(group.Classes.Contains(":invalid")).IsTrue();
        DataValidationErrors.ClearErrors(box);
        group.IsInvalid = true;
        await Assert.That(group.Classes.Contains(":invalid")).IsTrue();
    }

    // MARK: RangeSlider

    // A 200px track from x 16, centered at y 20 (uikit-slider).
    private static readonly GoldenCase Slider = Case("uikit-slider/range.base/normal/light");

    private static (CaseHost Host, RangeSlider Slider) OpenSlider(Action<RangeSlider>? configure = null)
    {
        var slider = new RangeSlider { Width = 200, StartValue = 20, EndValue = 60 };
        configure?.Invoke(slider);
        return (CaseHost.Open(Slider, slider), slider);
    }

    // slider.rs SliderTrack: the nearer thumb takes the press; a range does not follow a drag from the track.
    [Test]
    public async Task A_press_on_the_track_moves_the_nearer_thumb()
    {
        var (host, slider) = OpenSlider();
        using var _h = host;
        host.Drive(Slider, "pressed-at-76-20");
        await Assert.That((slider.StartValue, slider.EndValue)).IsEqualTo((30d, 60d));
        host.Drive(Slider, "drag-at-96-20+release");
        await Assert.That(slider.StartValue).IsEqualTo(30d);
        host.Drive(Slider, "pressed-at-186-20+release");
        await Assert.That(slider.EndValue).IsEqualTo(85d);
    }

    [Test]
    public async Task A_single_value_follows_a_drag_from_the_track()
    {
        var (host, slider) = OpenSlider(s => s.IsRange = false);
        using var _h = host;
        host.Drive(Slider, "pressed-at-76-20+drag-at-126-20+release");
        await Assert.That(slider.EndValue).IsEqualTo(55d);
    }

    // update_value_by_position: a thumb stops at the other; the value is rounded, the thumb follows the pointer.
    [Test]
    public async Task A_dragged_thumb_stops_at_the_other_and_rounds_its_value()
    {
        var (host, slider) = OpenSlider(s => s.Step = 10);
        using var _h = host;
        host.Drive(Slider, "pressed-at-56-20+drag-at-75-20");
        await Assert.That(slider.StartValue).IsEqualTo(30d);
        await Assert.That(slider.StartRatio).IsEqualTo(59 / 200d);
        host.Drive(Slider, "drag-at-200-20+release");
        await Assert.That(slider.StartValue).IsEqualTo(60d);
        await Assert.That(slider.StartRatio).IsEqualTo(0.6);
    }

    // slider.rs SliderEvent: Change while the value moves, Release once after a press or a drag.
    [Test]
    public async Task Changes_are_raised_while_moving_and_release_once()
    {
        var (host, slider) = OpenSlider();
        using var _h = host;
        var changes = new List<double>();
        var releases = new List<(double, double)>();
        slider.Changed += (_, e) => changes.Add(e.EndValue);
        slider.Released += (_, e) => releases.Add((e.StartValue, e.EndValue));
        // A press on a thumb without moving changes nothing.
        host.Drive(Slider, "pressed-at-136-20+release");
        await Assert.That(changes.Count).IsEqualTo(0);
        await Assert.That(releases.Count).IsEqualTo(0);
        host.Drive(Slider, "pressed-at-136-20+drag-at-146-20+drag-at-156-20+release");
        await Assert.That(string.Join(",", changes)).IsEqualTo("65,70");
        await Assert.That(releases.Count).IsEqualTo(1);
        await Assert.That(releases[0]).IsEqualTo((20d, 70d));
    }

    [Test]
    public async Task A_disabled_slider_ignores_the_pointer()
    {
        var (host, slider) = OpenSlider(s => s.IsEnabled = false);
        using var _h = host;
        host.Drive(Slider, "pressed-at-186-20+release");
        await Assert.That(slider.EndValue).IsEqualTo(60d);
    }

    // base/slider.rs: logarithmic, a third of 1..1000 is 10, two thirds 100.
    [Test]
    public async Task A_logarithmic_scale_maps_ratios_to_powers()
    {
        var slider = new RangeSlider { Minimum = 1, Maximum = 1000, Scale = SliderScale.Logarithmic };
        await Assert.That(slider.RatioOf(10)).IsEqualTo(1 / 3d).Within(1e-9);
        await Assert.That(slider.ValueAt(2 / 3d)).IsEqualTo(100d).Within(1e-9);
        // A minimum of 0 cannot be logarithmic: the slider stays linear.
        slider.Minimum = 0;
        await Assert.That(slider.ValueAt(0.5)).IsEqualTo(500d);
    }

    [Test]
    public async Task A_vertical_slider_counts_from_the_bottom()
    {
        var golden = Case("uikit-slider/rangevertical.base/normal/light");
        var slider = new RangeSlider { Orientation = Orientation.Vertical, IsRange = false, EndValue = 0 };
        using var host = CaseHost.Open(golden, slider);
        // 120px tall from y 16: 25 % up is y 106.
        host.Drive(golden, "pressed-at-28-106+release");
        await Assert.That(slider.EndValue).IsEqualTo(25d);
    }
}
