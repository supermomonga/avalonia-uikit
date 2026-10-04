using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Adapters for the button and group controls of ADR 30 (uikit:Buttons, ButtonGroup, ToggleGroup, Accordion, Toolbar).</summary>
public static partial class Adapters
{
    /// <summary>
    /// GPUI's Button with an icon before its label: the icon and the text in a row,
    /// gap_1 (xsmall, small) or gap_2 apart (button.rs).
    /// </summary>
    private static StackPanel LabelledIcon(string icon, string label, string size) => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = size is "xsmall" or "small" ? 4 : 8,
        Children = { Icon(icon), new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center } },
    };

    private static StackPanel LabelledIcon(GoldenCase c) => LabelledIcon(c.Str("icon"), c.Str("label"), c.Str("size", "medium"));

    /// <summary>A button case (button.rs) with Button::loading and loading_icon.</summary>
    private static Button LoadingButton(GoldenCase c)
    {
        var button = Button(c);
        if (c.Has("icon") && c.Has("label"))
        {
            button.Content = LabelledIcon(c);
        }
        Buttons.SetIsLoading(button, c.Bool("loading"));
        if (c.Has("loading_icon"))
        {
            Buttons.SetLoadingIcon(button, Icon(c.Str("loading_icon")).Data);
        }
        return button;
    }

    /// <summary>A split case (menu.rs split) whose action Button loads, with an icon before its label.</summary>
    private static SplitButton LoadingSplit(GoldenCase c)
    {
        var split = Split(c);
        if (c.Has("icon"))
        {
            split.Content = LabelledIcon(c.Str("icon"), c.Str("label", "Save"), c.Str("size", "medium"));
        }
        Buttons.SetIsLoading(split, c.Bool("loading"));
        return split;
    }

    /// <summary>An input-group case whose buttons load, or leave the focus in the input when pressed.</summary>
    private static TextBox LoadingInputGroup(GoldenCase c)
    {
        var box = InputGroup(c);
        if (box.InnerRightContent is Button button)
        {
            Buttons.SetIsLoading(button, c.Bool("loading"));
        }
        if (c.Bool("keeps_focus"))
        {
            Buttons.SetTakesFocusOnPointer(box, false);
        }
        return box;
    }

    /// <summary>
    /// The buttongroup cases as a uikit:ButtonGroup: the group's variant, outline,
    /// compact and size as its classes; each button's rounding and selection as its
    /// own. With `selectable` the case applies the indices Click reports, as an app
    /// following on_click does.
    /// </summary>
    private static ButtonGroup ButtonGroupCase(GoldenCase c)
    {
        var group = new ButtonGroup
        {
            Orientation = c.Str("layout", "horizontal") == "vertical" ? Orientation.Vertical : Orientation.Horizontal,
            Multiple = c.Bool("multiple"),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(group, c, "variant", "default");
        ClassFrom(group, c, "size", "medium");
        FlagClass(group, c, "outline");
        FlagClass(group, c, "compact");
        var selected = c.Str("selected", "");
        var labels = c.Str("labels", "One,Two,Three").Split(',');
        for (var i = 0; i < labels.Length; i++)
        {
            var button = new Button { Content = labels[i] };
            var rounded = c.Str("rounded", "medium");
            if (rounded != "medium")
            {
                button.Classes.Add("rounded-" + rounded);
            }
            button.Classes.Set("selected", i < selected.Length && selected[i] == '1');
            group.Children.Add(button);
        }
        if (c.Bool("selectable"))
        {
            group.Click += (_, e) =>
            {
                for (var i = 0; i < group.Children.Count; i++)
                {
                    group.Children[i].Classes.Set("selected", e.SelectedIndices.Contains(i));
                }
            };
        }
        return group;
    }

    /// <summary>The togglegroup cases as a uikit:ToggleGroup of ToggleButtons.</summary>
    private static ToggleGroup ToggleGroupCase(GoldenCase c)
    {
        var group = new ToggleGroup { IsEnabled = !c.Bool("disabled") };
        ClassFrom(group, c, "variant", "ghost");
        ClassFrom(group, c, "size", "medium");
        FlagClass(group, c, "segmented");
        var labels = c.Str("labels", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        var icons = c.Str("icons", "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        var checkedMask = c.Str("checked", "");
        for (var i = 0; i < Math.Max(labels.Length, icons.Length); i++)
        {
            group.Children.Add(new Avalonia.Controls.Primitives.ToggleButton
            {
                Content = i < icons.Length ? Icon(icons[i]) : labels[i],
                IsChecked = i < checkedMask.Length && checkedMask[i] == '1',
            });
        }
        return group;
    }

    /// <summary>
    /// The accordion cases as a uikit:Accordion of Expanders: the accordion's size
    /// and card on it, items as there. `single` is GPUI's default of one open item.
    /// </summary>
    private static Accordion AccordionCase(GoldenCase c)
    {
        var size = c.Str("size", "medium");
        var scope = c.Str("scope");
        var accordion = new Accordion
        {
            Width = c.Num("width", 280),
            Multiple = !c.Bool("single"),
            IsBordered = !c.Bool("borderless"),
            IsEnabled = scope != "all",
            HorizontalAlignment = HorizontalAlignment.Left,
        };
        ClassFrom(accordion, c, "size", "medium");
        (string Title, string Body, string Icon)[] rows =
        [
            ("Is it accessible?", "Yes, it is.", "copy"),
            ("Is it styled?", "Yes, by the theme.", "plus"),
            ("Is it animated?", "Yes, with a spring.", "check"),
        ];
        for (var i = 0; i < rows.Length; i++)
        {
            object header = rows[i].Title;
            if (c.Bool("icons"))
            {
                // AccordionItem::icon: the item's icon size, 4px (xsmall, small) or 8px before the title.
                var icon = Icon(rows[i].Icon);
                icon.Width = icon.Height = size switch { "xsmall" => 12, "small" => 14, "large" => 24, _ => 16 };
                header = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = size is "xsmall" or "small" ? 4 : 8,
                    Children = { icon, new TextBlock { Text = rows[i].Title, VerticalAlignment = VerticalAlignment.Center } },
                };
            }
            accordion.Items.Add(new Expander
            {
                Header = header,
                Content = rows[i].Body,
                IsExpanded = i == 0 && c.Bool("open_first"),
                IsEnabled = !(scope == "item" && i == 1),
            });
        }
        return accordion;
    }

    /// <summary>
    /// The toolbar cases as a uikit:Toolbar: optional text, undo and redo (in a
    /// ToolbarGroup with `group`), a separator, New, a separator, a spacer with
    /// `spacer`, and the bold toggle. The toolbar sizes its controls.
    /// </summary>
    private static Toolbar ToolbarCase(GoldenCase c)
    {
        var enabled = !c.Bool("disabled");
        var bar = new Toolbar();
        if (c.Has("width"))
        {
            bar.Width = c.Num("width", 0);
        }
        ClassFrom(bar, c, "size", "small");
        if (c.Has("content"))
        {
            bar.Children.Add(new TextBlock { Text = c.Str("content") });
        }
        Button IconButton(string icon) => new() { Classes = { "icon-only" }, Content = Icon(icon), IsEnabled = enabled };
        if (c.Bool("group"))
        {
            var history = new ToolbarGroup { Spacing = 4, Children = { IconButton("undo-2"), IconButton("redo-2") } };
            Avalonia.Automation.AutomationProperties.SetName(history, "History");
            bar.Children.Add(history);
        }
        else
        {
            bar.Children.Add(IconButton("undo-2"));
            bar.Children.Add(IconButton("redo-2"));
        }
        bar.Children.Add(new Separator { Classes = { "vertical" }, Height = 20 });
        bar.Children.Add(new Button { Content = LabelledIcon("plus", "New", c.Str("size", "small")), IsEnabled = enabled });
        bar.Children.Add(new Separator { Classes = { "vertical" }, Height = 20 });
        if (c.Bool("spacer"))
        {
            bar.Children.Add(new ToolbarSpacer());
        }
        bar.Children.Add(new Avalonia.Controls.Primitives.ToggleButton { Content = "B", IsChecked = c.Bool("checked"), IsEnabled = enabled });
        return bar;
    }
}

