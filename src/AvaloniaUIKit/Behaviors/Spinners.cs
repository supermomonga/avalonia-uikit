using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>The icon of GPUI Kit's Spinner, for a ProgressBar with the UIKitSpinner theme.</summary>
public static class Spinners
{
    /// <summary>
    /// The glyph that turns (spinner.rs icon): an icon geometry such as
    /// <c>{StaticResource UIKit.Icon.LoaderCircle}</c>. The UIKitSpinner theme
    /// sets the Loader icon, GPUI's default.
    /// </summary>
    public static readonly AttachedProperty<Geometry?> IconProperty =
        AvaloniaProperty.RegisterAttached<ProgressBar, Geometry?>("Icon", typeof(Spinners));

    /// <summary>Gets the spinner's icon.</summary>
    public static Geometry? GetIcon(ProgressBar element) => element.GetValue(IconProperty);

    /// <summary>Sets the spinner's icon.</summary>
    public static void SetIcon(ProgressBar element, Geometry? value) => element.SetValue(IconProperty, value);
}
