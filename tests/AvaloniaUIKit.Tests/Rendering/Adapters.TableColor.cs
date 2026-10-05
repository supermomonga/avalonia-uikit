using Avalonia.Controls;
using Avalonia.Layout;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>Adapters for uikit:Table and uikit:ColorSelect (ADR 30).</summary>
public static partial class Adapters
{
    private static TableHead Head(string text, bool right = false, int span = 1) => new()
    {
        Content = text,
        ColSpan = span,
        HorizontalContentAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left,
    };

    private static TableCell Cell(string text, bool right = false, int span = 1) => new()
    {
        Content = text,
        ColSpan = span,
        HorizontalContentAlignment = right ? HorizontalAlignment.Right : HorizontalAlignment.Left,
    };

    private static TableRow Row(params Control[] cells)
    {
        var row = new TableRow();
        foreach (var cell in cells)
        {
            row.Items.Add(cell);
        }
        return row;
    }

    private static T Rows<T>(T section, IEnumerable<TableRow> rows) where T : TableSection
    {
        foreach (var row in rows)
        {
            section.Items.Add(row);
        }
        return section;
    }

    /// <summary>
    /// reference/src/cases/table.rs on uikit:Table: the invoices in three
    /// columns (pixel widths with fixed_widths), a footer, a caption, a head
    /// over two columns above the column heads, or the story's table.
    /// </summary>
    private static Table UikitTableCase(GoldenCase c)
    {
        var table = new Table { Width = c.Num("width", 360), HorizontalAlignment = HorizontalAlignment.Left };
        ClassFrom(table, c, "size", "medium");
        FlagClass(table, c, "stripe");
        if (c.Bool("bordered"))
        {
            table.BorderThickness = new Avalonia.Thickness(1);
            table.CornerRadius = new Avalonia.CornerRadius(5.5);
        }
        if (c.Bool("story"))
        {
            StoryTable(table);
            return table;
        }
        var fixedWidths = c.Bool("fixed_widths");
        double[] widths = [100, 120, 100];
        T Fixed<T>(T cell, int i) where T : TableCell
        {
            if (fixedWidths)
            {
                cell.PreferredWidth = widths[i];
            }
            return cell;
        }
        var header = new TableHeader();
        if (c.Bool("group_header"))
        {
            header.Items.Add(Row(Head("Invoice", span: 2), Head("Total", right: true)));
        }
        header.Items.Add(Row(Fixed(Head("Invoice"), 0), Fixed(Head("Method"), 1), Fixed(Head("Amount", right: true), 2)));
        table.Items.Add(header);
        table.Items.Add(Rows(new TableBody(), Invoices.Select(r =>
            Row(Fixed(Cell(r.Id), 0), Fixed(Cell(r.Method), 1), Fixed(Cell(r.Amount, right: true), 2)))));
        if (c.Bool("footer"))
        {
            table.Items.Add(Rows(new TableFooter(), [Row(Cell("Total", span: 2), Cell("$1,750.00", right: true))]));
        }
        if (c.Bool("caption"))
        {
            table.Items.Add(new TableCaption { Content = "A list of your recent invoices." });
        }
        return table;
    }

    /// <summary>table.rs story: the story's invoices (table_story.rs), the statuses as text.</summary>
    private static readonly string[][] StoryInvoices =
    [
        ["INV001", "Paid", "Credit Card", "$250.00", "2024-01-15"],
        ["INV002", "Pending", "PayPal", "$150.00", "2024-02-01"],
        ["INV003", "Unpaid", "Bank Transfer", "$350.00", "2024-02-15"],
        ["INV004", "Paid", "Credit Card\nMaster Card / Visa", "$450.00", "2024-03-01"],
        ["INV005", "Paid", "PayPal", "$550.00", "2024-03-15"],
        ["INV006", "Pending", "Bank Transfer", "$200.00", "2024-04-01"],
        ["INV007", "Unpaid", "Credit Card", "$300.00", "2024-04-15"],
    ];

    private static void StoryTable(Table table)
    {
        var invoiceHead = Head("Invoice");
        invoiceHead.PreferredWidth = 150;
        table.Items.Add(Rows(new TableHeader(), [Row(invoiceHead, Head("Status", span: 2), Head("Amount", right: true), Head("Date", right: true))]));
        table.Items.Add(Rows(new TableBody(), StoryInvoices.Select(r =>
        {
            var invoice = Cell(r[0]);
            invoice.PreferredWidth = 150;
            return Row(invoice, Cell(r[1]), Cell(r[2]), Cell(r[3], right: true), Cell(r[4], right: true));
        })));
        table.Items.Add(Rows(new TableFooter(), [Row(Cell("Total", span: 3), Cell("$2,250.00", right: true, span: 2))]));
        table.Items.Add(new TableCaption { Content = "A list of your recent invoices." });
    }

    /// <summary>
    /// reference/src/cases/color_picker.rs on uikit:ColorSelect: the swatch (with
    /// a label or an icon) or the field class 200px wide; "none" is no color.
    /// </summary>
    private static ColorSelect ColorSelectCase(GoldenCase c)
    {
        var value = c.Str("value", "2563EB");
        var select = new ColorSelect
        {
            Color = value == "none" ? null : Avalonia.Media.Color.Parse("#" + value),
            Label = c.Has("label") ? c.Str("label") : null,
            Icon = c.Has("icon") ? Icon(c.Str("icon")).Data : null,
        };
        if (c.Has("placeholder"))
        {
            select.PlaceholderText = c.Str("placeholder");
        }
        ClassFrom(select, c, "size", "medium");
        if (c.Bool("field"))
        {
            select.Classes.Add("field");
            select.Width = c.Num("width", 200);
        }
        return select;
    }
}
