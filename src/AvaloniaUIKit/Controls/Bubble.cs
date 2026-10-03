using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Bubble (bubble.rs): a message surface at most 80% of the room it
/// is given. Classes: the variant (secondary, muted, tinted, outline, ghost,
/// destructive; filled by default) and the alignment (start, end). A
/// <see cref="Reaction"/> hangs over the bottom edge at the end (classes
/// reaction-top, reaction-start move it).
/// </summary>
public class Bubble : ContentControl
{
    /// <summary>The reaction pill's content.</summary>
    public static readonly StyledProperty<object?> ReactionProperty =
        AvaloniaProperty.Register<Bubble, object?>(nameof(Reaction));

    /// <inheritdoc cref="ReactionProperty"/>
    public object? Reaction { get => GetValue(ReactionProperty); set => SetValue(ReactionProperty, value); }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        // bubble.rs: max_w(relative(0.8)), the full width when ghost.
        if (!Classes.Contains("ghost") && !double.IsInfinity(availableSize.Width))
        {
            availableSize = availableSize.WithWidth(availableSize.Width * 0.8);
        }
        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc />
    protected override void ArrangeCore(Rect finalRect)
    {
        // A stretched bubble stops at 80% too, at the start of its room.
        if (!Classes.Contains("ghost") && HorizontalAlignment == Avalonia.Layout.HorizontalAlignment.Stretch)
        {
            finalRect = finalRect.WithWidth(Math.Min(finalRect.Width, finalRect.Width * 0.8));
        }
        base.ArrangeCore(finalRect);
    }
}

/// <summary>
/// GPUI Kit's Message (message.rs): one turn of a conversation. The items are
/// its bubbles (or other content), 10px apart; <see cref="Avatar"/> sits at
/// the bottom beside them, <see cref="Header"/> above and <see cref="Footer"/>
/// below in muted text_xs, inset 12px unless a bubble is ghost. The end class
/// puts the message at the end of the row.
/// </summary>
public class Message : ItemsControl
{
    /// <summary>The avatar beside the bubbles.</summary>
    public static readonly StyledProperty<object?> AvatarProperty =
        AvaloniaProperty.Register<Message, object?>(nameof(Avatar));

    /// <summary>The line above the bubbles (a name and a time).</summary>
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<Message, object?>(nameof(Header));

    /// <summary>The line below the bubbles (a delivery status).</summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<Message, object?>(nameof(Footer));

    static Message()
    {
        AvatarProperty.Changed.AddClassHandler<Message>((m, _) => m.Update());
    }

    /// <summary>Creates a message.</summary>
    public Message()
    {
        Items.CollectionChanged += (_, _) => Update();
        Update();
    }

    /// <inheritdoc cref="AvatarProperty"/>
    public object? Avatar { get => GetValue(AvatarProperty); set => SetValue(AvatarProperty, value); }

    /// <inheritdoc cref="HeaderProperty"/>
    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }

    /// <inheritdoc cref="FooterProperty"/>
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    private void Update()
    {
        PseudoClasses.Set(":avatar", Avatar is not null);
        // message.rs: header and footer drop their inset beside a ghost bubble.
        PseudoClasses.Set(":ghost", Items.OfType<Bubble>().Any(b => b.Classes.Contains("ghost")));
    }
}
