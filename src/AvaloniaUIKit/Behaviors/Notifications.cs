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

    /// <summary>Gets whether the notifications come in from the bottom edge.</summary>
    public static bool GetFromBottom(StyledElement element) => element.GetValue(FromBottomProperty);

    /// <summary>Sets whether the notifications come in from the bottom edge.</summary>
    public static void SetFromBottom(StyledElement element, bool value) => element.SetValue(FromBottomProperty, value);
}
