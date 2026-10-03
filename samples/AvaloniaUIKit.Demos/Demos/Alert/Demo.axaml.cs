using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class AlertDemo
{
    // The alert only asks to be closed; the app decides what that means.
    private void OnClose(object? sender, RoutedEventArgs e) => ((Control)sender!).IsVisible = false;
}
