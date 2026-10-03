using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's carousel page change (carousel.rs CarouselContent): both pages
/// ride one track on a spring from rest, <see cref="Gap"/> apart, so a page
/// travels the viewport's length plus the gap. Each page's offset snaps to
/// device pixels, as GPUI lays the track out.
/// </summary>
/// <remarks>
/// Time comes from an animation on the incoming page's own clock, so the
/// slide moves with every other animation. Without a <see cref="Spring"/>
/// it slides as <see cref="PageSlide"/> does.
/// </remarks>
public class SpringSlide : PageSlide
{
    private static readonly AttachedProperty<double> ClockProperty =
        AvaloniaProperty.RegisterAttached<Visual, double>("Clock", typeof(SpringSlide));

    // Long enough for any spring to settle; it stops as soon as one does.
    private static readonly Animation Clock = new()
    {
        Duration = TimeSpan.FromSeconds(1000),
        Children =
        {
            new KeyFrame { Cue = new Cue(0), Setters = { new Setter(ClockProperty, 0d) } },
            new KeyFrame { Cue = new Cue(1), Setters = { new Setter(ClockProperty, 1000d) } },
        },
    };

    /// <summary>The spring the track rides (GPUI: spring_move with a 0.5px epsilon).</summary>
    public Spring? Spring { get; set; }

    /// <summary>The space between two pages while they move (GPUI: the items' 16px leading padding).</summary>
    public double Gap { get; set; }

    /// <inheritdoc />
    public override async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (Spring is not { } spring)
        {
            await base.Start(from, to, forward, cancellationToken);
            return;
        }
        if (cancellationToken.IsCancellationRequested || (from ?? to) is not { } driver)
        {
            return;
        }
        var parent = GetVisualParent(from, to);
        var horizontal = Orientation == SlideAxis.Horizontal;
        var distance = (horizontal ? parent.Bounds.Width : parent.Bounds.Height) + Gap;
        var scale = TopLevel.GetTopLevel(parent)?.RenderScaling ?? 1;
        var sign = forward ? 1 : -1;
        var fromOffset = Offset(from, horizontal);
        var toOffset = Offset(to, horizontal);
        if (to is not null)
        {
            to.IsVisible = true;
        }

        // The incoming page's distance from rest: it starts a whole page (and gap) away.
        void Show(double remaining)
        {
            var snapped = Math.Round(remaining * scale) / scale;
            fromOffset?.Invoke(-sign * (distance - snapped));
            toOffset?.Invoke(sign * snapped);
        }

        Show(distance);
        using var running = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var settled = new TaskCompletionSource();
        var clocked = to ?? driver;
        void OnClock(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != ClockProperty || running.IsCancellationRequested)
            {
                return;
            }
            var state = spring.Step(new SpringState(distance, 0), 0, e.GetNewValue<double>());
            if (spring.IsSettled(state, 0))
            {
                Show(0);
                settled.TrySetResult();
                return;
            }
            Show(state.Position);
        }

        clocked.PropertyChanged += OnClock;
        using (cancellationToken.Register(() => settled.TrySetResult()))
        {
            _ = Clock.RunAsync(clocked, running.Token);
            await settled.Task;
        }
        clocked.PropertyChanged -= OnClock;
        running.Cancel();
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }
        if (from is not null)
        {
            from.IsVisible = false;
            from.RenderTransform = null;
        }
        if (to is not null)
        {
            to.RenderTransform = null;
        }
    }

    // Sets the page's offset along the axis through its own TranslateTransform.
    private static Action<double>? Offset(Visual? page, bool horizontal)
    {
        if (page is null)
        {
            return null;
        }
        if (page.RenderTransform is not TranslateTransform translate)
        {
            page.RenderTransform = translate = new TranslateTransform();
        }
        return horizontal ? x => translate.X = x : y => translate.Y = y;
    }
}
