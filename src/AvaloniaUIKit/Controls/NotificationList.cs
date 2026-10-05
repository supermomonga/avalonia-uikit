using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's NotificationList (notification.rs), the in-app notifications
/// of a window: <see cref="Show(NotificationItem)"/> adds a
/// <see cref="NotificationItem"/> to the stack at its placement, newest in
/// front. A stack shows its three newest cards on top of each other, each
/// older one 14px further out and 5% narrower, and spreads them 14px apart
/// while the pointer is over it or the focus is in it; cards move to their
/// places on GPUI's springs. A card hides itself 5s after it came in, a
/// countdown that stops while any stack is spread. Only the
/// <see cref="MaxItems"/> newest open cards are shown. Showing a card with the
/// <see cref="NotificationItem.Id"/> and <see cref="NotificationItem.Key"/> of
/// one already shown replaces that one at once.
/// </summary>
/// <remarks>
/// Create one for a TopLevel (<c>new NotificationList(topLevel)</c>), which
/// puts it over the window's content in the adorner layer as
/// WindowNotificationManager does, or place it over the content yourself. It
/// takes no input itself: only the cards do. Its <see cref="Padding"/> is the
/// space between the stacks and the edges (GPUI's margins: 50px at the top,
/// under a 34px title bar, and 16px elsewhere).
/// </remarks>
public class NotificationList : Control
{
    /// <summary>Where notifications without their own placement go (GPUI's NotificationSettings::placement).</summary>
    public static readonly StyledProperty<NotificationPlacement> PlacementProperty =
        AvaloniaProperty.Register<NotificationList, NotificationPlacement>(nameof(Placement), NotificationPlacement.TopRight);

    /// <summary>The number of open notifications shown at once; older ones wait (GPUI's max_items, 10).</summary>
    public static readonly StyledProperty<int> MaxItemsProperty =
        AvaloniaProperty.Register<NotificationList, int>(nameof(MaxItems), 10);

    /// <summary>The width of the notifications (GPUI's width, 382).</summary>
    public static readonly StyledProperty<double> ItemWidthProperty =
        AvaloniaProperty.Register<NotificationList, double>(nameof(ItemWidth), 382);

    /// <summary>The space between the stacks and the list's edges (GPUI's margins).</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<NotificationList>(new StyledPropertyMetadata<Thickness>(new Thickness(16, 50, 16, 16)));

    // notification.rs: the lifecycle clock's tick, the enter time and the autohide timeout.
    private static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(50);
    private static readonly TimeSpan EnterDuration = TimeSpan.FromMilliseconds(400);
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly List<Entry> _entries = [];
    private readonly List<NotificationStack> _stacks = [];
    private DispatcherTimer? _clock;
    private TopLevel? _pointerSource;

    static NotificationList()
    {
        AffectsArrange<NotificationList>(PaddingProperty, PlacementProperty);
        AffectsMeasure<NotificationList>(ItemWidthProperty);
        MaxItemsProperty.Changed.AddClassHandler<NotificationList>((list, _) => list.UpdateShown());
        PlacementProperty.Changed.AddClassHandler<NotificationList>((list, _) => list.Regroup());
    }

    /// <summary>Creates a list to place over the content.</summary>
    public NotificationList()
    {
    }

    /// <summary>Creates a list shown over <paramref name="host"/>'s content, in its adorner layer.</summary>
    public NotificationList(TopLevel host) : this() => Install(host);

    /// <inheritdoc cref="PlacementProperty"/>
    public NotificationPlacement Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <inheritdoc cref="MaxItemsProperty"/>
    public int MaxItems
    {
        get => GetValue(MaxItemsProperty);
        set => SetValue(MaxItemsProperty, value);
    }

    /// <inheritdoc cref="ItemWidthProperty"/>
    public double ItemWidth
    {
        get => GetValue(ItemWidthProperty);
        set => SetValue(ItemWidthProperty, value);
    }

    /// <inheritdoc cref="PaddingProperty"/>
    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    /// <summary>The notifications in the list, oldest first, closing ones included.</summary>
    public IReadOnlyList<NotificationItem> Notifications => _entries.Select(e => e.Item).ToList();

    /// <summary>Shows a notification without a type (GPUI's push_notification of a string).</summary>
    public NotificationItem Show(string message) => Show(new NotificationItem(null, message));

    /// <summary>Shows a notification of <paramref name="type"/> (GPUI's push_notification of a type and a string).</summary>
    public NotificationItem Show(NotificationType type, string message) => Show(new NotificationItem(type, message));

