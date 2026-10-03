using Avalonia.Controls;
using Avalonia.Controls.Documents;
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
        "buttongroup" => ButtonGroup(c),
        "toggle" => Toggle(c),
        "togglegroup" => ToggleGroup(c),
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
        "progress" => Progress(c),
        "spinner" => Spinner(c),
        "tooltip" => Tooltip(c),
        "scroll" => Scroll(c),
        "icon" => IconCase(c),
        "label" => LabelCase(c),
        "input" => Input(c),
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
        // R28: Avalonia selects a menu's first item when the menu opens; GPUI starts
        // with none. Only the pointer's item (and an open submenu's) stays selected.
        if (!c.State.Contains("key-", StringComparison.Ordinal))
        {
            foreach (var item in host.Window.GetVisualDescendants().OfType<MenuItem>())
            {
                if (item.IsSelected && !item.IsPointerOver && !item.IsSubMenuOpen)
                {
                    item.IsSelected = false;
                }
            }
        }
        foreach (var box in host.Window.GetVisualDescendants().OfType<TextBox>())
        {
            // R24: Tab selects all of a single-line TextBox's text, GPUI selects nothing.
            if (c.Bool("select_all"))
            {
                box.SelectAll();
            }
            else
            {
                box.ClearSelection();
            }
            box.CaretBrush = Avalonia.Media.Brushes.Transparent;
        }
        host.Flush();
    }

    /// <summary>ScrollbarMode::Always / Hover / Scrolling.</summary>
    private static ScrollViewer Scroll(GoldenCase c)
    {
        var mode = c.Str("scrollbar_mode", "hover");
        var viewer = new ScrollViewer
        {
            Width = c.Num("width", 160),
            Height = c.Num("height", 100),
            AllowAutoHide = mode != "always",
            Content = new Border { Width = c.Num("width", 160), Height = c.Num("content", 400) },
        };
        Scrollbars.SetShowOnHover(viewer, mode != "scrolling");
        return viewer;
    }

    /// <summary>A TextBox as GPUI's Input: value or placeholder, prefix icon, suffix button, masking.</summary>
    private static TextBox Input(GoldenCase c)
    {
        var box = new TextBox
        {
            Text = c.Has("value") ? c.Str("value") : null,
            PlaceholderText = c.Has("placeholder") ? c.Str("placeholder") : null,
            Width = c.Num("width", 200),
            IsEnabled = !c.Bool("disabled"),
            IsReadOnly = c.Bool("readonly"),
        };
        ClassFrom(box, c, "size", "medium");
        if (c.Bool("masked"))
        {
            box.PasswordChar = '•';
        }
        if (c.Bool("mask_toggle"))
        {
            box.Classes.Add("revealPasswordButton");
        }
        if (c.Has("prefix"))
        {
            var icon = Icon(c.Str("prefix"));
            icon.Classes.Add("small");
            box.InnerLeftContent = icon;
        }
        if (c.Has("suffix"))
        {
            box.InnerRightContent = new Button { Classes = { "ghost", "xsmall", "icon-only" }, Content = Icon(c.Str("suffix")) };
        }
        return box;
    }

    /// <summary>Text as GPUI's Label: TextBlock.label, SelectableTextBlock.label or a Label.</summary>
    private static Control LabelCase(GoldenCase c)
    {
        TextBlock MakeText(TextBlock text)
        {
            text.Inlines!.Add(new Avalonia.Controls.Documents.Run(c.Str("text", "Label")));
            if (c.Has("secondary"))
            {
                text.Inlines.Add(new Avalonia.Controls.Documents.Run(" " + c.Str("secondary")) { Classes = { "secondary" } });
            }
            return text;
        }
        var control = c.Str("control", "");
        Control result = control switch
        {
            "label" => new Label { Content = MakeText(new TextBlock()) },
            "selectable" => MakeText(new SelectableTextBlock { Classes = { "label" } }),
            _ => MakeText(new TextBlock { Classes = { "label" } }),
        };
        var size = c.Str("text_size", "base") switch { "xs" => 12, "sm" => 14, "lg" => 18, "xl" => 20, "2xl" => 24, _ => 16 };
        result.SetValue(TextElement.FontSizeProperty, (double)size);
        result.SetValue(TextElement.FontWeightProperty, c.Str("weight", "normal") switch
        {
            "medium" => Avalonia.Media.FontWeight.Medium,
            "semibold" => Avalonia.Media.FontWeight.SemiBold,
            "bold" => Avalonia.Media.FontWeight.Bold,
            _ => Avalonia.Media.FontWeight.Normal,
        });
        if (c.Has("width"))
        {
            result.Width = c.Num("width", 0);
            result.SetValue(TextBlock.TextAlignmentProperty, c.Str("align", "left") switch
            {
                "center" => Avalonia.Media.TextAlignment.Center,
                "right" => Avalonia.Media.TextAlignment.Right,
                _ => Avalonia.Media.TextAlignment.Left,
            });
        }
        if (c.Str("leading", "default") == "relaxed")
        {
            result.SetValue(TextBlock.LineHeightProperty, 28.8);
        }
        if (c.Has("color"))
        {
            var key = c.Str("color") == "muted" ? "Gpui.MutedForeground" : "Gpui.Danger";
            result.SetValue(TextElement.ForegroundProperty, (Avalonia.Media.IBrush)Avalonia.Application.Current!.FindResource(c.IsDark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light, key)!);
        }
        return result;
    }

    /// <summary>A PathIcon with the theme's geometry, size class, color and rotation.</summary>
    private static PathIcon IconCase(GoldenCase c)
    {
        var icon = Icon(c.Str("icon"));
        if (c.Has("size"))
        {
            ClassFrom(icon, c, "size", "medium");
        }
        if (c.Has("color"))
        {
            var key = "Gpui." + string.Concat(c.Str("color").Split('-').Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
            icon.Foreground = (Avalonia.Media.IBrush)Avalonia.Application.Current!.FindResource(c.IsDark ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light, key)!;
        }
        if (c.Has("rotate"))
        {
            icon.RenderTransform = new Avalonia.Media.RotateTransform(c.Num("rotate", 0));
        }
        return icon;
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

    private static ProgressBar Progress(GoldenCase c)
    {
        var value = c.Num("value", 40);
        var valueTo = c.Num("value_to", value);
        var progress = new ProgressBar
        {
            Width = c.Num("width", 200),
            Value = value,
            IsIndeterminate = c.Bool("loading"),
        };
        ClassFrom(progress, c, "size", "medium");
        // The case's click switches the value, as the GPUI case's wrapper does.
        progress.PointerReleased += (_, _) => progress.Value = progress.Value == value ? valueTo : value;
        return progress;
    }

    private static Button Tooltip(GoldenCase c)
    {
        var button = new Button { Content = c.Str("label", "Hover me") };
        ClassFrom(button, c, "size", "medium");
        ToolTip.SetTip(button, c.Str("tip", "Tooltip text"));
        return button;
    }

    private static ProgressBar Spinner(GoldenCase c)
    {
        var spinner = new ProgressBar { IsIndeterminate = true };
        spinner.Theme = (Avalonia.Styling.ControlTheme)Avalonia.Application.Current!.FindResource("GpuiSpinner")!;
        ClassFrom(spinner, c, "size", "medium");
        return spinner;
    }

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

    /// <summary>A ListBox.toggle-group; '1's in `checked` mark the selected items.</summary>
    private static ListBox ToggleGroup(GoldenCase c)
    {
        var list = new ListBox
        {
            Classes = { "toggle-group" },
            SelectionMode = SelectionMode.Multiple | SelectionMode.Toggle,
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(list, c, "variant", "ghost");
        ClassFrom(list, c, "size", "medium");
        FlagClass(list, c, "segmented");
        var labels = c.Str("labels", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        var icons = c.Str("icons", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < Math.Max(labels.Length, icons.Length); i++)
        {
            list.Items.Add(i < icons.Length ? Icon(icons[i]) : labels[i]);
        }
        var checkedMask = c.Str("checked", "");
        for (var i = 0; i < checkedMask.Length; i++)
        {
            if (checkedMask[i] == '1')
            {
                list.Selection.Select(i);
            }
        }
        return list;
    }

    /// <summary>A StackPanel.button-group of labelled buttons; '1's in `selected` mark selected ones.</summary>
    private static StackPanel ButtonGroup(GoldenCase c)
    {
        var panel = new StackPanel
        {
            Orientation = c.Str("layout", "horizontal") == "vertical" ? Avalonia.Layout.Orientation.Vertical : Avalonia.Layout.Orientation.Horizontal,
            Classes = { "button-group" },
        };
        var selected = c.Str("selected", "");
        var labels = c.Str("labels", "One,Two,Three").Split(',');
        for (var i = 0; i < labels.Length; i++)
        {
            var button = new Button { Content = labels[i], IsEnabled = !c.Bool("disabled") };
            ClassFrom(button, c, "variant", "default");
            ClassFrom(button, c, "size", "medium");
            FlagClass(button, c, "outline");
            FlagClass(button, c, "compact");
            var rounded = c.Str("rounded", "medium");
            if (rounded != "medium")
            {
                button.Classes.Add("rounded-" + rounded);
            }
            if (i < selected.Length && selected[i] == '1')
            {
                button.Classes.Add("selected");
            }
            panel.Children.Add(button);
        }
        return panel;
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
