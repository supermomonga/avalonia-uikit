using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>GPUI Kit's carousel input on Avalonia's <see cref="Carousel"/>.</summary>
public static class Carousels
{
    /// <summary>
    /// Makes the carousel move as GPUI Kit's does (carousel/scroll_mask.rs,
    /// state.rs): the theme lays the pages out side by side on a
    /// <see cref="CarouselTrack"/>, which follows a mouse, pen or touch drag
    /// and snaps to the nearest page, steps one page per wheel notch, follows
    /// a trackpad scroll and snaps when it ends, and lets a vertical carousel
    /// hand a scroll that starts at an end to its parent. Page changes ride
    /// the track on GPUI's spring, across several pages at once and, with
    /// <c>WrapSelection</c>, around the ends without a jump. The carousel's
    /// own <c>ItemsPanel</c> must be left to the theme.
    /// </summary>
    public static readonly AttachedProperty<bool> TracksPointerProperty =
        AvaloniaProperty.RegisterAttached<Carousel, bool>("TracksPointer", typeof(Carousels));

    /// <summary>Gets whether the carousel follows the pointer as GPUI's does.</summary>
    public static bool GetTracksPointer(Carousel element) => element.GetValue(TracksPointerProperty);

    /// <summary>Sets whether the carousel follows the pointer as GPUI's does.</summary>
    public static void SetTracksPointer(Carousel element, bool value) => element.SetValue(TracksPointerProperty, value);
}