    /// <summary>
    /// Shows <paramref name="notification"/> as the newest in its stack. One
    /// with the same <see cref="NotificationItem.Id"/> and
    /// <see cref="NotificationItem.Key"/> leaves at once, without its exit.
    /// </summary>
    public NotificationItem Show(NotificationItem notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        Dispatcher.UIThread.VerifyAccess();
        // base/toast.rs ToastManager::push: a toast with the same id is replaced, the new one is the newest.
        foreach (var entry in _entries.Where(e => e.Item == notification ||
            (notification.Id is { } id && e.Item.Matches(id, notification.Key))).ToList())
        {
            Drop(entry);
        }
        var added = new Entry(notification) { HidesItself = notification.HidesItself, Remaining = Timeout };
        _entries.Add(added);
        notification.NotificationClosed += OnNotificationClosed;
        notification.PropertyChanged += OnNotificationPropertyChanged;
        StackFor(notification.Placement ?? Placement).Children.Add(notification);
        UpdateShown();
        RunClock();
        return notification;
    }

    /// <summary>
    /// Closes the notifications with this <see cref="NotificationItem.Id"/>,
    /// whatever their key (GPUI's remove_notification::&lt;T&gt;).
    /// </summary>
    public void Remove(object id)
    {
        foreach (var entry in _entries.Where(e => Equals(e.Item.Id, id)).ToList())
        {
            entry.Item.Close();
        }
    }

    /// <summary>Closes the notification with this id and key (GPUI's remove_notification1::&lt;T&gt;(key)).</summary>
    public void Remove(object id, object? key)
    {
        foreach (var entry in _entries.Where(e => e.Item.Matches(id, key)).ToList())
        {
            entry.Item.Close();
        }
    }

    /// <summary>Closes every notification (GPUI's clear_notifications).</summary>
    public void Clear()
    {
        foreach (var entry in _entries.ToList())
        {
            entry.Item.Close();
        }
    }

