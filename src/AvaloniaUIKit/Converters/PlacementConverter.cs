using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUIKit.Converters;

/// <summary>What a <see cref="PlacementConverter"/> derives from a popup's placement.</summary>
public enum PlacementPart
{
    /// <summary>The surface's margin: <see cref="PlacementConverter.Gap"/> on the side facing the target.</summary>
    Gap,
    /// <summary>Whether the placement has a side to point an arrow from.</summary>
    ArrowVisible,
    /// <summary>The arrow box's horizontal alignment on the surface.</summary>
    ArrowHorizontalAlignment,
    /// <summary>The arrow box's vertical alignment on the surface.</summary>
    ArrowVerticalAlignment,
    /// <summary>The arrow box's margin: its center on the surface edge, clear of the corner.</summary>
    ArrowMargin,
    /// <summary>The arrow triangle, closed, in its 12px box.</summary>
    ArrowFill,
    /// <summary>The arrow's two slopes, open, in its 12px box.</summary>
    ArrowStroke,
    /// <summary>The width of the patch that joins the arrow to the surface across its ring.</summary>
    PatchWidth,
    /// <summary>The height of that patch.</summary>
    PatchHeight,
}

/// <summary>
/// A popup's side and alignment from its <see cref="PlacementMode"/>, for a
/// surface drawn as GPUI Kit's Popover draws it (popover.rs arrow_points): the
/// gap toward the target and an arrow on the facing edge. The arrow is 6px
/// deep and 12px wide, 12px (radius plus half its width) from the edge's start
/// or end when the placement aligns that edge, centered otherwise. Placements
/// without a side (Pointer, Center, AnchorAndGravity, Custom) get no gap or arrow.
/// </summary>
public sealed class PlacementConverter : IValueConverter
{
    private const double Half = 6;
    private const double Inset = 12;

    /// <summary>What to derive.</summary>
    public PlacementPart Part { get; set; }

    /// <summary>The gap between the target and the surface.</summary>
    public double Gap { get; set; } = 4;

    private enum Side { None, Top, Bottom, Left, Right }

    private enum Along { Start, Center, End }

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Side: the surface edge facing the target. Below the target, it is the top edge.
        var (side, along) = value is PlacementMode mode ? mode switch
        {
            PlacementMode.Bottom => (Side.Top, Along.Center),
            PlacementMode.BottomEdgeAlignedLeft => (Side.Top, Along.Start),
            PlacementMode.BottomEdgeAlignedRight => (Side.Top, Along.End),
            PlacementMode.Top => (Side.Bottom, Along.Center),
            PlacementMode.TopEdgeAlignedLeft => (Side.Bottom, Along.Start),
            PlacementMode.TopEdgeAlignedRight => (Side.Bottom, Along.End),
            PlacementMode.Right => (Side.Left, Along.Center),
            PlacementMode.RightEdgeAlignedTop => (Side.Left, Along.Start),
            PlacementMode.RightEdgeAlignedBottom => (Side.Left, Along.End),
            PlacementMode.Left => (Side.Right, Along.Center),
            PlacementMode.LeftEdgeAlignedTop => (Side.Right, Along.Start),
            PlacementMode.LeftEdgeAlignedBottom => (Side.Right, Along.End),
            _ => (Side.None, Along.Center),
        } : (Side.None, Along.Center);
        var vertical = side is Side.Left or Side.Right;
        return Part switch
        {
            PlacementPart.Gap => side switch
            {
                Side.Top => new Thickness(0, Gap, 0, 0),
                Side.Bottom => new Thickness(0, 0, 0, Gap),
                Side.Left => new Thickness(Gap, 0, 0, 0),
                Side.Right => new Thickness(0, 0, Gap, 0),
                _ => default,
            },
            PlacementPart.ArrowVisible => side != Side.None,
            PlacementPart.ArrowHorizontalAlignment => side switch
            {
                Side.Left => HorizontalAlignment.Left,
                Side.Right => HorizontalAlignment.Right,
                _ => along switch { Along.Start => HorizontalAlignment.Left, Along.End => HorizontalAlignment.Right, _ => HorizontalAlignment.Center },
            },
            PlacementPart.ArrowVerticalAlignment => side switch
            {
                Side.Top => VerticalAlignment.Top,
                Side.Bottom => VerticalAlignment.Bottom,
                _ => along switch { Along.Start => VerticalAlignment.Top, Along.End => VerticalAlignment.Bottom, _ => VerticalAlignment.Center },
            },
            // The box is 12px square, centered on the edge where the arrow's base lies.
            PlacementPart.ArrowMargin => (side, along) switch
            {
                (Side.None, _) => default,
                (Side.Top, _) => new Thickness(along == Along.Start ? Inset - Half : 0, -Half, along == Along.End ? Inset - Half : 0, 0),
                (Side.Bottom, _) => new Thickness(along == Along.Start ? Inset - Half : 0, 0, along == Along.End ? Inset - Half : 0, -Half),
                (Side.Left, _) => new Thickness(-Half, along == Along.Start ? Inset - Half : 0, 0, along == Along.End ? Inset - Half : 0),
                _ => new Thickness(0, along == Along.Start ? Inset - Half : 0, -Half, along == Along.End ? Inset - Half : 0),
            },
            PlacementPart.ArrowFill => Geometry.Parse(Triangle(side) + " Z"),
            PlacementPart.ArrowStroke => Geometry.Parse(Triangle(side)),
            PlacementPart.PatchWidth => vertical ? 2d : 10d,
            PlacementPart.PatchHeight => vertical ? 10d : 2d,
            _ => null,
        };
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    // The triangle's base runs through the box's center line; its tip is on the box's edge.
    private static string Triangle(Side side) => side switch
    {
        Side.Bottom => "M0,6 L6,12 L12,6",
        Side.Left => "M6,0 L0,6 L6,12",
        Side.Right => "M6,0 L12,6 L6,12",
        _ => "M0,6 L6,0 L12,6",
    };
}
