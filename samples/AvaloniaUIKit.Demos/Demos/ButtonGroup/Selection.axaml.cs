namespace AvaloniaUIKit.Demos;

public sealed partial class ButtonGroupSelection
{
    // Click reports the selection the click makes; the app applies it.
    private void OnSelect(object? sender, ButtonGroupClickEventArgs e)
    {
        var group = (ButtonGroup)sender!;
        for (var i = 0; i < group.Children.Count; i++)
        {
            group.Children[i].Classes.Set("selected", e.SelectedIndices.Contains(i));
        }
    }
}