    /// <summary>Whether a stack is spread, which stops the countdowns (notification.rs is_expanded).</summary>
    internal bool IsExpanded => _stacks.Any(s => s.IsExpanded);

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // base/toast.rs ToastStack: a stack's hover is the pointer's last move over
        // its bounds, wherever it goes, so it is read from the whole window.
        _pointerSource = TopLevel.GetTopLevel(this);
        _pointerSource?.AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _pointerSource?.RemoveHandler(PointerMovedEvent, OnPointerMoved);
        _pointerSource = null;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var stack in _stacks)
        {
            stack.Measure(new Size(ItemWidth, availableSize.Height));
        }
        return default;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var margins = Padding;
        foreach (var stack in _stacks)
        {
            // notification.rs NotificationList::render: each placement's stack, absolutely placed.
            var width = ItemWidth;
            var height = Math.Min(stack.DesiredSize.Height, finalSize.Height);
            var x = stack.Placement switch
            {
                NotificationPlacement.TopLeft or NotificationPlacement.BottomLeft or NotificationPlacement.LeftCenter => margins.Left,
                NotificationPlacement.TopCenter or NotificationPlacement.BottomCenter => finalSize.Width / 2 - width / 2,
                _ => finalSize.Width - margins.Right - width,
            };
            var y = stack.Placement switch
            {
                NotificationPlacement.TopLeft or NotificationPlacement.TopCenter or NotificationPlacement.TopRight => margins.Top,
                NotificationPlacement.LeftCenter or NotificationPlacement.RightCenter => (finalSize.Height - height) / 2,
                _ => finalSize.Height - margins.Bottom - height,
            };
            stack.Arrange(new Rect(x, y, width, height));
        }
        return finalSize;
    }

    private NotificationStack StackFor(NotificationPlacement placement)
    {
        if (_stacks.FirstOrDefault(s => s.Placement == placement) is { } stack)
        {
            return stack;
        }
        stack = new NotificationStack(placement);
        _stacks.Add(stack);
        LogicalChildren.Add(stack);
        VisualChildren.Add(stack);
        InvalidateMeasure();
        return stack;
    }

    private void OnNotificationClosed(object? sender, RoutedEventArgs e)
    {
        if (_entries.FirstOrDefault(entry => entry.Item == sender) is { } closed)
        {
            Drop(closed);
        }
    }

    private void OnNotificationPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // An older notification takes the place of one that starts closing.
        if (e.Property == NotificationCard.IsClosingProperty)
        {
            UpdateShown();
        }
        else if (e.Property == NotificationItem.PlacementProperty)
        {
            Regroup();
        }
    }

    /// <summary>Takes the notification out of the list now.</summary>
    private void Drop(Entry entry)
    {
        var item = entry.Item;
        _entries.Remove(entry);
        item.NotificationClosed -= OnNotificationClosed;
        item.PropertyChanged -= OnNotificationPropertyChanged;
        if (item.Parent is NotificationStack stack)
        {
            stack.Children.Remove(item);
            Prune(stack);
        }
        UpdateShown();
    }

    /// <summary>notification.rs: a placement without notifications has no stack.</summary>
    private void Prune(NotificationStack stack)
    {
        if (stack.Children.Count == 0)
        {
            _stacks.Remove(stack);
            VisualChildren.Remove(stack);
            LogicalChildren.Remove(stack);
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// notification.rs grouped: each notification in the stack of its
    /// placement, in the order they came in. One moved to another stack enters
    /// it anew, as GPUI's does under the new stack's id.
    /// </summary>
    private void Regroup()
    {
        foreach (var entry in _entries)
        {
            var item = entry.Item;
            var placement = item.Placement ?? Placement;
            if (item.Parent is NotificationStack { } current)
            {
                if (current.Placement == placement)
                {
                    continue;
                }
                current.Children.Remove(item);
                Prune(current);
            }
            var stack = StackFor(placement);
            stack.Children.Insert(_entries.TakeWhile(e => e != entry).Count(e => e.Item.Parent == stack), item);
        }
    }

    /// <summary>
    /// base/toast.rs ToastManager::visible: the newest <see cref="MaxItems"/>
    /// open notifications and every closing one.
    /// </summary>
    private void UpdateShown()
    {
        var open = _entries.Count(e => !e.Item.IsClosing);
        var skip = Math.Max(0, open - Math.Max(0, MaxItems));
        foreach (var entry in _entries)
        {
            var shown = entry.Item.IsClosing || skip-- <= 0;
            entry.Item.IsVisible = shown;
        }
    }

    private void RunClock()
    {
        if (_clock is null)
        {
            _clock = new DispatcherTimer { Interval = Tick };
            _clock.Tick += (_, _) => Advance();
        }
        _clock.Start();
    }

    /// <summary>
    /// notification.rs advance (base/toast.rs ToastManager::advance): every
    /// 50ms, a notification that came in 400ms ago is present, and a present
    /// one that hides itself counts down while no stack is spread.
    /// </summary>
    private void Advance()
    {
        var paused = IsExpanded;
        foreach (var entry in _entries.ToList())
        {
            if (entry.Item.IsClosing)
            {
                continue;
            }
            if (!entry.IsPresent)
            {
                entry.Entering += Tick;
                entry.IsPresent = entry.Entering >= EnterDuration;
            }
            else if (entry.HidesItself && !paused)
            {
                entry.Remaining -= Tick;
                if (entry.Remaining <= TimeSpan.Zero)
                {
                    entry.Item.Close();
                }
            }
        }
        // notification.rs needs_clock: nothing to time once every open notification is present and stays.
        if (!_entries.Any(e => !e.Item.IsClosing && (!e.IsPresent || e.HidesItself)))
        {
            _clock?.Stop();
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        foreach (var stack in _stacks)
        {
            stack.PointerMovedTo(e.GetPosition(stack));
        }
    }

    /// <summary>Installs the list in the TopLevel's adorner layer, as WindowNotificationManager does.</summary>
    private void Install(TopLevel host)
    {
        if (AdornerLayer.GetAdornerLayer(host) is { } layer)
        {
            layer.Children.Add(this);
            AdornerLayer.SetAdornedElement(this, layer);
            return;
        }
        host.TemplateApplied += OnHostTemplateApplied;
    }

    private void OnHostTemplateApplied(object? sender, TemplateAppliedEventArgs e)
    {
        var host = (TopLevel)sender!;
        host.TemplateApplied -= OnHostTemplateApplied;
        if (Parent is AdornerLayer layer)
        {
            layer.Children.Remove(this);
        }
        Install(host);
    }

    private sealed class Entry(NotificationItem item)
    {
        public NotificationItem Item { get; } = item;
        public bool HidesItself { get; init; }
        public bool IsPresent { get; set; }
        public TimeSpan Entering { get; set; }
        public TimeSpan Remaining { get; set; }
    }
}
