using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// uikit:Inputs, NumberInput, InputGroup and RangeSlider for the uikit-input,
/// uikit-number, uikit-inputgroup and uikit-slider cases
/// (reference/src/cases/input.rs, number.rs, slider.rs).
/// </summary>
public static partial class Adapters
{
    /// <summary>
    /// A TextBox with the uikit:Inputs rules of the case: `mask` ("number" with
    /// `separator` and `fraction`, or a pattern), `digits` (GPUI's validate
    /// closure; here the same rule as a Pattern), `clean_on_escape`; or, with
    /// `textarea`, a multi-line TextBox of `rows` lines indenting by 2, its
    /// lines selected when `select_lines`.
    /// </summary>
    public static TextBox InputRulesCase(GoldenCase c)
    {
        if (c.Bool("textarea"))
        {
            var area = new TextBox
            {
                AcceptsReturn = true,
                Text = c.Str("value", string.Empty),
                Width = c.Num("width", 220),
            };
            area.MinLines = area.MaxLines = (int)c.Num("rows", 3);
            // TextareaState's default TabSize.
            Inputs.SetTabSize(area, 2);
            if (c.Bool("select_lines"))
            {
                area.SelectAll();
            }
            return area;
        }
        var box = new TextBox
        {
            Text = c.Has("value") ? c.Str("value") : null,
            PlaceholderText = c.Has("placeholder") ? c.Str("placeholder") : null,
            Width = c.Num("width", 200),
        };
        if (c.Has("mask"))
        {
            Inputs.SetMaskPattern(box, c.Str("mask") == "number" ? NumberMaskOf(c) : MaskPattern.Parse(c.Str("mask")));
        }
        if (c.Bool("digits"))
        {
            Inputs.SetPattern(box, "^[0-9]*$");
        }
        Inputs.SetCleanOnEscape(box, c.Bool("clean_on_escape"));
        KeepCaretOnTabFocus(box);
        return box;
    }

