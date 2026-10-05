using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What uikit:Table and uikit:ColorSelect do, each with the GPUI Kit rule it
/// ports: the table rows' flex layout (table.rs on taffy).
/// </summary>
public class TableColorBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    /// <summary>A table <paramref name="width"/> wide with one body row of <paramref name="cells"/>.</summary>
    private static (CaseHost Host, TableCell[] Cells) Row(double width, params TableCell[] cells)
    {
        var row = new TableRow();
        foreach (var cell in cells)
        {
            row.Items.Add(cell);
        }
        var body = new TableBody();
        body.Items.Add(row);
        var table = new Table { Width = width, HorizontalAlignment = HorizontalAlignment.Left };
        table.Items.Add(body);
        var golden = Case("uikit-table/size.medium/normal/light") with { Viewport = new Size(width + 32, 120) };
        return (CaseHost.Open(golden, table), cells);
    }

    private static TableCell Cell(int span = 1, double preferred = double.NaN, double width = double.NaN) =>
        new() { Content = "x", ColSpan = span, PreferredWidth = preferred, Width = width };

    private static async Task WidthsAre(TableCell[] cells, params double[] widths)
    {
        for (var i = 0; i < cells.Length; i++)
        {
            await Assert.That(cells[i].Bounds.Width).IsEqualTo(widths[i]).Within(0.51);
        }
    }

    // table.rs: a cell's flex basis is col_span x the row's width (relative(col_span)),
    // taffy shrinks every cell by its basis less its padding, and MIN_CELL_WIDTH (100 per
    // column) holds w(150) up: the story's Status head over two columns is 223.5 wide
    // while the two body cells under it are 115 each.
    [Test]
    public async Task A_row_shares_its_width_by_col_span_as_gpui_flex_does()
    {
        var (header, heads) = Row(560, Cell(preferred: 150), Cell(span: 2), Cell(), Cell());
        using (header)
        {
            await WidthsAre(heads, 100, 223.5, 118.25, 118.25);
        }
        var (body, cells) = Row(560, Cell(preferred: 150), Cell(), Cell(), Cell(), Cell());
        using (body)
        {
            await WidthsAre(cells, 100, 115, 115, 115, 115);
        }
    }

    // A preferred width shrinks with the others; Width keeps the cell rigid (flex-shrink 0).
    [Test]
    public async Task A_fixed_width_cell_keeps_its_width_while_the_others_shrink()
    {
        var (host, cells) = Row(400, Cell(width: 150), Cell(), Cell());
        using (host)
        {
            await WidthsAre(cells, 150, 125, 125);
        }
    }

    // No cell grows: preferred widths that fit leave the rest of the row empty (the fixed case).
    [Test]
    public async Task Cells_whose_widths_fit_keep_them()
    {
        var (host, cells) = Row(360, Cell(preferred: 100), Cell(preferred: 120), Cell(preferred: 100));
        using (host)
        {
            await WidthsAre(cells, 100, 120, 100);
            await Assert.That(cells[2].Bounds.Right).IsEqualTo(320);
        }
    }

    // MIN_CELL_WIDTH: a row too narrow for its cells overflows; the table clips it.
    [Test]
    public async Task Cells_never_shrink_below_their_minimum()
    {
        var (host, cells) = Row(240, Cell(), Cell(span: 2));
        using (host)
        {
            await WidthsAre(cells, 100, 200);
            var content = host.Control.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Content");
            await Assert.That(content.ClipToBounds).IsTrue();
        }
    }

    // Without a width to fill (a horizontal StackPanel), a row is as wide as its cells' content, at least their minimums.
    [Test]
    public async Task An_unbounded_table_is_as_wide_as_its_cells()
    {
        var row = new TableRow { Items = { Cell(), new TableCell { Content = new Border { Width = 180, Height = 10 } } } };
        var table = new Table { Items = { new TableBody { Items = { row } } } };
        var golden = Case("uikit-table/size.medium/normal/light");
        using var host = CaseHost.Open(golden, new StackPanel { Orientation = Orientation.Horizontal, Children = { table } });
        // 100 (the minimum) + 180 and the 16px padding.
        await Assert.That(table.Bounds.Width).IsEqualTo(296);
    }
}
