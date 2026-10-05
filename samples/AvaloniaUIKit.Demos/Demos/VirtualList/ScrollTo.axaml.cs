using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class VirtualListScrollTo
{
    // GPUI's virtual list story: Center centers a row; Top and Bottom bring a row
    // out of view to the nearer edge.
    private void OnTop(object? sender, RoutedEventArgs e) => Rows.ScrollToItem(0, ScrollStrategy.Top);

    private void OnRow50(object? sender, RoutedEventArgs e) => Rows.ScrollToItem(49, ScrollStrategy.Top);

    private void OnCenter25(object? sender, RoutedEventArgs e) => Rows.ScrollToItem(24, ScrollStrategy.Center);

    private void OnBottom(object? sender, RoutedEventArgs e) => Rows.ScrollToItem(Rows.ItemCount - 1, ScrollStrategy.Top);
}
