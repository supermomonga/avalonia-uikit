using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaUIKit;

/// <summary>Where a <see cref="NotificationList"/> stacks a notification (GPUI's Anchor).</summary>
public enum NotificationPlacement
{
    /// <summary>The window's top left corner.</summary>
    TopLeft,

    /// <summary>The middle of the window's top edge.</summary>
    TopCenter,

    /// <summary>The window's top right corner (GPUI's default).</summary>
    TopRight,

    /// <summary>The middle of the window's left edge.</summary>
    LeftCenter,

    /// <summary>The middle of the window's right edge.</summary>
    RightCenter,

    /// <summary>The window's bottom left corner.</summary>
    BottomLeft,

    /// <summary>The middle of the window's bottom edge.</summary>
    BottomCenter,

    /// <summary>The window's bottom right corner.</summary>
    BottomRight,
}

/// <summary>
/// GPUI Kit's Notification (notification.rs), shown by a
/// <see cref="NotificationList"/>; renamed as Avalonia has a Notification. A
/// <see cref="NotificationCard"/> with the card's look: a <see cref="Type"/>'s
/// icon (none without a type, or the app's <see cref="Icon"/>), a
/// <see cref="Title"/>, a <see cref="Message"/>, the content below them and
/// an <see cref="Action"/> at the end. The close button shows while the card
/// is hovered; a middle click closes it too. A click raises
/// <see cref="Click"/> and closes the card, but only when something handles
/// <see cref="Click"/> (GPUI's on_click). <see cref="NotificationCard.Close"/>
/// is GPUI's dismiss; <see cref="NotificationCard.NotificationClosed"/> is
/// its on_close.
/// </summary>
public class NotificationItem : NotificationCard
{
    /// <summary>
    /// The notification's type (GPUI's with_type): its icon in the type's
    /// color. Null (the default) shows <see cref="Icon"/>, or no icon.
    /// </summary>
    public static readonly StyledProperty<NotificationType?> TypeProperty =
        AvaloniaProperty.Register<NotificationItem, NotificationType?>(nameof(Type));

    /// <summary>The title, semibold above the message.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<NotificationItem, string?>(nameof(Title));

    /// <summary>The message.</summary>
    public static readonly StyledProperty<string?> MessageProperty =
        AvaloniaProperty.Register<NotificationItem, string?>(nameof(Message));

    /// <summary>
    /// The icon of a notification without a <see cref="Type"/>: an
    /// <see cref="IconName"/>, a Geometry or any control.
    /// </summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<NotificationItem, object?>(nameof(Icon));

    /// <summary>
    /// The action at the card's end, typically a Button (GPUI's action), which
    /// takes the small size unless it has a size class. A notification with an
    /// action does not hide itself unless <see cref="AutoHide"/> is true.
    /// </summary>
    public static readonly StyledProperty<object?> ActionProperty =
        AvaloniaProperty.Register<NotificationItem, object?>(nameof(Action));

    /// <summary>Where the notification is stacked; null takes the list's <see cref="NotificationList.Placement"/>.</summary>
    public static readonly StyledProperty<NotificationPlacement?> PlacementProperty =
        AvaloniaProperty.Register<NotificationItem, NotificationPlacement?>(nameof(Placement));

    /// <summary>
    /// Whether the notification closes itself 5s after it came in (the time
    /// the pointer or the focus is on the notifications does not count). Null
    /// (the default) hides it unless it has an <see cref="Action"/>.
    /// </summary>
    public static readonly StyledProperty<bool?> AutoHideProperty =
        AvaloniaProperty.Register<NotificationItem, bool?>(nameof(AutoHide));

    /// <summary>
    /// The notification's id (GPUI's id::&lt;T&gt;): showing one with the same
    /// <see cref="Id"/> and <see cref="Key"/> replaces it, and
    /// <see cref="NotificationList.Remove(object)"/> closes the ones with this id.
    /// Null (the default) never replaces another.
    /// </summary>
    public static readonly StyledProperty<object?> IdProperty =
        AvaloniaProperty.Register<NotificationItem, object?>(nameof(Id));

    /// <summary>The second part of the id (GPUI's id1::&lt;T&gt;(key)), to keep several notifications of one <see cref="Id"/>.</summary>
    public static readonly StyledProperty<object?> KeyProperty =
        AvaloniaProperty.Register<NotificationItem, object?>(nameof(Key));

    /// <summary>Defines the <see cref="Click"/> event.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
        RoutedEvent.Register<NotificationItem, RoutedEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private static readonly string[] Sizes = ["xsmall", "small", "large"];

    private bool _pressed;

