using Avalonia;
using Avalonia.Animation;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>Motion helpers the theme's templates attach to their parts.</summary>
public static class Motion
{
    /// <summary>
    /// Transitions that take effect once the element has been laid out for the
    /// first time. Values a control settles into while it loads (a progress
    /// bar's indicator width) then appear at once, as GPUI animates changes
    /// but not a component's first frame.
    /// </summary>
    public static readonly AttachedProperty<Transitions?> SettledTransitionsProperty =
        AvaloniaProperty.RegisterAttached<Layoutable, Transitions?>("SettledTransitions", typeof(Motion));

    static Motion()
    {
        SettledTransitionsProperty.Changed.AddClassHandler<Layoutable>(OnSettledTransitionsChanged);
    }

    /// <summary>Gets the transitions <paramref name="element"/> takes on after its first layout.</summary>
    public static Transitions? GetSettledTransitions(Layoutable element) => element.GetValue(SettledTransitionsProperty);

    /// <summary>Sets the transitions <paramref name="element"/> takes on after its first layout.</summary>
    public static void SetSettledTransitions(Layoutable element, Transitions? value) => element.SetValue(SettledTransitionsProperty, value);

    private static void OnSettledTransitionsChanged(Layoutable element, AvaloniaPropertyChangedEventArgs e)
    {
        if (element.IsArrangeValid && element.IsAttachedToVisualTree())
        {
            element.Transitions = GetSettledTransitions(element);
            return;
        }
        element.LayoutUpdated += OnFirstLayout;

        void OnFirstLayout(object? sender, EventArgs args)
        {
            element.LayoutUpdated -= OnFirstLayout;
            element.Transitions = GetSettledTransitions(element);
        }
    }
}
