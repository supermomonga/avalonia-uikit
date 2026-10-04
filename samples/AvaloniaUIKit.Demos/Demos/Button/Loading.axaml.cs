using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AvaloniaUIKit.Demos;

public sealed partial class ButtonLoading
{
    // The button loads for two seconds; clicks meanwhile do nothing.
    private void OnSave(object? sender, RoutedEventArgs e)
    {
        var button = (Button)sender!;
        Buttons.SetIsLoading(button, true);
        DispatcherTimer.RunOnce(() => Buttons.SetIsLoading(button, false), TimeSpan.FromSeconds(2));
    }
}
