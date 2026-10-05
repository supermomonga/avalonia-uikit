using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class UikitResizablePanelGroupProgrammatic
{
    // ResizePanel resizes by the drag's rules: the neighbours give or take the room.
    private void OnResizeLeft(object? sender, RoutedEventArgs e) =>
        this.GetControl<ResizablePanelGroup>("Group").ResizePanel(0, double.Parse((string)((Button)sender!).Tag!, CultureInfo.InvariantCulture));

    private void OnResized(object? sender, ResizablePanelResizedEventArgs e) =>
        this.GetControl<TextBlock>("Sizes").Text = string.Join(" / ", e.Sizes.Select(s => s.ToString("0", CultureInfo.InvariantCulture)));
}
