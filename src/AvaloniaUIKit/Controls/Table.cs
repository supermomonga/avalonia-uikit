using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Table (table/table.rs): a declarative, display-only table of
/// <see cref="TableHeader"/>, <see cref="TableBody"/>, <see cref="TableFooter"/>
/// and <see cref="TableCaption"/> parts, stacked in the order given, each
/// holding <see cref="TableRow"/>s of <see cref="TableHead"/>s or
/// <see cref="TableCell"/>s. Like GPUI's, it does not virtualize, sort or
/// select, and its rows show no hover.
/// </summary>
/// <remarks>
/// Every row lays its cells out on its own, as GPUI's flex row does
/// (<see cref="TableRowPanel"/>): a cell asks for its <see cref="TableCell.ColSpan"/>
/// times the row's width and all of them shrink in proportion, so the rows'
/// columns line up as long as the spans add up the same. The size classes
/// (<c>xsmall</c>, <c>small</c>, none, <c>large</c>) set the cells' padding;
/// the text is 14px at every size. The class <c>stripe</c> fills every other
/// body row with table_even (the story's stripes); a border is the table's
/// BorderThickness, BorderBrush and CornerRadius (the story's bordered table).
/// </remarks>
public class Table : ItemsControl
{
    static Table()
    {
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<Table>(AutomationControlType.Table);
    }
}

/// <summary>A group of a <see cref="Table"/>'s rows (base/table.rs RowGroup).</summary>
public abstract class TableSection : ItemsControl
{
    static TableSection()
    {
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<TableSection>(AutomationControlType.Group);
    }
}

/// <summary>GPUI Kit's TableHeader: the header rows on table_head, with a rule below.</summary>
public class TableHeader : TableSection
{
}

/// <summary>GPUI Kit's TableBody: the data rows, on the table's own fill.</summary>
public class TableBody : TableSection
{
}

/// <summary>GPUI Kit's TableFooter: the footer rows on table_foot, with a rule above.</summary>
public class TableFooter : TableSection
{
}

/// <summary>
/// GPUI Kit's TableRow: a row of <see cref="TableHead"/>s or
/// <see cref="TableCell"/>s laid out by <see cref="TableRowPanel"/>, with a
/// table_row_border rule above every row of its section but the first.
/// Background fills the row (GPUI's <c>bg</c>).
/// </summary>
public class TableRow : ItemsControl
{
    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new TableRowPanel());

    static TableRow()
    {
        ItemsPanelProperty.OverrideDefaultValue<TableRow>(DefaultPanel);
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<TableRow>(AutomationControlType.DataItem);
    }
}

/// <summary>
/// GPUI Kit's TableCell: a data cell of a <see cref="TableRow"/>, its content
/// centered vertically and placed by HorizontalContentAlignment (GPUI's
/// <c>text_center</c> and <c>text_right</c>), in the table size's padding.
/// </summary>
public class TableCell : ContentControl
{
    /// <summary>How many columns the cell takes (GPUI's <c>col_span</c>, at least 1).</summary>
    public static readonly StyledProperty<int> ColSpanProperty =
        AvaloniaProperty.Register<TableCell, int>(nameof(ColSpan), 1, coerce: (_, v) => Math.Max(1, v));

    /// <summary>
    /// The width the cell asks for in place of its share of the row (GPUI's
    /// <c>w</c>), or NaN. Like GPUI's, it shrinks with the other cells when the
    /// row is too narrow for all of them, down to the cell's minimum width
    /// (MinWidth, or 100 per column). Width, by contrast, never shrinks.
    /// </summary>
    public static readonly StyledProperty<double> PreferredWidthProperty =
        AvaloniaProperty.Register<TableCell, double>(nameof(PreferredWidth), double.NaN);

    /// <inheritdoc cref="ColSpanProperty"/>
    public int ColSpan
    {
        get => GetValue(ColSpanProperty);
        set => SetValue(ColSpanProperty, value);
    }

    /// <inheritdoc cref="PreferredWidthProperty"/>
    public double PreferredWidth
    {
        get => GetValue(PreferredWidthProperty);
        set => SetValue(PreferredWidthProperty, value);
    }
}

/// <summary>GPUI Kit's TableHead: a header cell, laid out and padded as <see cref="TableCell"/>.</summary>
public class TableHead : TableCell
{
    static TableHead()
    {
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<TableHead>(AutomationControlType.HeaderItem);
    }

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(TableCell);
}

/// <summary>GPUI Kit's TableCaption: muted, centered text in the cells' padding, where it is placed among the parts.</summary>
public class TableCaption : ContentControl
{
}

/// <summary>
/// The row layout of <see cref="TableRow"/>, GPUI's flex row of cells
/// (table.rs, laid out by taffy): a cell's flex basis is its
/// <see cref="TableCell.ColSpan"/> times the row's width, or its
/// <see cref="TableCell.PreferredWidth"/>; the cells shrink in proportion to
/// their bases less their padding, none below its minimum (MinWidth, or
/// MIN_CELL_WIDTH 100 per column), and none grows. A cell with a Width keeps
/// it. Every cell is as tall as the row, and the edges between cells fall on
/// device pixels as GPUI rounds them.
/// </summary>
public class TableRowPanel : Panel
{
    /// <summary>table.rs MIN_CELL_WIDTH: a cell's minimum width per column.</summary>
    public const double MinCellWidth = 100;

