using Avalonia;

namespace AvaloniaUIKit;

/// <summary>Styling hooks for GPUI Kit's notifications.</summary>
public static class Notifications
{
    /// <summary>
    /// Whether the notifications below come in from the window's bottom edge
    /// (inherited). The notification manager's theme sets it for its bottom
    /// positions; a card's enter and exit slides read it.
    /// </summary>
    public static readonly AttachedProperty<bool> FromBottomProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("FromBottom", typeof(Notifications), inherits: true);

    /// <summary>
    /// Whether the notifications below slide in and out from their edge
    /// (inherited; true by default) or only fade, as GPUI's notifications at the
    /// middle of the left and right edges do (notification.rs). A
    /// <see cref="NotificationItem"/> shows it as the pseudo-class :fades.
    /// </summary>
    public static readonly AttachedProperty<bool> SlidesProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("Slides", typeof(Notifications), true, inherits: true);

    /// <summary>Gets whether the notifications come in from the bottom edge.</summary>
    public static bool GetFromBottom(StyledElement element) => element.GetValue(FromBottomProperty);

    /// <summary>Sets whether the notifications come in from the bottom edge.</summary>
    public static void SetFromBottom(StyledElement element, bool value) => element.SetValue(FromBottomProperty, value);

    /// <summary>Gets whether the notifications slide in and out.</summary>
    public static bool GetSlides(StyledElement element) => element.GetValue(SlidesProperty);

    /// <summary>Sets whether the notifications slide in and out.</summary>
    public static void SetSlides(StyledElement element, bool value) => element.SetValue(SlidesProperty, value);
}
