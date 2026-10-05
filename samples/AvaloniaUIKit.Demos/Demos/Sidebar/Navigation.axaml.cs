using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace AvaloniaUIKit.Demos;

public sealed partial class SidebarNavigation
{
    // The clicked item becomes the active one.
    private void OnItemClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not SidebarMenuItem clicked)
        {
            return;
        }
        foreach (var item in this.GetControl<Sidebar>("Sidebar").GetVisualDescendants().OfType<SidebarMenuItem>())
        {
            item.IsActive = item == clicked;
        }
        this.GetControl<TextBlock>("Current").Text = clicked.Label;
    }
}
