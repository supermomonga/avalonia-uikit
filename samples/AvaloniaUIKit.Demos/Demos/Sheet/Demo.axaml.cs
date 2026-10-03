using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class SheetDemo
{
    private void OnOpenSheet(object? sender, RoutedEventArgs e) => this.GetControl<DrawerPage>("Sheet").IsOpen = true;
}
