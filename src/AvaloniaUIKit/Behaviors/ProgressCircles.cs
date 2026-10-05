using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>The content of GPUI Kit's ProgressCircle, for a ProgressBar with the UIKitProgressCircle theme.</summary>
public static class ProgressCircles
{
    /// <summary>
    /// What the circle holds (progress_circle.rs children): centered in the
    /// circle under the ring, in the text color, such as the value as a
    /// percentage in a large circle.
    /// </summary>
    public static readonly AttachedProperty<object?> ContentProperty =
        AvaloniaProperty.RegisterAttached<ProgressBar, object?>("Content", typeof(ProgressCircles));

    /// <summary>Gets the circle's content.</summary>
    public static object? GetContent(ProgressBar element) => element.GetValue(ContentProperty);

    /// <summary>Sets the circle's content.</summary>
    public static void SetContent(ProgressBar element, object? value) => element.SetValue(ContentProperty, value);
}
