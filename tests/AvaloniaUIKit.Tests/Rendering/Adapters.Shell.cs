using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Adapters for uikit:NotificationList and uikit:TitleBar
/// (reference/src/cases/notification.rs, title_bar.rs).
/// </summary>
public static partial class Adapters
{
    /// <summary>notification.rs STACK: the notifications a stack case shows, oldest first.</summary>
    private static readonly (string Type, string? Title, string Message)[] NotificationStack =
    [
        ("success", "Payment received", "Your receipt was emailed."),
        ("warning", null, "Connection unstable."),
        ("info", null, "Your changes have been saved."),
        ("error", null, "Request failed."),
    ];

    /// <summary>
    /// The uikit-notification cases: a list filling the case area, showing its
    /// notifications as the window opens, as GPUI pushes them on the first frame.
    /// </summary>
    public static NotificationList NotificationListCase(GoldenCase c)
    {
        var placement = NotificationPlacementOf(c.Str("placement", "top-right"));
        var list = new NotificationList
        {
            Width = c.Num("width", 430),
            Height = c.Num("height", 170),
            Placement = placement,
        };
        var items = new List<NotificationItem>();
        if (c.Num("stack", 0) is > 0 and var stack)
        {
            foreach (var (type, title, message) in NotificationStack.Take((int)stack))
            {
                items.Add(new NotificationItem(NotificationTypeOf(type), message) { Title = title, Placement = placement, AutoHide = false });
            }
        }
        else
        {
            var item = new NotificationItem(NotificationTypeOf(c.Str("type", "info")), c.Str("message", "Your changes have been saved."))
            {
                Title = c.Has("title") ? c.Str("title") : null,
                Placement = placement,
            };
            if (c.Bool("persistent"))
            {
                item.AutoHide = false;
            }
            if (c.Has("icon"))
            {
                item.Icon = IconKind(c.Str("icon"));
            }
            if (c.Has("action"))
            {
                // The story's action: a primary button (the notification makes it small).
                item.Action = new Button { Content = c.Str("action"), Classes = { "primary" } };
            }
            items.Add(item);
        }
        var shown = false;
        list.AttachedToVisualTree += (_, _) =>
        {
            if (!shown)
            {
                shown = true;
                items.ForEach(item => list.Show(item));
            }
        };
        return list;
    }

    public static NotificationPlacement NotificationPlacementOf(string placement) => placement switch
    {
        "top-left" => NotificationPlacement.TopLeft,
        "top-center" => NotificationPlacement.TopCenter,
        "left-center" => NotificationPlacement.LeftCenter,
        "right-center" => NotificationPlacement.RightCenter,
        "bottom-left" => NotificationPlacement.BottomLeft,
        "bottom-center" => NotificationPlacement.BottomCenter,
        "bottom-right" => NotificationPlacement.BottomRight,
        _ => NotificationPlacement.TopRight,
    };

    private static NotificationType? NotificationTypeOf(string type) => type switch
    {
        "none" => null,
        "success" => NotificationType.Success,
        "warning" => NotificationType.Warning,
        "error" => NotificationType.Error,
        _ => NotificationType.Information,
    };

    /// <summary>
    /// The uikit-titlebar cases: the bar with macOS's 80px for the traffic
    /// lights and no caption buttons, as GPUI draws it there (R14), with the
    /// story's title and buttons.
    /// </summary>
    public static TitleBar TitleBarCase(GoldenCase c)
    {
        var bar = new TitleBar
        {
            Width = c.Num("width", 480),
            Padding = new Thickness(80, 0, 0, 1),
            ShowsCaptionButtons = false,
        };
        if (c.Has("title"))
        {
            // crates/story/src/title_bar.rs: the title, text_sm and medium.
            bar.Items.Add(new TextBlock { Text = c.Str("title"), FontSize = 14, FontWeight = FontWeight.Medium, LineHeight = 22.5 });
        }
        if (c.Bool("content"))
        {
            // The story's right side: small ghost icon buttons, 8px apart and 8px in from both ends.
            bar.Items.Add(new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Spacing = 8,
                Margin = new Thickness(8, 0),
                Children =
                {
                    new Button { Classes = { "ghost", "small", "icon-only" }, Content = Icon("github") },
                    new Button { Classes = { "ghost", "small", "compact", "icon-only" }, Content = Icon("bell") },
                },
            });
        }
        return bar;
    }
}