    static NotificationItem()
    {
        TypeProperty.Changed.AddClassHandler<NotificationItem>((n, e) =>
        {
            if (e.GetNewValue<NotificationType?>() is { } type)
            {
                n.SetCurrentValue(NotificationTypeProperty, type);
            }
            n.UpdateIcon();
        });
        // NotificationCard's own type follows Type both ways.
        NotificationTypeProperty.Changed.AddClassHandler<NotificationItem>((n, e) =>
        {
            n.SetCurrentValue(TypeProperty, e.GetNewValue<NotificationType>());
            n.UpdateIcon();
        });
        IconProperty.Changed.AddClassHandler<NotificationItem>((n, _) => n.UpdateIcon());
        ActionProperty.Changed.AddClassHandler<NotificationItem>((n, e) => n.OnActionChanged(e.GetNewValue<object?>()));
    }

    /// <summary>Creates a notification without a type.</summary>
    public NotificationItem() => UpdateIcon();

    /// <summary>Creates a notification of <paramref name="type"/> showing <paramref name="message"/>.</summary>
    public NotificationItem(NotificationType? type, string? message) : this()
    {
        Type = type;
        Message = message;
    }

    /// <inheritdoc cref="TypeProperty"/>
    public NotificationType? Type
    {
        get => GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <inheritdoc cref="TitleProperty"/>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="MessageProperty"/>
    public string? Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <inheritdoc cref="IconProperty"/>
    public object? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <inheritdoc cref="ActionProperty"/>
    public object? Action
    {
        get => GetValue(ActionProperty);
        set => SetValue(ActionProperty, value);
    }

    /// <inheritdoc cref="PlacementProperty"/>
    public NotificationPlacement? Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <inheritdoc cref="AutoHideProperty"/>
    public bool? AutoHide
    {
        get => GetValue(AutoHideProperty);
        set => SetValue(AutoHideProperty, value);
    }

    /// <inheritdoc cref="IdProperty"/>
    public object? Id
    {
        get => GetValue(IdProperty);
        set => SetValue(IdProperty, value);
    }

    /// <inheritdoc cref="KeyProperty"/>
    public object? Key
    {
        get => GetValue(KeyProperty);
        set => SetValue(KeyProperty, value);
    }

    /// <summary>
    /// Raised when the card is clicked (not its buttons), as it starts closing.
    /// A card nothing handles this event for stays open when clicked.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>Whether the notification closes itself (notification.rs: an action turns autohide off).</summary>
    internal bool HidesItself => AutoHide ?? Action is null;

    /// <summary>Whether <paramref name="id"/> and <paramref name="key"/> are this notification's.</summary>
    internal bool Matches(object id, object? key) => Equals(Id, id) && Equals(Key, key);

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == Notifications.SlidesProperty)
        {
            // The card theme's enter and exit read this: only a fade at the sides' middle.
            PseudoClasses.Set(":fades", !change.GetNewValue<bool>());
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // A press its buttons took is theirs.
        if (e.Handled)
        {
            return;
        }
        var button = e.GetCurrentPoint(this).Properties.PointerUpdateKind;
        _pressed = button is PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.MiddleButtonPressed;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_pressed)
        {
            return;
        }
        _pressed = false;
        if (e.Handled || !new Rect(Bounds.Size).Contains(e.GetPosition(this)))
        {
            return;
        }
        switch (e.InitialPressMouseButton)
        {
            // notification.rs on_aux_click: a middle click dismisses.
            case MouseButton.Middle:
                Close();
                break;
            // notification.rs on_click: dismiss, then the app's handler; no handler, no dismiss.
            case MouseButton.Left:
                using (var route = BuildEventRoute(ClickEvent))
                {
                    if (!route.HasHandlers)
                    {
                        return;
                    }
                }
                Close();
                RaiseEvent(new RoutedEventArgs(ClickEvent, this));
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pressed = false;
    }

    private void UpdateIcon()
    {
        // Without a type, notification.rs shows the app's icon (or none) in place of the type's.
        var type = Type;
        var typed = type is not null;
        PseudoClasses.Set(":error", type == NotificationType.Error);
        PseudoClasses.Set(":information", type == NotificationType.Information);
        PseudoClasses.Set(":success", type == NotificationType.Success);
        PseudoClasses.Set(":warning", type == NotificationType.Warning);
        PseudoClasses.Set(":icon", !typed && Icon is not null);
        PseudoClasses.Set(":plain", !typed && Icon is null);
    }

    private void OnActionChanged(object? action)
    {
        PseudoClasses.Set(":action", action is not null);
        // notification.rs: the action is a small button.
        if (action is Button button && !Sizes.Any(button.Classes.Contains))
        {
            button.Classes.Add("small");
        }
    }
}
