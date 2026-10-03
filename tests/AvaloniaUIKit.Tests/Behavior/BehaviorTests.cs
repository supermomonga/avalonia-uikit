using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// GPUI Kit behaviors the theme reproduces that a single frame does not show:
/// timing, which input shows focus, and what a disabled control ignores.
/// </summary>
public class BehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    private static bool RingVisible(CaseHost host, Control? within = null) =>
        (within ?? host.Window).GetVisualDescendants().OfType<Border>().Any(b => b.Name == "PART_FocusRing" && b.IsEffectivelyVisible);

    [Test]
    public async Task A_tooltip_opens_500ms_after_the_pointer_arrives()
    {
        var golden = Case("tooltip/text.base/hover+wait-800ms/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "hover");
        VirtualTime.Advance(TimeSpan.FromMilliseconds(499));
        await Assert.That(ToolTip.GetIsOpen(host.Control)).IsFalse();
        VirtualTime.Advance(TimeSpan.FromMilliseconds(1));
        await Assert.That(ToolTip.GetIsOpen(host.Control)).IsTrue();
    }

    // GPUI does not move focus on a pointer press; Avalonia does, without showing
    // focus (:focus but not :focus-visible). Either way no ring shows (R27).
    [Test]
    [Arguments("button/label.default.medium/normal/light")]
    [Arguments("checkbox/label.medium/normal/light")]
    [Arguments("radio/label.medium/normal/light")]
    public async Task A_pointer_press_does_not_show_the_focus_ring(string id)
    {
        var golden = Case(id);
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "click");
        await Assert.That(RingVisible(host)).IsFalse();
    }

    [Test]
    [Arguments("button/label.default.medium/normal/light")]
    [Arguments("checkbox/label.medium/normal/light")]
    [Arguments("radio/label.medium/normal/light")]
    public async Task Keyboard_focus_shows_the_focus_ring(string id)
    {
        var golden = Case(id);
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "focus");
        await Assert.That(RingVisible(host)).IsTrue();
    }

    [Test]
    public async Task A_switch_shows_its_ring_after_a_click()
    {
        var golden = Case("switch/label.medium/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "click");
        await Assert.That(((ToggleSwitch)host.Control).IsChecked == true).IsTrue();
        await Assert.That(RingVisible(host)).IsTrue();
    }

    // GPUI's switch has no drag; Avalonia's does. The spring must not hold the
    // thumb back while the pointer moves it, and travels on from the release.
    [Test]
    public async Task A_dragged_switch_thumb_follows_the_pointer_at_once()
    {
        var golden = Case("switch/label.medium/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var thumb = host.Part<Border>("PART_Thumb");
        double ThumbX() => thumb.TranslatePoint(default, host.Window)!.Value.X;
        var start = ThumbX();
        var at = new Point(start + 8, thumb.TranslatePoint(new Point(0, 8), host.Window)!.Value.Y);
        host.Window.MouseMove(at);
        host.Window.MouseDown(at, MouseButton.Left);
        host.Window.MouseMove(at + new Point(10, 0));
        host.Flush();
        await Assert.That(ThumbX()).IsEqualTo(start + 10);
        host.Window.MouseUp(at + new Point(10, 0), MouseButton.Left);
        host.Flush();
        await Assert.That(((ToggleSwitch)host.Control).IsChecked == true).IsTrue();
        await Assert.That(ThumbX()).IsEqualTo(start + 10);
        VirtualTime.Advance(TimeSpan.FromMilliseconds(600));
        host.Flush();
        await Assert.That(ThumbX()).IsEqualTo(start + 16);
    }

    // GPUI's VirtualList paints the visible rows only; the theme must keep the
    // ListBox's virtualizing panel so Avalonia realizes only those too.
    [Test]
    public async Task A_long_list_realizes_only_its_visible_rows()
    {
        var golden = Case("list/rows.base/normal/light");
        var list = new ListBox { Width = 240, Height = 200, ItemsSource = Enumerable.Range(0, 10000).Select(i => $"Item {i}").ToList() };
        using var host = CaseHost.Open(golden, list);
        await Assert.That(list.ItemsPanelRoot).IsTypeOf<VirtualizingStackPanel>();
        await Assert.That(host.Window.GetVisualDescendants().OfType<ListBoxItem>().Count()).IsLessThan(20);
        host.Drive(golden, "wheel-at-60-60");
        await Assert.That(host.Window.GetVisualDescendants().OfType<ListBoxItem>().Count()).IsLessThan(20);
    }

    // GPUI Kit has no AutoCompleteBox; its suggestions use the Select dropdown's
    // geometry (select/combobox goldens): 6px below the field, as wide as it,
    // 4px of padding, 30.5px medium rows rounded with the theme radius.
    [Test]
    public async Task AutoCompleteBox_suggestions_open_as_the_select_dropdown()
    {
        var golden = Case("select/open.medium/click+wait-200ms/light");
        var box = new AutoCompleteBox
        {
            Width = 200,
            ItemsSource = Adapters.Names(4).ToList(),
            FilterMode = AutoCompleteFilterMode.None,
            MinimumPrefixLength = 0,
        };
        using var host = CaseHost.Open(golden, box);
        box.IsDropDownOpen = true;
        VirtualTime.Advance(TimeSpan.FromMilliseconds(200));
        host.Flush();
        var field = box.GetVisualDescendants().OfType<TextBox>().First();
        var fieldBounds = new Rect(field.TranslatePoint(default, host.Window)!.Value, field.Bounds.Size);
        var popup = box.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.Popup>().First();
        var surface = ((Control)popup.Child!).GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_PopupSurface");
        var items = surface.GetVisualDescendants().OfType<ListBoxItem>().ToList();
        await Assert.That(fieldBounds.Height).IsEqualTo(32);
        await Assert.That(surface.Bounds.Width).IsEqualTo(200);
        await Assert.That(items.Count).IsEqualTo(4);
        await Assert.That(items[0].Bounds.Height).IsEqualTo(30.5);
        await Assert.That(items[0].CornerRadius.TopLeft).IsEqualTo(6);
        await Assert.That(surface.Bounds.Height).IsEqualTo(4 * 30.5 + 8);
    }

    [Test]
    public async Task A_disabled_button_ignores_a_click()
    {
        var golden = Case("button/label.primary.medium/disabled/light");
        var button = (Button)Adapters.Create(golden);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        using var host = CaseHost.Open(golden, button);
        host.Drive(golden, "click");
        await Assert.That(clicks).IsEqualTo(0);
    }

    [Test]
    public async Task Arrow_keys_highlight_a_menu_item_as_hovering_does()
    {
        var golden = Case("dropdown/menu.base/click/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "click");
        var items = host.Window.GetVisualDescendants().OfType<MenuItem>().ToList();
        static bool Lit(MenuItem i) => i.Background is Avalonia.Media.ISolidColorBrush b && b.Color.A > 0;
        host.Window.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        host.Flush();
        await Assert.That(items.Count(Lit)).IsEqualTo(1);
    }

    // GPUI focuses each page button; PipsPager's page list is one tab stop that
    // focuses the current page. Its ring must show whole at the list's edge.
    [Test]
    public async Task A_focused_page_shows_its_whole_ring()
    {
        var golden = Case("pagination/ends.current-1/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "focus");
        var page = host.Window.GetVisualDescendants().OfType<ListBoxItem>().First();
        await Assert.That(page.IsFocused && RingVisible(host, page)).IsTrue();
        var image = host.Capture();
        var left = page.TranslatePoint(default, host.Window)!.Value;
        var band = image.Pixel((int)((left.X - 1.5) * CaseHost.Scale), (int)((left.Y + page.Bounds.Height / 2) * CaseHost.Scale)).ToArray();
        var background = image.Pixel(2, 2).ToArray();
        await Assert.That(band.SequenceEqual(background)).IsFalse();
    }

    // Past MaxVisiblePips PipsPager scrolls its pages (GPUI shows an ellipsis
    // instead). The window must hold whole pages: five, the current one inside.
    [Test]
    public async Task Pages_past_the_visible_count_scroll_whole()
    {
        var golden = Case("pagination/size.medium/normal/light");
        var pager = (PipsPager)Adapters.Create(golden);
        pager.NumberOfPages = 10;
        pager.SelectedPageIndex = 7;
        using var host = CaseHost.Open(golden, pager);
        var list = host.Part<ListBox>("PART_PipsPagerList");
        var pages = host.Window.GetVisualDescendants().OfType<ListBoxItem>().ToList();
        var listLeft = list.TranslatePoint(default, host.Window)!.Value.X + 2;
        double Left(ListBoxItem p) => p.TranslatePoint(default, host.Window)!.Value.X;
        var visible = pages.Where(p => Left(p) >= listLeft - 0.01 && Left(p) + p.Bounds.Width <= listLeft + 176 + 0.01).ToList();
        var cut = pages.Where(p => Left(p) + p.Bounds.Width > listLeft - 3 && Left(p) < listLeft + 179).Except(visible);
        await Assert.That(visible.Count).IsEqualTo(5);
        await Assert.That(cut).IsEmpty();
        await Assert.That(visible.Any(p => p.IsSelected)).IsTrue();
    }

    // GPUI tabs take no focus; Avalonia's do (arrow keys select), so they show
    // the ring on keyboard focus only (R16).
    [Test]
    public async Task A_tab_shows_the_focus_ring_on_keyboard_focus_only()
    {
        var golden = Case("tabs/label.pill.medium/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "click");
        await Assert.That(RingVisible(host)).IsFalse();
        host.Drive(golden, "focus");
        await Assert.That(RingVisible(host)).IsTrue();
    }

    // GPUI removes a closed notification after its 200ms exit; Avalonia's card
    // must report IsClosed (the theme's contract) for the manager to remove it.
    [Test]
    public async Task A_closed_notification_is_removed_after_its_exit()
    {
        var golden = Case("notification/type.info/wait-1000ms/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "wait-1000ms");
        var card = host.Window.GetVisualDescendants().OfType<Avalonia.Controls.Notifications.NotificationCard>().Single();
        card.Close();
        VirtualTime.Advance(TimeSpan.FromMilliseconds(150));
        await Assert.That(card.IsClosed).IsFalse();
        VirtualTime.Advance(TimeSpan.FromMilliseconds(60));
        host.Flush();
        await Assert.That(card.IsClosed).IsTrue();
        await Assert.That(host.Window.GetVisualDescendants().OfType<Avalonia.Controls.Notifications.NotificationCard>()).IsEmpty();
    }

    // GPUI rings a DataTable while it is focused and the last input was a key:
    // a click focuses without the ring, a key after it shows it (Avalonia moves
    // focus between rows, so Tables.ShowsFocusRing follows the input instead).
    [Test]
    [Arguments("datatable/size.medium/normal/light")]
    [Arguments("datagrid/medium.base/at-80-97/light")]
    public async Task A_data_table_shows_the_focus_ring_after_keys_only(string id)
    {
        var golden = Case(id);
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "click-at-80-65");
        await Assert.That(RingVisible(host)).IsFalse();
        host.Drive(golden, "key-down");
        await Assert.That(RingVisible(host)).IsTrue();
        host.Drive(golden, "click-at-80-129");
        await Assert.That(RingVisible(host)).IsFalse();
        host.Drive(golden, "focus");
        await Assert.That(RingVisible(host)).IsTrue();
    }

    // A cell's text overflows from the side it is aligned to when its column
    // narrows (GPUI justifies an overflowing text to that side), rather than
    // centering on the cell at its rounded width.
    [Test]
    public async Task A_narrowed_column_keeps_its_text_at_its_side()
    {
        var golden = Case("datatable/size.medium/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var table = (TableView)host.Control;
        table.Columns[0].Width = new GridLength(40);
        table.Columns[2].Width = new GridLength(40);
        host.Flush();
        var texts = host.Window.GetVisualDescendants().OfType<TextBlock>().ToList();
        var name = texts.Single(t => t.Text == "Barbara");
        var amount = texts.Single(t => t.Text == "$250.00");
        var nameCell = name.FindAncestorOfType<TableViewCell>()!;
        var amountCell = amount.FindAncestorOfType<TableViewCell>()!;
        await Assert.That(name.TranslatePoint(default, nameCell)!.Value.X).IsEqualTo(8);
        await Assert.That(amount.TranslatePoint(new Point(amount.Bounds.Width, 0), amountCell)!.Value.X).IsEqualTo(32);
    }

    // GPUI drops the last row's rule while the rows fill the body, and draws it
    // again once the body grows past them (Tables.IsFilled follows the body).
    [Test]
    public async Task A_data_table_tracks_whether_its_rows_fill_it()
    {
        var golden = Case("datatable/filled.base/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var table = (TableView)host.Control;
        await Assert.That(Tables.GetIsFilled(table)).IsTrue();
        table.Height += 32;
        host.Flush();
        await Assert.That(Tables.GetIsFilled(table)).IsFalse();
    }

    // GPUI rings a focused carousel unless a pointer press focused it.
    [Test]
    public async Task A_carousel_shows_the_focus_ring_on_keyboard_focus_only()
    {
        var golden = Case("carousel/nav.selected-1/normal/light");
        using (var host = CaseHost.Open(golden, Adapters.Create(golden)))
        {
            var carousel = host.Window.GetVisualDescendants().OfType<Carousel>().Single();
            host.Drive(golden, "click-at-184-76");
            await Assert.That(carousel.IsFocused).IsTrue();
            await Assert.That(RingVisible(host, carousel)).IsFalse();
        }
        using (var host = CaseHost.Open(golden, Adapters.Create(golden)))
        {
            var carousel = host.Window.GetVisualDescendants().OfType<Carousel>().Single();
            host.Drive(golden, "focus");
            await Assert.That(carousel.IsFocused).IsTrue();
            await Assert.That(RingVisible(host, carousel)).IsTrue();
        }
    }

    // TitleBar's caption buttons (title_bar.rs ControlIcon): 34 x 33 at the bar's
    // right end with 14px icons, secondary when hovered, danger for close, and
    // restore in place of maximize while maximized. GPUI on macOS draws none, so
    // no golden shows them.
    [Test]
    public async Task Caption_buttons_take_gpui_sizes_and_colors()
    {
        var golden = Case("titlebar/bar.base/normal/light");
        var bar = new DecorationsHost(null) { Width = 480, Height = 34 };
        using var host = CaseHost.Open(golden, bar);
        var states = (IPseudoClasses)bar.Decorations.Classes;
        states.Set(":has-minimize", true);
        states.Set(":has-maximize", true);
        host.Flush();
        var buttons = bar.GetVisualDescendants().OfType<Button>().Where(b => b.IsEffectivelyVisible).ToList();
        await Assert.That(string.Join(",", buttons.Select(b => b.Name))).IsEqualTo("PART_MinimizeButton,PART_MaximizeButton,PART_CloseButton");
        await Assert.That(buttons.Select(b => new Rect(b.TranslatePoint(default, bar)!.Value, b.Bounds.Size))).IsEquivalentTo([new Rect(378, 0, 34, 33), new Rect(412, 0, 34, 33), new Rect(446, 0, 34, 33)]);
        await Assert.That(buttons.SelectMany(b => b.GetVisualDescendants().OfType<PathIcon>()).All(i => i.Bounds.Size == new Size(14, 14))).IsTrue();

        Avalonia.Media.Color Brush(string key) => ((Avalonia.Media.ISolidColorBrush)host.Window.FindResource(host.Window.ActualThemeVariant, key)!).Color;
        Avalonia.Media.Color? Fill(Button b) => (b.Background as Avalonia.Media.ISolidColorBrush)?.Color;
        host.Drive(golden, "at-463-16");
        await Assert.That(Fill(buttons[2])).IsEqualTo(Brush("Gpui.Danger"));
        host.Drive(golden, "at-429-16");
        await Assert.That(Fill(buttons[1])).IsEqualTo(Brush("Gpui.SecondaryHover"));
        await Assert.That(Fill(buttons[2])).IsEqualTo(Avalonia.Media.Colors.Transparent);

        var icon = buttons[1].GetVisualDescendants().OfType<PathIcon>().Single();
        await Assert.That(ReferenceEquals(icon.Data, host.Window.FindResource("Gpui.Icon.WindowMaximize"))).IsTrue();
        states.Set(":maximized", true);
        await Assert.That(ReferenceEquals(icon.Data, host.Window.FindResource("Gpui.Icon.WindowRestore"))).IsTrue();
    }

    // ColorPicker's popover (color_picker.rs): 288px wide, 4px below the swatch from
    // its left edge, with the palette, the picked color and its hex to edit.
    [Test]
    public async Task A_color_picker_opens_its_popover_below_the_swatch()
    {
        // A window wide enough for the popover (the golden case's is not).
        var golden = Case("color_picker/swatch.medium/normal/light") with { Viewport = new Size(480, 480) };
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "click");
        var presenter = host.Window.GetVisualDescendants().OfType<FlyoutPresenter>().Single();
        var picker = (Avalonia.Controls.ColorPicker)host.Control;
        var surface = presenter.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Surface");
        var at = surface.TranslatePoint(default, host.Window)!.Value;
        var swatch = picker.TranslatePoint(default, host.Window)!.Value;
        await Assert.That(surface.Bounds.Width).IsEqualTo(288);
        await Assert.That(at.X).IsEqualTo(swatch.X);
        await Assert.That(at.Y).IsEqualTo(swatch.Y + picker.Bounds.Height + 4);
        var swatches = presenter.GetVisualDescendants().OfType<ListBoxItem>().ToList();
        await Assert.That(swatches.Count).IsEqualTo(picker.PaletteColors!.Count());
        await Assert.That(swatches.All(i => i.Bounds.Size == new Size(20, 20))).IsTrue();
        var hex = presenter.GetVisualDescendants().OfType<TextBox>().Single(t => t.Name == "PART_HexTextBox");
        // ColorView writes the alpha too while it is shown (GPUI only for a translucent color).
        await Assert.That(hex.Text).IsEqualTo("2563EBFF");
    }
}
