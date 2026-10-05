using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What uikit:Table and uikit:ColorSelect do, each with the GPUI Kit rule it
/// ports: the table rows' flex layout (table.rs on taffy); the color picker's
/// popover, preview, commits, hex field, sliders and keys (color_picker.rs,
/// base color_picker.rs).
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

    /// <summary>An open-popover case's window (336 x 480) with <paramref name="select"/> at its anchor.</summary>
    private static CaseHost Open(ColorSelect select) =>
        CaseHost.Open(Case("uikit-colorselect/open-field.base/click/light"), select);

    private static void ClickAt(CaseHost host, Visual target)
    {
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseMove(center);
        host.Window.MouseDown(center, MouseButton.Left);
        host.Window.MouseUp(center, MouseButton.Left);
        host.Flush();
    }

    private static void Key(CaseHost host, PhysicalKey key)
    {
        host.Window.KeyPressQwerty(key, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        host.Flush();
    }

    private static T Part<T>(CaseHost host, string name) where T : Control => host.Part<T>(name);

    // The palette's swatch for blue 300 (row 7, column 8: GPUI's hsl(211.7, 96.4%, 78.4%)).
    private static ColorSwatch Blue300(CaseHost host) =>
        Part<Panel>(host, "PART_Palette").Children.OfType<Panel>().ElementAt(6).Children.OfType<ColorSwatch>().ElementAt(7);

    // color_picker.rs: Popover::w_72 below the trigger (the 0.25rem gap), the
    // featured row of twelve theme colors shrunk to share the row, nine palette rows
    // of eleven 20px swatches, and the value's preview with its hex.
    [Test]
    public async Task A_color_select_opens_its_popover_below_the_trigger()
    {
        var select = new ColorSelect { Color = Color.Parse("#2563EB"), Classes = { "field" }, Width = 200 };
        using var host = Open(select);
        ClickAt(host, select);
        await Assert.That(select.IsDropDownOpen).IsTrue();
        var surface = host.Window.GetVisualDescendants().OfType<FlyoutPresenter>().Single()
            .GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Surface");
        var at = surface.TranslatePoint(default, host.Window)!.Value;
        await Assert.That(surface.Bounds.Width).IsEqualTo(288);
        await Assert.That(at).IsEqualTo(new Point(16, 16 + 32 + 4));
        var featured = Part<Panel>(host, "PART_Featured").GetVisualDescendants().OfType<ColorSwatch>().ToList();
        await Assert.That(featured.Count).IsEqualTo(12);
        await Assert.That(featured.All(s => s.Bounds.Size == new Size(18, 20))).IsTrue();
        var palette = Part<Panel>(host, "PART_Palette").GetVisualDescendants().OfType<ColorSwatch>().ToList();
        await Assert.That(palette.Count).IsEqualTo(99);
        await Assert.That(palette.All(s => s.Bounds.Size == new Size(20, 20))).IsTrue();
        await Assert.That(Part<TextBox>(host, "PART_HexTextBox").Text).IsEqualTo("#2563EB");
    }

    // render_item on_hover: pointing previews the color in the swatch and the hex
    // field below, the value stays; GPUI keeps the preview when the pointer leaves.
    [Test]
    public async Task Pointing_at_a_palette_color_previews_it()
    {
        var select = new ColorSelect { Color = Color.Parse("#2563EB") };
        using var host = Open(select);
        ClickAt(host, select);
        var swatch = Blue300(host);
        var center = swatch.TranslatePoint(new Point(10, 10), host.Window)!.Value;
        host.Window.MouseMove(center);
        host.Flush();
        await Assert.That(select.PreviewColor).IsEqualTo(swatch.Color);
        await Assert.That(select.Color).IsEqualTo(Color.Parse("#2563EB"));
        // GPUI's hex of the palette's HSL color: each channel times 255, truncated.
        await Assert.That(Part<TextBox>(host, "PART_HexTextBox").Text).IsEqualTo("#92C4FD");
        host.Window.MouseMove(new Point(300, 470));
        host.Flush();
        await Assert.That(select.PreviewColor).IsEqualTo(swatch.Color);
    }

    // select_color: a click commits the color and closes the popover; the field shows GPUI's hex.
    [Test]
    public async Task A_click_on_a_palette_color_commits_it_and_closes()
    {
        var select = new ColorSelect { Color = null, Classes = { "field" }, Width = 200 };
        using var host = Open(select);
        ClickAt(host, select);
        var swatch = Blue300(host);
        ClickAt(host, swatch);
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(select.Color).IsEqualTo(swatch.Color);
        await Assert.That(select.DisplayText).IsEqualTo("#92C4FD");
    }

    // HEX_PATTERN: the field takes only "#" and up to eight hex digits; a hex that
    // parses previews; Enter commits it and closes; one that does not leaves all as it is.
    [Test]
    public async Task The_hex_field_previews_and_commits_a_hex()
    {
        var select = new ColorSelect { Color = Color.Parse("#2563EB") };
        using var host = Open(select);
        ClickAt(host, select);
        var hex = Part<TextBox>(host, "PART_HexTextBox");
        ClickAt(host, hex);
        hex.Text = "#2563EBz";
        host.Flush();
        await Assert.That(hex.Text).IsEqualTo("#2563EB");
        hex.Text = "#f0";
        host.Flush();
        Key(host, PhysicalKey.Enter);
        await Assert.That(select.IsDropDownOpen).IsTrue();
        await Assert.That(select.Color).IsEqualTo(Color.Parse("#2563EB"));
        hex.Text = "#f00";
        host.Flush();
        await Assert.That(select.PreviewColor).IsEqualTo(Colors.Red);
        await Assert.That(select.Color).IsEqualTo(Color.Parse("#2563EB"));
        Key(host, PhysicalKey.Enter);
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(select.Color).IsEqualTo(Colors.Red);
        await Assert.That(select.DisplayText).IsEqualTo("#FF0000");
        // The keyboard close gives the trigger its focus back.
        await Assert.That(select.IsFocused).IsTrue();
    }

    // update_value_from_slider: a slider commits all four components and keeps the popover open.
    [Test]
    public async Task A_slider_commits_the_color_and_keeps_the_popover_open()
    {
        var select = new ColorSelect { Color = Color.Parse("#2563EB"), ActiveTab = 1 };
        using var host = Open(select);
        ClickAt(host, select);
        await Assert.That(Part<TextBlock>(host, "PART_HueText").Text).IsEqualTo("221");
        Part<Slider>(host, "PART_Lightness").Value = 0.5;
        host.Flush();
        await Assert.That(select.IsDropDownOpen).IsTrue();
        var hsla = GpuiHsla.From(select.Color!.Value);
        await Assert.That(hsla.L).IsEqualTo(0.5f).Within(0.003f);
        await Assert.That(Part<TextBlock>(host, "PART_LightnessText").Text).IsEqualTo("50");
        await Assert.That(Part<TextBox>(host, "PART_HexTextBox").Text).IsEqualTo(select.DisplayText);
    }

    // base color_picker.rs: Enter (Confirm) on the trigger opens and closes the
    // popover, Escape (Cancel) closes it; the focus goes into it, then back.
    [Test]
    public async Task Enter_and_escape_open_and_close_the_popover()
    {
        var select = new ColorSelect { Color = Color.Parse("#2563EB") };
        using var host = Open(select);
        select.Focus(NavigationMethod.Tab);
        Key(host, PhysicalKey.Enter);
        await Assert.That(select.IsDropDownOpen).IsTrue();
        var focused = TopLevel.GetTopLevel(select)!.FocusManager!.GetFocusedElement();
        await Assert.That(focused is ColorSwatch).IsTrue();
        Key(host, PhysicalKey.Escape);
        await Assert.That(select.IsDropDownOpen).IsFalse();
        await Assert.That(select.IsFocused).IsTrue();
        Key(host, PhysicalKey.Enter);
        Key(host, PhysicalKey.Escape);
        await Assert.That(select.IsDropDownOpen).IsFalse();
    }

    // featured_colors replaces the theme's row; no color shows the placeholder and no preview.
    [Test]
    public async Task Featured_colors_and_no_color()
    {
        var select = new ColorSelect { FeaturedColors = [Colors.Red, Colors.Green], Classes = { "field" }, Width = 200 };
        using var host = Open(select);
        await Assert.That(Part<TextBlock>(host, "PART_Placeholder").IsEffectivelyVisible).IsTrue();
        ClickAt(host, select);
        var featured = Part<Panel>(host, "PART_Featured").GetVisualDescendants().OfType<ColorSwatch>().ToList();
        await Assert.That(featured.Select(s => s.Color)).IsEquivalentTo([Colors.Red, Colors.Green]);
        await Assert.That(featured.All(s => s.Bounds.Size == new Size(20, 20))).IsTrue();
        await Assert.That(Part<Grid>(host, "PART_PreviewRow").IsVisible).IsFalse();
        select.Color = Colors.Blue;
        host.Flush();
        await Assert.That(Part<Grid>(host, "PART_PreviewRow").IsVisible).IsTrue();
        await Assert.That(select.DisplayText).IsEqualTo("#0000FF");
    }

    // A disabled color select does not open.
    [Test]
    public async Task A_disabled_color_select_does_not_open()
    {
        var select = new ColorSelect { Color = Color.Parse("#2563EB"), IsEnabled = false };
        using var host = Open(select);
        ClickAt(host, select);
        await Assert.That(select.IsDropDownOpen).IsFalse();
    }

    // base color_picker.rs parse_hex / hex_string tests.
    [Test]
    [Arguments("#fff", "#FFFFFF")]
    [Arguments("ffffff", "#FFFFFF")]
    [Arguments("#ff000080", "#FF000080")]
    [Arguments("#f008", "#FF000088")]
    public async Task Hex_parses_every_supported_width(string text, string hex) =>
        await Assert.That(GpuiColor.ParseHex(text) is { } c ? GpuiColor.Hex(c) : null).IsEqualTo(hex);

    [Test]
    [Arguments("#nope")]
    [Arguments("#12")]
    [Arguments("#1234567")]
    [Arguments("")]
    [Arguments("#+f0000")]
    [Arguments("#-fffff")]
    public async Task Malformed_hex_does_not_parse(string text) =>
        await Assert.That(GpuiColor.ParseHex(text)).IsNull();

    [Test]
    public async Task Hex_writes_the_alpha_only_while_translucent()
    {
        await Assert.That(new GpuiHsla(0, 1, 0.5f, 1).ToHex()).IsEqualTo("#FF0000");
        await Assert.That(new GpuiHsla(0, 1, 0.5f, 0.5f).ToHex()).IsEqualTo("#FF00007F");
    }
}
