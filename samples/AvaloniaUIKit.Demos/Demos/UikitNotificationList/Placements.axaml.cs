using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class UikitNotificationListPlacements
{
    private void OnShow(object? sender, RoutedEventArgs e)
    {
        var placement = (string)((Button)sender!).Content!;
        this.FindControl<NotificationList>("Notifications")!.Show(
            new NotificationItem(NotificationType.Information, $"This notification is at {placement}.")
            {
                Placement = Enum.Parse<NotificationPlacement>(placement),
            });
    }
}
