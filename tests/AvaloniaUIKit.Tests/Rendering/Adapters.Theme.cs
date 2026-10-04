using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Adapters for the theme's support of features the standard controls already
/// have: DropDownButton, and the new parameters of the existing cases.
/// </summary>
public static partial class Adapters
{
    /// <summary>
    /// reference/src/cases/button.rs with `dropdown_caret`: a DropDownButton with the
    /// Button's classes, a fixed width, and the standard menu below it.
    /// </summary>
    private static DropDownButton DropDownButtonCase(GoldenCase c)
    {
        var button = new DropDownButton
        {
            Content = c.Str("label", "Open"),
            IsEnabled = !c.Bool("disabled"),
        };
        ClassFrom(button, c, "variant", "default");
        ClassFrom(button, c, "size", "medium");
        FlagClass(button, c, "outline");
        FlagClass(button, c, "selected");
        if (c.Has("width"))
        {
            button.Width = c.Num("width", 0);
        }
        if (c.Bool("menu"))
        {
            button.Flyout = new MenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft, ItemsSource = StandardMenu() };
        }
        return button;
    }

    /// <summary>
    /// reference/src/cases/number.rs: GPUI's prefix and suffix as InnerLeftContent and
    /// InnerRightContent, text, a small icon, or the story's text xsmall icon button.
    /// </summary>
    private static NumericUpDown NumberAffixes(NumericUpDown number, GoldenCase c)
    {
        if (c.Has("prefix_icon"))
        {
            var icon = Icon(c.Str("prefix_icon"));
            icon.Classes.Add("small");
            number.InnerLeftContent = icon;
        }
        else if (c.Has("prefix"))
        {
            number.InnerLeftContent = c.Str("prefix");
        }
        if (c.Has("suffix_icon"))
        {
            number.InnerRightContent = new Button { Classes = { "text", "xsmall" }, Content = Icon(c.Str("suffix_icon")) };
        }
        else if (c.Has("suffix"))
        {
            number.InnerRightContent = c.Str("suffix");
        }
        return number;
    }

    /// <summary>reference/src/cases/input.rs: `cleanable` as the clearButton class.</summary>
    private static TextBox InputFeatures(TextBox box, GoldenCase c)
    {
        FlagClass(box, c, "cleanable", "clearButton");
        return box;
    }

    /// <summary>
    /// A select case's ComboBox with IsEditable: the field shows the selected
    /// item's text where the title is, in the same frame.
    /// </summary>
    public static ComboBox EditableSelect(GoldenCase c)
    {
        var box = Select(c);
        box.IsEditable = true;
        return box;
    }

    /// <summary>The case's row height by size (sizing.rs table_row_height).</summary>
    private static double TableRowHeight(GoldenCase c) =>
        c.Str("size", "medium") switch { "xsmall" => 26, "small" => 30, "large" => 40, _ => 32 };

    /// <summary>
    /// reference/src/cases/table.rs `rows`: the first rows only (none for the empty
    /// view), a DataTable as tall as the header, those rows and `extra`.
    /// </summary>
    private static TableView TableRows(TableView table, GoldenCase c)
    {
        if (!c.Has("rows"))
        {
            return table;
        }
        var count = (int)c.Num("rows", 5);
        if (table.Theme is null)
        {
            table.ItemsSource = People.Take(count).ToArray();
            table.Height = (c.Bool("borderless") ? 0 : 2) + TableRowHeight(c) * (count + 1) + c.Num("extra", 10);
        }
        else
        {
            table.ItemsSource = Invoices.Take(count).ToArray();
        }
        return table;
    }

    /// <summary>
    /// reference/src/cases/table.rs on DataGrid: `rows` as for the DataTable,
    /// `cell_selectable` as the cell-selectable class, `row_header` as the row
    /// header column and `fixed_columns` as columns that do not resize.
    /// </summary>
    private static DataGrid DataGridCells(DataGrid grid, GoldenCase c)
    {
        if (c.Bool("fixed_columns"))
        {
            grid.CanUserResizeColumns = false;
        }
        if (c.Has("rows"))
        {
            var count = (int)c.Num("rows", 5);
            grid.ItemsSource = new Avalonia.Collections.DataGridCollectionView(People.Take(count).ToArray());
            grid.Height = (c.Bool("borderless") ? 0 : 2) + TableRowHeight(c) * (count + 1) + c.Num("extra", 10);
        }
        FlagClass(grid, c, "cell_selectable", "cell-selectable");
        if (c.Bool("row_header"))
        {
            grid.HeadersVisibility = DataGridHeadersVisibility.All;
        }
        return grid;
    }
}
