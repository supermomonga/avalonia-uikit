using Avalonia.Controls;
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
        _ => throw new NotSupportedException($"no adapter for {c.Component}"),
    };

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

    private static Button Button(GoldenCase c)
    {
        var button = new Button();
        if (c.Has("label"))
        {
            button.Content = c.Str("label");
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
