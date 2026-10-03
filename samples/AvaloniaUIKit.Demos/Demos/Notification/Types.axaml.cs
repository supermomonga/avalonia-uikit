using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class NotificationTypes
{
    private WindowNotificationManager? _manager;

    private void OnShow(object? sender, RoutedEventArgs e)
    {
        _manager ??= new WindowNotificationManager(TopLevel.GetTopLevel(this)) { MaxItems = 3 };
        var kind = (sender as Button)?.Tag as string;
        var type = kind switch
        {
            "success" => NotificationType.Success,
            "warning" => NotificationType.Warning,
            "error" => NotificationType.Error,
            _ => NotificationType.Information,
        };
        // The class plain is the notification without a type icon.
        _manager.Show(new Notification("Update available", "A new version is ready to install.", type), type,
            classes: kind == "plain" ? ["plain"] : null);
    }
}
