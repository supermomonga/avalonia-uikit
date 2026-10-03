using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Builds the Avalonia control for a golden case: the same parameters the
/// GPUI reference harness used (reference/src/cases/*.rs), mapped to the
/// theme's style classes and the control's own properties.
/// </summary>
public static class Adapters
{
    public static Control Create(GoldenCase c) => c.Component switch
    {
        "surface" => new Border { Width = 1, Height = 1 },
        "button" => Button(c),
        "toggle" => Toggle(c),
        "checkbox" => Check(new CheckBox(), c),
        "radio" => Check(new RadioButton(), c),
        "switch" => Check(new ToggleSwitch(), c),
        _ => throw new NotSupportedException($"no adapter for {c.Component}"),
    };

    /// <summary>A Lucide icon from the theme, as an app would put one in content.</summary>
    public static PathIcon Icon(string name)
    {
        var key = "Gpui.Icon." + string.Concat(name.Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
        return new PathIcon { Data = (Avalonia.Media.Geometry)Avalonia.Application.Current!.FindResource(key)! };
    }

    /// <summary>Adds the case's value of <paramref name="key"/> as a class unless it is the default.</summary>
    public static void ClassFrom(Control control, GoldenCase c, string key, string defaultValue)
    {
        var value = c.Str(key, defaultValue);
        if (value != defaultValue)
        {
            control.Classes.Add(value);
        }
    }

    public static void FlagClass(Control control, GoldenCase c, string key, string? className = null)
    {
        if (c.Bool(key))
        {
            control.Classes.Add(className ?? key);
        }
    }

    private static ToggleButton Check(ToggleButton control, GoldenCase c)
    {
        if (c.Has("label"))
        {
            control.Content = c.Str("label");
        }
        control.IsChecked = c.Bool("checked");
        control.IsEnabled = !c.Bool("disabled");
        ClassFrom(control, c, "size", "medium");
        return control;
    }

    private static ToggleButton Toggle(GoldenCase c)
    {
        var toggle = new ToggleButton
        {
            Content = c.Has("label") ? c.Str("label") : Icon(c.Str("icon")),
            IsChecked = c.Bool("checked"),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(toggle, c, "variant", "ghost");
        ClassFrom(toggle, c, "size", "medium");
        return toggle;
    }

    private static Button Button(GoldenCase c)
    {
        var button = new Button();
        if (c.Has("label"))
        {
            button.Content = c.Str("label");
        }
        else if (c.Has("icon"))
        {
            button.Content = Icon(c.Str("icon"));
            button.Classes.Add("icon-only");
        }
        ClassFrom(button, c, "variant", "default");
        ClassFrom(button, c, "size", "medium");
        FlagClass(button, c, "outline");
        FlagClass(button, c, "compact");
        FlagClass(button, c, "selected");
        var rounded = c.Str("rounded", "medium");
        if (rounded != "medium")
        {
            button.Classes.Add($"rounded-{rounded}");
        }
        button.IsEnabled = !c.Bool("disabled");
        return button;
    }
}
