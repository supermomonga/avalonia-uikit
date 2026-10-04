using Avalonia.Controls;
using Avalonia.Interactivity;
using Tabalonia.Controls;

namespace AvaloniaUIKit.Demos;

public sealed partial class TabaloniaDemo
{
    private int _added;

    // The add button asks the factory for the new tab; the last one is kept open.
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var tabs = this.GetControl<TabsControl>("Tabs");
        tabs.NewItemFactory = () => new DragTabItem
        {
            Header = $"Tab {++_added}",
            Content = new TextBlock { Text = "A new tab.", Margin = new Avalonia.Thickness(0, 12) },
        };
        tabs.TabClosing = (_, args) => args.Cancel = tabs.ItemCount == 1;
    }
}
