using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's scrollbar visibility on top of Avalonia's ScrollBar
/// (crates/base/src/scrollbar.rs): a scroll reveals the bars for the 2s idle
/// time in every auto-hide mode, and a bar the pointer moved over while it
/// showed stays until the pointer leaves.
/// </summary>
/// <remarks>
/// ScrollViewer.AllowAutoHide picks between ScrollbarMode::Always (false) and
/// the auto-hiding modes. <see cref="ShowOnHoverProperty"/> picks between them:
/// ScrollbarMode::Hover (true, the default) also shows a bar when the pointer
/// is over it; ScrollbarMode::Scrolling (false) only after a scroll.
/// </remarks>
public static class Scrollbars
{
    /// <summary>The time a bar stays after the last scroll, or after the pointer leaves it.</summary>
    public static readonly TimeSpan Idle = TimeSpan.FromSeconds(2);

    /// <summary>Whether pointing at a hidden bar shows it (ScrollbarMode::Hover). Inherited by the bars.</summary>
    public static readonly AttachedProperty<bool> ShowOnHoverProperty =
        AvaloniaProperty.RegisterAttached<StyledElement, bool>("ShowOnHover", typeof(Scrollbars), true, inherits: true);

    /// <summary>Set on a ScrollBar by its theme to follow its ScrollViewer's scrolling.</summary>
    public static readonly AttachedProperty<bool> TracksScrollingProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, bool>("TracksScrolling", typeof(Scrollbars));

    /// <summary>
    /// Whether the bar shows, and how it came in: the theme styles the bar from
    /// this one property (selectors can test one attached property at a time).
    /// </summary>
    public static readonly AttachedProperty<ScrollbarState> StateProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, ScrollbarState>("State", typeof(Scrollbars));

    /// <summary>
    /// Set on a bar's thumb while the pointer is over where the thumb rests,
    /// which GPUI tests even while a hidden bar has slid away.
    /// </summary>
    public static readonly AttachedProperty<bool> IsThumbHoveredProperty =
        AvaloniaProperty.RegisterAttached<Thumb, bool>("IsThumbHovered", typeof(Scrollbars));

    private static readonly AttachedProperty<Tracker?> TrackerProperty =
        AvaloniaProperty.RegisterAttached<ScrollBar, Tracker?>("Tracker", typeof(Scrollbars));

    static Scrollbars()
    {
        TracksScrollingProperty.Changed.AddClassHandler<ScrollBar>((bar, e) =>
        {
            if (e.GetNewValue<bool>() && bar.GetValue(TrackerProperty) is null)
            {
                bar.SetValue(TrackerProperty, new Tracker(bar));
            }
        });
    }

    /// <summary>Gets whether pointing at a hidden bar shows it.</summary>
    public static bool GetShowOnHover(StyledElement element) => element.GetValue(ShowOnHoverProperty);

    /// <summary>Sets whether pointing at a hidden bar shows it.</summary>
    public static void SetShowOnHover(StyledElement element, bool value) => element.SetValue(ShowOnHoverProperty, value);

    /// <summary>Gets whether the bar follows its ScrollViewer's scrolling.</summary>
    public static bool GetTracksScrolling(ScrollBar bar) => bar.GetValue(TracksScrollingProperty);

    /// <summary>Sets whether the bar follows its ScrollViewer's scrolling.</summary>
    public static void SetTracksScrolling(ScrollBar bar, bool value) => bar.SetValue(TracksScrollingProperty, value);

    /// <summary>Gets whether the pointer is over where the thumb rests.</summary>
    public static bool GetIsThumbHovered(Thumb thumb) => thumb.GetValue(IsThumbHoveredProperty);

    /// <summary>Gets whether the bar shows, and how it came in.</summary>
    public static ScrollbarState GetState(ScrollBar bar) => bar.GetValue(StateProperty);

    private sealed class Tracker
    {
        private readonly ScrollBar _bar;
        private readonly DispatcherTimer _idle;
        private ScrollViewer? _owner;
        private Thumb? _thumb;
        private bool _revealed;
        private bool _hovered;
        private bool _slidesIn;

