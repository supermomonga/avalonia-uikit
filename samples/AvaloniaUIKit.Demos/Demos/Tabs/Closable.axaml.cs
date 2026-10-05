using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class TabsClosable
{
    private int _added;

    // The add button asks the factory for the new tab; the last tab is kept open.
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var tabs = this.GetControl<TabControl>("Tabs");
        AvaloniaUIKit.Tabs.SetNewTabFactory(tabs, () => new TabItem
        {
            Header = $"Tab {++_added}",
            Content = new TextBlock { Text = "A new tab.", Margin = new Avalonia.Thickness(0, 12) },
        });
        AvaloniaUIKit.Tabs.AddTabClosingHandler(tabs, (_, args) => args.Cancel = tabs.ItemCount == 1);
    }
}