    /// <summary>uikit:NumberInput as GPUI's NumberInput: number.toml's value and size, and the state's step, min, max and number mask.</summary>
    public static NumberInput NumberInputCase(GoldenCase c)
    {
        var number = new NumberInput
        {
            Value = decimal.Parse(c.Str("value", "42"), CultureInfo.InvariantCulture),
            Width = c.Num("width", 160),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(number, c, "size", "medium");
        if (c.Has("step"))
        {
            number.Increment = (decimal)c.Num("step", 1);
        }
        if (c.Has("min"))
        {
            number.Minimum = (decimal)c.Num("min", 0);
        }
        if (c.Has("max"))
        {
            number.Maximum = (decimal)c.Num("max", 0);
        }
        if (c.Has("separator"))
        {
            Inputs.SetMaskPattern(number, NumberMaskOf(c));
        }
        // NumericUpDown selects its text once the field has focus (after the field's own handlers).
        number.GotFocus += (_, e) =>
        {
            if (e.NavigationMethod == NavigationMethod.Tab && e.Source is TextBox box)
            {
                CollapseToStart(box);
            }
        };
        return number;
    }

    /// <summary>
    /// uikit:InputGroup as GPUI's InputGroup: input-group.toml's addons (`start`
    /// an icon or text, `end` text, `end_button`, `end_icon`), and rows above
    /// (`block_start`) and below (`block_end` text, `block_button` at the end).
    /// </summary>
    public static InputGroup InputGroupCase(GoldenCase c)
    {
        var group = new InputGroup
        {
            Width = c.Num("width", 240),
            IsEnabled = !c.Bool("disabled"),
            IsReadOnly = c.Bool("readonly"),
            IsInvalid = c.Bool("invalid"),
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        ClassFrom(group, c, "size", "medium");
        var box = new TextBox
        {
            Text = c.Has("value") ? c.Str("value") : null,
            PlaceholderText = c.Has("placeholder") ? c.Str("placeholder") : null,
        };
        if (c.Bool("multiline"))
        {
            box.AcceptsReturn = true;
            box.MinLines = box.MaxLines = (int)c.Num("rows", 3);
        }
        if (c.Has("block_start"))
        {
            group.Items.Add(Addon(InputGroupAddonAlignment.BlockStart, c.Str("block_start")));
        }
        if (c.Has("start"))
        {
            var start = c.Str("start");
            group.Items.Add(Addon(InputGroupAddonAlignment.InlineStart, start == "search" ? Icon(start) : start));
        }
        group.Items.Add(box);
        var tail = new List<object>();
        if (c.Has("end"))
        {
            tail.Add(c.Str("end"));
        }
        if (c.Has("end_button"))
        {
            tail.Add(new Button { Content = c.Str("end_button") });
        }
        if (c.Has("end_icon"))
        {
            var button = new Button { Content = Icon(c.Str("end_icon")), Classes = { "icon-only" } };
            if (c.Str("button_size", "xsmall") == "small")
            {
                button.Classes.Add("small");
            }
            tail.Add(button);
        }
        if (tail.Count > 0)
        {
            group.Items.Add(Addon(InputGroupAddonAlignment.InlineEnd, [.. tail]));
        }
        var below = new List<object>();
        if (c.Has("block_end"))
        {
            below.Add(c.Str("block_end"));
        }
        if (c.Has("block_button"))
        {
            below.Add(new Button { Content = c.Str("block_button"), HorizontalAlignment = HorizontalAlignment.Right });
        }
        if (below.Count > 0)
        {
            group.Items.Add(Addon(InputGroupAddonAlignment.BlockEnd, [.. below]));
        }
        return group;
    }

    /// <summary>uikit:RangeSlider: the range `start`..`end`, or slider.toml's single `value`; `scale = "log"`.</summary>
    public static RangeSlider RangeSliderCase(GoldenCase c)
    {
        var slider = new RangeSlider
        {
            Minimum = c.Num("min", 0),
            Maximum = c.Num("max", 100),
            IsRange = c.Has("start"),
            IsEnabled = !c.Bool("disabled"),
        };
        if (c.Str("scale", "linear") == "log")
        {
            slider.Scale = SliderScale.Logarithmic;
        }
        if (slider.IsRange)
        {
            slider.StartValue = c.Num("start", 0);
            slider.EndValue = c.Num("end", 0);
        }
        else
        {
            slider.EndValue = c.Num("value", 40);
        }
        if (c.Bool("vertical"))
        {
            slider.Orientation = Orientation.Vertical;
        }
        else
        {
            slider.Width = c.Num("width", 200);
        }
        FlagClass(slider, c, "reverse");
        return slider;
    }

    private static NumberMask NumberMaskOf(GoldenCase c) => new()
    {
        Separator = c.Has("separator") ? c.Str("separator")[0] : null,
        Fraction = c.Has("fraction") ? (int)c.Num("fraction", 0) : null,
    };

    private static InputGroupAddon Addon(InputGroupAddonAlignment alignment, params object[] items)
    {
        var addon = new InputGroupAddon { Alignment = alignment };
        foreach (var item in items)
        {
            addon.Items.Add(item);
        }
        return addon;
    }

    /// <summary>
    /// R24: Tab selects all of a single-line TextBox's text. GPUI selects
    /// nothing and leaves the caret where it was: at the start of a default
    /// value (state.rs default_value), where the case types.
    /// </summary>
    private static void KeepCaretOnTabFocus(TextBox box) =>
        box.GotFocus += (_, e) =>
        {
            if (e.NavigationMethod == NavigationMethod.Tab)
            {
                CollapseToStart(box);
            }
        };

    private static void CollapseToStart(TextBox box)
    {
        box.SetCurrentValue(TextBox.SelectionEndProperty, 0);
        box.SetCurrentValue(TextBox.SelectionStartProperty, 0);
        box.SetCurrentValue(TextBox.CaretIndexProperty, 0);
    }
}
