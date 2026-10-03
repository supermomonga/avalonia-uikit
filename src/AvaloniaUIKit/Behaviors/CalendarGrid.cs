using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// Lays a calendar's months grid out in GPUI Kit's three columns: Avalonia's
/// CalendarItem places its twelve buttons in four columns by code. The
/// buttons keep their order; only their Grid cells change. The columns share
/// the width as GPUI's grid does, each edge snapped to the nearest device pixel
/// (a star column's width would be rounded instead).
/// </summary>
public static class CalendarGrid
{
    /// <summary>The number of columns the grid's children wrap into (0: leave them).</summary>
    public static readonly AttachedProperty<int> ColumnsProperty =
        AvaloniaProperty.RegisterAttached<Grid, int>("Columns", typeof(CalendarGrid));

    static CalendarGrid()
    {
        ColumnsProperty.Changed.AddClassHandler<Grid>((grid, e) =>
        {
            grid.Children.CollectionChanged -= OnChildrenChanged;
            grid.SizeChanged -= OnSizeChanged;
            if (e.GetNewValue<int>() > 0)
            {
                grid.Children.CollectionChanged += OnChildrenChanged;
                grid.SizeChanged += OnSizeChanged;
                Arrange(grid);
            }
        });

        void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is Controls children && children.FirstOrDefault()?.Parent is Grid grid)
            {
                Arrange(grid);
            }
        }
    }

    private static void OnSizeChanged(object? sender, SizeChangedEventArgs e) => SnapColumns((Grid)sender!);

    private static void SnapColumns(Grid grid)
    {
        var columns = grid.ColumnDefinitions;
        var n = columns.Count;
        if (n == 0 || grid.Bounds.Width <= 0)
        {
            return;
        }
        var scale = TopLevel.GetTopLevel(grid)?.RenderScaling ?? 1;
        double Snap(double v) => Math.Round(v * scale) / scale;
        var width = (grid.Bounds.Width - grid.ColumnSpacing * (n - 1)) / n;
        for (var i = 0; i < n; i++)
        {
            var left = i * (width + grid.ColumnSpacing);
            var snapped = new GridLength(Snap(left + width) - Snap(left));
            if (columns[i].Width != snapped)
            {
                columns[i].Width = snapped;
            }
        }
    }

    /// <summary>Gets the number of columns.</summary>
    public static int GetColumns(Grid grid) => grid.GetValue(ColumnsProperty);

    /// <summary>Sets the number of columns.</summary>
    public static void SetColumns(Grid grid, int value) => grid.SetValue(ColumnsProperty, value);

    private static void Arrange(Grid grid)
    {
        var columns = GetColumns(grid);
        if (columns <= 0)
        {
            return;
        }
        for (var i = 0; i < grid.Children.Count; i++)
        {
            Grid.SetRow(grid.Children[i], i / columns);
            Grid.SetColumn(grid.Children[i], i % columns);
        }
    }
}
