using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class SheetPlacements
{
    private void OnOpenSheet(object? sender, RoutedEventArgs e)
    {
        var sheet = this.GetControl<DrawerPage>("Sheet");
        sheet.DrawerPlacement = (sender as Button)?.Tag switch
        {
            "Left" => DrawerPlacement.Left,
            "Top" => DrawerPlacement.Top,
            "Bottom" => DrawerPlacement.Bottom,
            _ => DrawerPlacement.Right,
        };
        sheet.IsOpen = true;
    }
}
