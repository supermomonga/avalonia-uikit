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
    private static object LabelledIcon(GoldenCase c) => new StackPanel
    {
        Orientation = Orientation.Horizontal,
        Spacing = c.Str("size", "medium") is "xsmall" or "small" ? 4 : 8,
        Children = { Icon(c.Str("icon")), new TextBlock { Text = c.Str("label"), VerticalAlignment = VerticalAlignment.Center } },
    };

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
}
