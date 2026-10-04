using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>How a <see cref="CalendarRow"/> spreads its free width.</summary>
internal enum CalendarJustify
{
    /// <summary>justify_between: equal gaps between the items, none at the ends.</summary>
    Between,
    /// <summary>justify_around: equal space on both sides of every item.</summary>
    Around,
}

/// <summary>
/// GPUI's h_flex (items_center) with justify_between or justify_around: the
/// calendar's header and its row of months. Items keep their own width; with
/// no free width they start at the left and may overflow on the right, as
/// GPUI's seven cells do in a card 2px narrower than them. Edges are snapped
/// to device pixels as GPUI rounds a flex item's bounds.
/// </summary>
internal sealed class CalendarRow : Panel
{
    public CalendarJustify Justify { get; init; }

    protected override Size MeasureOverride(Size availableSize)
    {
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var count = Children.Count;
        if (count == 0)
        {
            return finalSize;
        }
        var free = Math.Max(0, finalSize.Width - Children.Sum(c => c.DesiredSize.Width));
        double gap, at;
        if (Justify == CalendarJustify.Around)
        {
            gap = free / count;
            at = gap / 2;
        }
        else
        {
            gap = count > 1 ? free / (count - 1) : 0;
            at = 0;
        }
        foreach (var child in Children)
        {
            var w = child.DesiredSize.Width;
            var h = child.DesiredSize.Height;
            var left = LayoutSnap.Edge(this, at);
            var top = LayoutSnap.Edge(this, (finalSize.Height - h) / 2);
            child.Arrange(new Rect(left, top, LayoutSnap.Edge(this, at + w) - left, h));
            at += w + gap;
        }
        return finalSize;
    }
}

/// <summary>
/// GPUI's grid().grid_cols(n).gap_x(4): the months and years grids. The
/// columns share the width, each edge snapped to the nearest device pixel;
/// a row is as tall as its tallest item (with its margins).
/// </summary>
internal sealed class CalendarPickerGrid : Panel
{
    public int Columns { get; init; } = 3;

    public double ColumnGap { get; init; } = 4;

    private double ColumnWidth(double width) => Math.Max(0, (width - ColumnGap * (Columns - 1)) / Columns);

    protected override Size MeasureOverride(Size availableSize)
    {
        var column = double.IsFinite(availableSize.Width) ? ColumnWidth(availableSize.Width) : double.PositiveInfinity;
        double widest = 0, height = 0, row = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            child.Measure(new Size(column, double.PositiveInfinity));
            widest = Math.Max(widest, child.DesiredSize.Width);
            row = Math.Max(row, child.DesiredSize.Height);
            if (i % Columns == Columns - 1 || i == Children.Count - 1)
            {
                height += row;
                row = 0;
            }
        }
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : widest * Columns + ColumnGap * (Columns - 1);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var column = ColumnWidth(finalSize.Width);
        double top = 0;
        for (var start = 0; start < Children.Count; start += Columns)
        {
            var end = Math.Min(start + Columns, Children.Count);
            double row = 0;
            for (var i = start; i < end; i++)
            {
                row = Math.Max(row, Children[i].DesiredSize.Height);
            }
            for (var i = start; i < end; i++)
            {
                var x = (i - start) * (column + ColumnGap);
                var left = LayoutSnap.Edge(this, x);
                Children[i].Arrange(new Rect(left, top, LayoutSnap.Edge(this, x + column) - left, row));
            }
            top += row;
        }
        return finalSize;
    }
}
