using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIKit.Demo.ControlCatalog.Inspector;

/// <summary>
/// The frame a demo card draws over one of its demo's controls (<see cref="Views.DemoCard"/>):
/// dashed around the control the property grid edits, filled over the one the pointer picks.
/// </summary>
public sealed class ElementHighlight : Control
{
    /// <param name="filled">Filled for picking; a dashed frame for the selection.</param>
    public ElementHighlight(bool filled)
    {
        IsFilled = filled;
        IsHitTestVisible = false;
        IsVisible = false;
        // The frame takes the theme's info color.
        ActualThemeVariantChanged += (_, _) => InvalidateVisual();
    }

    /// <summary>Whether the frame is filled.</summary>
    public bool IsFilled { get; }

    /// <summary>The control framed, or null to show nothing.</summary>
    public Control? Target { get; set; }

    /// <summary>Moves the frame over <see cref="Target"/>, as it is laid out in <paramref name="overlay"/>.</summary>
    public void Follow(Canvas overlay)
    {
        var bounds = Target is { IsEffectivelyVisible: true } target && target.TransformToVisual(overlay) is { } transform
            ? new Rect(target.Bounds.Size).TransformToAABB(transform)
            : (Rect?)null;
        IsVisible = bounds is { Width: > 0, Height: > 0 };
        if (bounds is { } rect)
        {
            Canvas.SetLeft(this, rect.X);
            Canvas.SetTop(this, rect.Y);
            Width = rect.Width;
            Height = rect.Height;
        }
    }

    public override void Render(DrawingContext context)
    {
        var color = this.TryFindResource("UIKit.Info.Color", ActualThemeVariant, out var resource) && resource is Color c ? c : Colors.DodgerBlue;
        if (IsFilled)
        {
            context.DrawRectangle(new SolidColorBrush(color, 0.18), new Pen(new SolidColorBrush(color), 1), new Rect(Bounds.Size));
        }
        else
        {
            context.DrawRectangle(null, new Pen(new SolidColorBrush(color), 1.5, new DashStyle([3, 2], 0)), new Rect(Bounds.Size).Inflate(2), 3, 3);
        }
    }
}