        public Tracker(ScrollBar bar)
        {
            _bar = bar;
            _idle = new DispatcherTimer(Idle, DispatcherPriority.Normal, (_, _) => OnIdle());
            _idle.Stop();
            bar.TemplateApplied += (_, e) => _thumb = e.NameScope.Find<Thumb>("PART_Thumb");
            bar.AttachedToVisualTree += (_, _) => Attach();
            bar.DetachedFromVisualTree += (_, _) => Detach();
            bar.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: true);
            bar.PointerExited += (_, _) => OnPointerExited();
            bar.PropertyChanged += (_, e) =>
            {
                if (e.Property == ScrollBar.IsExpandedProperty || e.Property == ShowOnHoverProperty || e.Property == ScrollBar.AllowAutoHideProperty)
                {
                    Update();
                }
            };
            Attach();
            Update();
        }

        private bool Shown => _bar.IsExpanded && GetShowOnHover(_bar) || _revealed;

        private void Update()
        {
            if (!Shown)
            {
                _slidesIn = false;
            }
            _bar.SetValue(StateProperty, !Shown ? ScrollbarState.Hidden : _slidesIn ? ScrollbarState.SlidingIn : ScrollbarState.Shown);
        }

        private void Attach()
        {
            if (_owner is not null || _bar.TemplatedParent is not ScrollViewer owner)
            {
                return;
            }
            _owner = owner;
            _owner.ScrollChanged += OnScrollChanged;
        }

        private void Detach()
        {
            if (_owner is not null)
            {
                _owner.ScrollChanged -= OnScrollChanged;
                _owner = null;
            }
            _idle.Stop();
        }

        private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (e.OffsetDelta != default)
            {
                Activity();
            }
        }

        // The pointer counts as over the bar only once it moves there while the
        // bar shows (GPUI tracks hover on a hidden bar in Hover mode only, where
        // ScrollBar.IsExpanded covers it).
        private void OnPointerMoved(object? sender, PointerEventArgs e)
        {
            var onThumb = OnThumb(e.GetPosition(_bar));
            var hoverMode = _bar.AllowAutoHide && GetShowOnHover(_bar);
            if (Shown)
            {
                _hovered = true;
            }
            else if (hoverMode && onThumb)
            {
                // ScrollBar expands on a timer, so this runs before the bar shows.
                _slidesIn = true;
            }
            // GPUI follows the thumb's hover while the bar shows, and always in Hover mode.
            _thumb?.SetValue(IsThumbHoveredProperty, onThumb && (!_bar.AllowAutoHide || hoverMode || Shown));
        }

        // Where the thumb rests, not where the hidden bar has slid it to: GPUI
        // tests the pointer against the thumb's laid-out bounds.
        private bool OnThumb(Point point)
        {
            var rect = _thumb?.Bounds ?? default;
            for (var parent = _thumb?.Parent as Visual; parent is not null && parent != _bar; parent = parent.Parent as Visual)
            {
                rect = rect.Translate(parent.Bounds.Position);
            }
            return rect.Contains(point);
        }

        private void OnPointerExited()
        {
            _thumb?.SetValue(IsThumbHoveredProperty, false);
            if (_hovered)
            {
                _hovered = false;
                Activity();
            }
        }

        /// <summary>A scroll, or the pointer leaving a shown bar: show for the idle time from now.</summary>
        private void Activity()
        {
            _revealed = true;
            _idle.Stop();
            _idle.Start();
            Update();
        }

        private void OnIdle()
        {
            _idle.Stop();
            if (!_hovered)
            {
                _revealed = false;
                Update();
            }
        }
    }
}

/// <summary>Whether a GPUI-style scrollbar shows, and how it came in.</summary>
public enum ScrollbarState
{
    /// <summary>Hidden (an auto-hiding bar at rest).</summary>
    Hidden,

    /// <summary>Shown, faded in where it stands.</summary>
    Shown,

    /// <summary>Shown, slid in from its edge (ScrollbarEntrance::SlideAndFade).</summary>
    SlidingIn,
}
