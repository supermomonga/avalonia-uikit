using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
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
        "dropdown" => new Button
        {
            Content = c.Str("label", "Open"),
            Flyout = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft, ItemsSource = StandardMenu() },
        },
        "context" => ContextArea(),
        "split" => Split(c),
        "menubar" => MenuBar(),
        "number" => Number(c),
        "groupbox" => GroupBox(c),
        "separator" => Separator(c),
        "link" => new HyperlinkButton { Content = c.Str("label", "Documentation") },
        _ => throw new NotSupportedException($"no adapter for {c.Component}"),
    };

    /// <summary>
    /// Undoes behavior of a control's own logic that the theme does not own and
    /// GPUI does not have (R24): NumericUpDown selects its text when a press on a
    /// step button focuses it. The caret is hidden: its blink phase at capture
    /// time is not part of the look (R25).
    /// </summary>
    public static void AfterDrive(GoldenCase c, CaseHost host)
    {
        foreach (var box in host.Window.GetVisualDescendants().OfType<TextBox>())
        {
            box.ClearSelection();
            box.CaretBrush = Avalonia.Media.Brushes.Transparent;
        }
        host.Flush();
    }

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

    /// <summary>The menu every popup case shows (reference/src/cases/menu.rs standard_menu).</summary>
    public static List<Control> StandardMenu() =>
    [
        new MenuItem { Header = "New File" },
        new MenuItem { Header = "Refresh", InputGesture = new KeyGesture(Key.F5) },
        new MenuItem { Header = "Word Wrap", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
        new Separator(),
        new MenuItem { Header = "Rename", IsEnabled = false },
        new MenuItem { Header = "Edit", ItemsSource = new List<Control> { new MenuItem { Header = "Copy" }, new MenuItem { Header = "Paste" } } },
    ];

    private static GroupBox GroupBox(GoldenCase c)
    {
        var group = new GroupBox { Content = c.Str("content", "Content"), Width = c.Num("width", 240) };
        if (c.Has("title"))
        {
            group.Header = c.Str("title");
        }
        ClassFrom(group, c, "variant", "normal");
        return group;
    }

    private static Separator Separator(GoldenCase c)
    {
        var separator = new Separator();
        var length = c.Num("length", 160);
        if (c.Bool("vertical"))
        {
            separator.Classes.Add("vertical");
            separator.Height = length;
        }
        else
        {
            separator.Width = length;
        }
        FlagClass(separator, c, "dashed");
        return separator;
    }

    private static NumericUpDown Number(GoldenCase c)
    {
        var number = new NumericUpDown
        {
            Value = decimal.Parse(c.Str("value", "42"), System.Globalization.CultureInfo.InvariantCulture),
            Width = c.Num("width", 160),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(number, c, "size", "medium");
        return number;
    }

    private static Border ContextArea()
    {
        var area = new Border
        {
            Width = 120,
            Height = 48,
            BorderThickness = new Avalonia.Thickness(1),
            Background = Avalonia.Media.Brushes.Transparent,
            ContextMenu = new ContextMenu { ItemsSource = StandardMenu() },
        };
        area.Bind(Border.BorderBrushProperty, area.GetResourceObservable("Gpui.Border"));
        return area;
    }

    private static SplitButton Split(GoldenCase c)
    {
        var split = new SplitButton
        {
            Content = c.Str("label", "Save"),
            Flyout = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedRight, ItemsSource = StandardMenu() },
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(split, c, "variant", "default");
        ClassFrom(split, c, "size", "medium");
        FlagClass(split, c, "outline");
        FlagClass(split, c, "selected");
        return split;
    }

    private static Menu MenuBar() => new()
    {
        ItemsSource = new List<MenuItem>
        {
            new()
            {
                Header = "File",
                ItemsSource = new List<Control>
                {
                    new MenuItem { Header = "New File" },
                    new MenuItem { Header = "Refresh", InputGesture = new KeyGesture(Key.F5) },
                    new Separator(),
                    new MenuItem { Header = "Quit" },
                },
            },
            new() { Header = "Edit", ItemsSource = new List<MenuItem> { new() { Header = "Undo" }, new() { Header = "Redo" } } },
            new() { Header = "View", ItemsSource = new List<MenuItem> { new() { Header = "Zoom" } } },
        },
    };

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
