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
}
