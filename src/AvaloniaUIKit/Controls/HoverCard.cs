using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's HoverCard (hover_card.rs): the content is the trigger; the
/// <see cref="Card"/> opens on a popover surface <see cref="OpenDelay"/>
/// after the pointer enters the trigger, and closes <see cref="CloseDelay"/>
/// after it leaves both the trigger and the card. GPUI anchors map to
/// placements as for the Popover (TopCenter, the default, is Bottom).
/// </summary>
[TemplatePart("PART_Trigger", typeof(Control))]
[TemplatePart("PART_Card", typeof(Control))]
public class HoverCard : ContentControl
{
    /// <summary>The card's content.</summary>
    public static readonly StyledProperty<object?> CardProperty =
        AvaloniaProperty.Register<HoverCard, object?>(nameof(Card));

    /// <summary>Where the card opens relative to the trigger.</summary>
    public static readonly StyledProperty<PlacementMode> PlacementProperty =
        AvaloniaProperty.Register<HoverCard, PlacementMode>(nameof(Placement), PlacementMode.Bottom);

    /// <summary>How long the pointer rests on the trigger before the card opens (GPUI: 600ms).</summary>
    public static readonly StyledProperty<TimeSpan> OpenDelayProperty =
        AvaloniaProperty.Register<HoverCard, TimeSpan>(nameof(OpenDelay), TimeSpan.FromMilliseconds(600));

    /// <summary>How long the card stays after the pointer leaves (GPUI: 300ms).</summary>
    public static readonly StyledProperty<TimeSpan> CloseDelayProperty =
        AvaloniaProperty.Register<HoverCard, TimeSpan>(nameof(CloseDelay), TimeSpan.FromMilliseconds(300));

    /// <summary>Whether the card is open.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<HoverCard, bool>(nameof(IsOpen));

    private Control? _trigger;
    private Control? _card;
    private DispatcherTimer? _timer;

    /// <inheritdoc cref="CardProperty"/>
    public object? Card { get => GetValue(CardProperty); set => SetValue(CardProperty, value); }

    /// <inheritdoc cref="PlacementProperty"/>
    public PlacementMode Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <inheritdoc cref="OpenDelayProperty"/>
    public TimeSpan OpenDelay { get => GetValue(OpenDelayProperty); set => SetValue(OpenDelayProperty, value); }

    /// <inheritdoc cref="CloseDelayProperty"/>
    public TimeSpan CloseDelay { get => GetValue(CloseDelayProperty); set => SetValue(CloseDelayProperty, value); }

    /// <inheritdoc cref="IsOpenProperty"/>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Hook(ref _trigger, e.NameScope.Find<Control>("PART_Trigger"), OnTriggerEntered, OnTriggerExited);
        Hook(ref _card, e.NameScope.Find<Control>("PART_Card"), OnCardEntered, OnCardExited);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _timer?.Stop();
        SetCurrentValue(IsOpenProperty, false);
    }

    private static void Hook(ref Control? field, Control? control, EventHandler<PointerEventArgs> entered, EventHandler<PointerEventArgs> exited)
    {
        if (field is not null)
        {
            field.PointerEntered -= entered;
            field.PointerExited -= exited;
        }
        field = control;
        if (field is not null)
        {
            field.PointerEntered += entered;
            field.PointerExited += exited;
        }
    }

    // hover_card.rs HoverCardState: entering the trigger schedules the open
    // (or cancels a pending close); leaving it cancels the open and schedules
    // the close; the card keeps itself open while the pointer is on it.
    private void OnTriggerEntered(object? sender, PointerEventArgs e) => Schedule(IsOpen ? null : true, OpenDelay);

    private void OnTriggerExited(object? sender, PointerEventArgs e) => Schedule(IsOpen ? false : null, CloseDelay);

    private void OnCardEntered(object? sender, PointerEventArgs e) => Schedule(null, default);

    private void OnCardExited(object? sender, PointerEventArgs e) => Schedule(false, CloseDelay);

    private void Schedule(bool? open, TimeSpan delay)
    {
        _timer?.Stop();
        _timer = null;
        if (open is not { } target)
        {
            return;
        }
        _timer = new DispatcherTimer(delay, DispatcherPriority.Normal, (_, _) =>
        {
            _timer?.Stop();
            _timer = null;
            SetCurrentValue(IsOpenProperty, target);
        });
        _timer.Start();
    }
}
