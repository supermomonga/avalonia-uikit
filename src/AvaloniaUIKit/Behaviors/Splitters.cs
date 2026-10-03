using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's resize handle states (resize_handle.rs): a pressed handle is
/// dragging from the first pointer move on, which the Thumb's pseudo-classes
/// do not tell apart.
/// </summary>
public static class Splitters
{
    /// <summary>Makes <see cref="IsDraggingProperty"/> follow the thumb's drag.</summary>
    public static readonly AttachedProperty<bool> TracksDragProperty =
        AvaloniaProperty.RegisterAttached<Thumb, bool>("TracksDrag", typeof(Splitters));

    /// <summary>Whether the pointer has moved since the thumb was pressed (set by the behavior).</summary>
    public static readonly AttachedProperty<bool> IsDraggingProperty =
        AvaloniaProperty.RegisterAttached<Thumb, bool>("IsDragging", typeof(Splitters));

    static Splitters()
    {
        TracksDragProperty.Changed.AddClassHandler<Thumb>((thumb, e) =>
        {
            thumb.RemoveHandler(Thumb.DragDeltaEvent, OnDragDelta);
            thumb.RemoveHandler(Thumb.DragCompletedEvent, OnDragEnded);
            thumb.RemoveHandler(InputElement.PointerCaptureLostEvent, OnDragEnded);
            if (e.GetNewValue<bool>())
            {
                thumb.AddHandler(Thumb.DragDeltaEvent, OnDragDelta, RoutingStrategies.Bubble, handledEventsToo: true);
                thumb.AddHandler(Thumb.DragCompletedEvent, OnDragEnded, RoutingStrategies.Bubble, handledEventsToo: true);
                thumb.AddHandler(InputElement.PointerCaptureLostEvent, OnDragEnded, RoutingStrategies.Direct, handledEventsToo: true);
            }
            else
            {
                thumb.ClearValue(IsDraggingProperty);
            }
        });
    }

    /// <summary>Gets whether the thumb's drag is tracked.</summary>
    public static bool GetTracksDrag(Thumb thumb) => thumb.GetValue(TracksDragProperty);

    /// <summary>Sets whether the thumb's drag is tracked.</summary>
    public static void SetTracksDrag(Thumb thumb, bool value) => thumb.SetValue(TracksDragProperty, value);

    /// <summary>Gets whether the thumb is being dragged.</summary>
    public static bool GetIsDragging(Thumb thumb) => thumb.GetValue(IsDraggingProperty);

    private static void OnDragDelta(object? sender, RoutedEventArgs e)
    {
        if (sender is Thumb thumb && ReferenceEquals(e.Source, thumb))
        {
            thumb.SetValue(IsDraggingProperty, true);
        }
    }

    private static void OnDragEnded(object? sender, RoutedEventArgs e)
    {
        if (sender is Thumb thumb)
        {
            thumb.ClearValue(IsDraggingProperty);
        }
    }
}