    private readonly List<double> _widths = [];

    static TableRowPanel()
    {
        AffectsParentMeasure<TableRowPanel>(TableCell.ColSpanProperty, TableCell.PreferredWidthProperty);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var cells = Visible();
        double width;
        if (double.IsInfinity(availableSize.Width))
        {
            // Unbounded: every cell at its preferred or content width, never below its minimum.
            width = 0;
            foreach (var cell in cells)
            {
                cell.Measure(Size.Infinity);
                width += Math.Max(Basis(cell, double.NaN) is var b && double.IsNaN(b) ? cell.DesiredSize.Width : b, Minimum(cell));
            }
        }
        else
        {
            width = availableSize.Width;
        }
        Resolve(cells, width);
        double height = 0;
        for (var i = 0; i < cells.Count; i++)
        {
            cells[i].Measure(new Size(_widths[i], availableSize.Height));
            height = Math.Max(height, cells[i].DesiredSize.Height);
        }
        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var cells = Visible();
        Resolve(cells, finalSize.Width);
        var runs = LayoutSnap.Runs(this, 0, _widths);
        for (var i = 0; i < cells.Count; i++)
        {
            cells[i].Arrange(new Rect(runs[i].Start, 0, runs[i].Length, finalSize.Height));
        }
        return finalSize;
    }

    private List<Control> Visible() => Children.Where(c => c.IsVisible).ToList();

    private static int Span(Control cell) => cell is TableCell c ? c.ColSpan : 1;

    // The cell's own minimum (GPUI's refine_style min_w) or MIN_CELL_WIDTH per column.
    private static double Minimum(Control cell) => cell.MinWidth > 0 ? cell.MinWidth : MinCellWidth * Span(cell);

    // The fixed or preferred width, or the share of a row `width` wide (NaN when unbounded).
    private static double Basis(Control cell, double width) =>
        !double.IsNaN(cell.Width) ? cell.Width
        : cell is TableCell { PreferredWidth: var preferred } && !double.IsNaN(preferred) ? preferred
        : Span(cell) * width;

    private static double PaddingOf(Control cell) =>
        cell is TemplatedControl t ? t.Padding.Left + t.Padding.Right + t.BorderThickness.Left + t.BorderThickness.Right : 0;

    /// <summary>
    /// taffy's resolve_flexible_lengths for a row <paramref name="width"/> wide
    /// (flexbox.rs): no item grows, every item but a fixed one shrinks by its
    /// basis less its padding, and an item that hits its minimum is frozen there.
    /// </summary>
    private void Resolve(List<Control> cells, double width)
    {
        var n = cells.Count;
        var basis = new double[n];
        var inner = new double[n];
        var min = new double[n];
        var shrink = new double[n];
        var target = new double[n];
        var frozen = new bool[n];
        var violation = new double[n];
        double hypothetical = 0;
        for (var i = 0; i < n; i++)
        {
            var padding = PaddingOf(cells[i]);
            basis[i] = Math.Max(Basis(cells[i], width), padding);
            if (double.IsNaN(basis[i]))
            {
                basis[i] = Math.Max(cells[i].DesiredSize.Width, padding);
            }
            inner[i] = basis[i] - padding;
            min[i] = Minimum(cells[i]);
            shrink[i] = double.IsNaN(cells[i].Width) ? 1 : 0;
            target[i] = Math.Max(basis[i], Math.Max(min[i], padding));
            hypothetical += target[i];
        }
        var shrinking = hypothetical > width;
        // An item that cannot flex, or that its minimum already holds above its basis, stays as it is.
        // No item grows: a row wider than its cells keeps them at their bases (the rest stays empty).
        for (var i = 0; i < n; i++)
        {
            frozen[i] = !shrinking || shrink[i] == 0 || basis[i] < target[i];
        }
        double Used() => Enumerable.Range(0, n).Sum(i => frozen[i] ? target[i] : basis[i]);
        var initialFree = width - Used();
        while (frozen.Any(f => !f))
        {
            var used = Used();
            var sumShrink = Enumerable.Range(0, n).Where(i => !frozen[i]).Sum(i => shrink[i]);
            var free = sumShrink < 1 ? Math.Max(initialFree * sumShrink, width - used) : width - used;
            if (free != 0 && double.IsFinite(free))
            {
                var sumScaled = Enumerable.Range(0, n).Where(i => !frozen[i]).Sum(i => inner[i] * shrink[i]);
                if (sumScaled > 0)
                {
                    for (var i = 0; i < n; i++)
                    {
                        if (!frozen[i])
                        {
                            target[i] = basis[i] + free * (inner[i] * shrink[i] / sumScaled);
                        }
                    }
                }
            }
            double total = 0;
            for (var i = 0; i < n; i++)
            {
                if (frozen[i])
                {
                    continue;
                }
                var clamped = Math.Max(Math.Max(target[i], min[i]), 0);
                violation[i] = clamped - target[i];
                target[i] = clamped;
                total += violation[i];
            }
            for (var i = 0; i < n; i++)
            {
                if (!frozen[i])
                {
                    frozen[i] = total > 0 ? violation[i] > 0 : total >= 0 || violation[i] < 0;
                }
            }
        }
        _widths.Clear();
        _widths.AddRange(target);
    }
}
