using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class NotificationDemo
{
    private WindowNotificationManager? _manager;

    private void OnShow(object? sender, RoutedEventArgs e)
    {
        _manager ??= new WindowNotificationManager(TopLevel.GetTopLevel(this)) { MaxItems = 3 };
        _manager.Show(new Notification("Saved", "Your changes have been saved."));
    }
}
