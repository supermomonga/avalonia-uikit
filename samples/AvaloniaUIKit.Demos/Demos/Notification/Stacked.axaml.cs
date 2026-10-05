using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Interactivity;

namespace AvaloniaUIKit.Demos;

public sealed partial class NotificationStacked
{
    private bool _shown;

    private NotificationList List => this.FindControl<NotificationList>("Notifications")!;

    // A stack to start with: the newest in front, the older ones behind it; hover it to spread them.
    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_shown)
        {
            return;
        }
        _shown = true;
        List.Show(new NotificationItem(NotificationType.Success, "Your receipt was emailed.") { Title = "Payment received", AutoHide = false });
        List.Show(new NotificationItem(NotificationType.Warning, "Connection unstable.") { AutoHide = false });
        List.Show(new NotificationItem(NotificationType.Information, "Your changes have been saved.") { AutoHide = false });
    }

    private void OnShowType(object? sender, RoutedEventArgs e)
    {
        var type = Enum.Parse<NotificationType>((string)((Button)sender!).Tag!);
        List.Show(new NotificationItem(type, "Your changes have been saved to the cloud.") { Title = type.ToString() });
    }

    // GPUI's action: a notification with one stays until it is closed.
    private void OnShowAction(object? sender, RoutedEventArgs e)
    {
        var retry = new Button { Content = "Retry", Classes = { "primary" } };
        Avalonia.Controls.Notifications.NotificationCard.SetCloseOnClick(retry, true);
        List.Show(new NotificationItem(null, "There was a problem with your request.")
        {
            Title = "Uh oh! Something went wrong.",
            Action = retry,
        });
    }

    // The same Id replaces the notification shown before.
    private void OnShowUnique(object? sender, RoutedEventArgs e) =>
        List.Show(new NotificationItem(null, $"Synced at {DateTime.Now:T}.") { Id = "sync", Icon = IconName.RefreshCw });

    private void OnClear(object? sender, RoutedEventArgs e) => List.Clear();
}
