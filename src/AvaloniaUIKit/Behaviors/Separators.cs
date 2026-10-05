using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>The label GPUI Kit's Separator can carry.</summary>
public static class Separators
{
    /// <summary>
    /// A label on the line (separator.rs label): small muted text, centered on
    /// the line over the background. A labeled separator takes the label's
    /// height (or width, vertical) and has the :labeled pseudo-class.
    /// </summary>
    public static readonly AttachedProperty<string?> LabelProperty =
        AvaloniaProperty.RegisterAttached<Separator, string?>("Label", typeof(Separators));

    static Separators()
    {
        LabelProperty.Changed.AddClassHandler<Separator>((separator, e) =>
            ((IPseudoClasses)separator.Classes).Set(":labeled", e.GetNewValue<string?>() is not null));
    }

    /// <summary>Gets the separator's label.</summary>
    public static string? GetLabel(Separator element) => element.GetValue(LabelProperty);

    /// <summary>Sets the separator's label.</summary>
    public static void SetLabel(Separator element, string? value) => element.SetValue(LabelProperty, value);
}
