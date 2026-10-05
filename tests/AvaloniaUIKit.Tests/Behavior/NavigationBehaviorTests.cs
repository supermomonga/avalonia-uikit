using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What the navigation controls do beyond their look, with GPUI Kit's rules:
/// uikit:Pagination (pagination.rs, gpui_base pagination.rs), the carousel's
/// buttons and track (carousel/carousel.rs, scroll_mask.rs, state.rs), the tab
/// bar's menu, slots and scrolling row (tab_bar.rs), and the slots of the
/// GroupBox, Separator, Spinner and ProgressCircle themes.
/// </summary>
public class NavigationBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    private static Point Center(CaseHost host, Visual control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), host.Window)!.Value;

    private static void Click(CaseHost host, GoldenCase golden, Visual control)
    {
        var at = Center(host, control);
        host.Drive(golden, $"click-at-{at.X:0.##}-{at.Y:0.##}");
    }

    private static void Advance(CaseHost host, int ms)
    {
        VirtualTime.Advance(TimeSpan.FromMilliseconds(ms));
        host.Flush();
    }

    // ---- Pagination ----

    private static string Items(IEnumerable<PaginationItem> items) =>
        string.Join(" ", items.Select(i => i.IsEllipsis ? $"…{i.Start}-{i.End}" : i.Start.ToString(System.Globalization.CultureInfo.InvariantCulture)));

    // gpui_base pagination.rs creates_pages_and_navigable_ellipsis_ranges, and the ends of calculate_items.
    [Test]
    public async Task Pagination_shows_the_pages_gpui_calculates()
    {
        await Assert.That(Items(Pagination.CalculateItems(5, 10, 7))).IsEqualTo("1 …2-3 3 4 5 6 7 …8-10 10");
        await Assert.That(Items(Pagination.CalculateItems(1, 10, 5))).IsEqualTo("1 2 3 …4-10 10");
        await Assert.That(Items(Pagination.CalculateItems(10, 10, 5))).IsEqualTo("1 …2-8 8 9 10");
        await Assert.That(Items(Pagination.CalculateItems(3, 5, 5))).IsEqualTo("1 2 3 4 5");
        // visible_pages is at least 5; a single page shows no page buttons.
        await Assert.That(Items(Pagination.CalculateItems(5, 10, 2))).IsEqualTo(Items(Pagination.CalculateItems(5, 10, 5)));
        await Assert.That(Pagination.CalculateItems(1, 1, 5).Count).IsEqualTo(0);
    }

    // pagination.rs test_ellipsis_menu_pages.
    [Test]
    public async Task An_ellipsis_menu_lists_at_most_the_100_pages_nearest_the_current_one()
    {
        await Assert.That(Pagination.EllipsisMenuPages(2, 8, 1)).IsEqualTo((2, 8));
        await Assert.That(Pagination.EllipsisMenuPages(2, 8, 9)).IsEqualTo((2, 8));
        await Assert.That(Pagination.EllipsisMenuPages(4, 10_000, 1)).IsEqualTo((4, 104));
        await Assert.That(Pagination.EllipsisMenuPages(2, 9_997, 10_000)).IsEqualTo((9_897, 9_997));
        await Assert.That(Pagination.EllipsisMenuPages(5_003, 10_000, 5_000)).IsEqualTo((5_003, 5_103));
        await Assert.That(Pagination.EllipsisMenuPages(2, 4_998, 5_000)).IsEqualTo((4_898, 4_998));
    }

    // gpui_base pagination.rs clamps_controlled_values_and_navigation_boundaries and
    // validates_every_page_change_request.
    [Test]
    public async Task Pagination_keeps_its_page_in_range_and_checks_every_request()
    {
        var golden = Case("uikit-pagination/size.medium/normal/light");
        var pagination = Adapters.PaginationCase(golden);
        using var host = CaseHost.Open(golden, pagination);
        var requested = new List<int>();
        pagination.PageChanged += (_, e) => requested.Add(e.NewPage);

        pagination.TotalPages = 0;
        await Assert.That((pagination.TotalPages, pagination.CurrentPage)).IsEqualTo((1, 1));
        pagination.TotalPages = 10;
        pagination.CurrentPage = 20;
        await Assert.That(pagination.CurrentPage).IsEqualTo(10);
        host.Flush();
        await Assert.That(host.Part<Button>("PART_PreviousButton").IsEnabled).IsTrue();
        await Assert.That(host.Part<Button>("PART_NextButton").IsEnabled).IsFalse();

        pagination.CurrentPage = 3;
        pagination.TotalPages = 5;
        foreach (var page in new[] { 3, 0, 6 })
        {
            await Assert.That(pagination.RequestPage(page)).IsFalse();
        }
        await Assert.That(requested).IsEmpty();
        await Assert.That(pagination.RequestPage(4)).IsTrue();
        await Assert.That((pagination.CurrentPage, requested.Single())).IsEqualTo((4, 4));
        pagination.IsEnabled = false;
        await Assert.That(pagination.RequestPage(2)).IsFalse();
        await Assert.That(pagination.CurrentPage).IsEqualTo(4);
    }

    [Test]
    public async Task Clicks_on_a_page_previous_and_next_change_the_page()
    {
        var golden = Case("uikit-pagination/ellipsis.current-5/normal/light");
        var pagination = Adapters.PaginationCase(golden);
        using var host = CaseHost.Open(golden, pagination);
        var changes = new List<(int, int)>();
        pagination.PageChanged += (_, e) => changes.Add((e.OldPage, e.NewPage));
        Button Page(string label) => host.Window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, label));

        Click(host, golden, Page("6"));
        Click(host, golden, host.Part<Button>("PART_PreviousButton"));
        Click(host, golden, host.Part<Button>("PART_NextButton"));
        Click(host, golden, host.Part<Button>("PART_NextButton"));
        // The current page's button does nothing.
        Click(host, golden, Page("7"));
        await Assert.That(changes).IsEquivalentTo(new[] { (5, 6), (6, 5), (5, 6), (6, 7) });
        await Assert.That(Items(pagination.Items)).IsEqualTo("1 …2-6 6 7 8 …9-10 10");
    }

    // pagination.rs: the ellipsis menu (min_w 55, max_h 240, scrollable) jumps to a hidden page.
    [Test]
    public async Task A_long_ellipsis_menu_lists_100_pages_in_240px_that_scroll()
    {
        // The long case's pages, in a window with room for the menu.
        var golden = Case("uikit-pagination/long.base/normal/light") with { Viewport = new Size(520, 320) };
        var pagination = Adapters.PaginationCase(golden);
        using var host = CaseHost.Open(golden, pagination);
        var ellipsis = host.Window.GetVisualDescendants().OfType<Button>().Where(b => b.Content is PathIcon).Last();
        Click(host, golden, ellipsis);
        Advance(host, 200);
        var entries = host.Window.GetVisualDescendants().OfType<MenuItem>().ToList();
        await Assert.That(entries.Count).IsEqualTo(100);
        await Assert.That((entries[0].Header, entries[^1].Header)).IsEqualTo(("502", "601"));
        await Assert.That(entries.Any(e => e.IsChecked)).IsFalse();
        var surface = host.Window.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Surface");
        await Assert.That(surface.Bounds.Height).IsEqualTo(240);
        await Assert.That(surface.Bounds.Width).IsGreaterThanOrEqualTo(55);
        var scroller = surface.GetVisualDescendants().OfType<ScrollViewer>().First();
        await Assert.That(scroller.Extent.Height).IsGreaterThan(scroller.Viewport.Height);

        Click(host, golden, entries[1]);
        await Assert.That(pagination.CurrentPage).IsEqualTo(503);
    }

    [Test]
    public async Task Compact_pagination_shows_previous_and_next_only()
    {
        var golden = Case("uikit-pagination/compact.medium/normal/light");
        var pagination = Adapters.PaginationCase(golden);
        using var host = CaseHost.Open(golden, pagination);
        await Assert.That(pagination.Items.Count).IsEqualTo(0);
        await Assert.That(host.Part<Panel>("PART_Pages").IsVisible).IsFalse();
        Click(host, golden, host.Part<Button>("PART_NextButton"));
        await Assert.That(pagination.CurrentPage).IsEqualTo(4);
    }

    // ---- Carousel ----

    private static (CaseHost Host, Carousel Carousel, CarouselTrack Track) OpenCarousel(GoldenCase golden, Action<Carousel>? configure = null)
    {
        var panel = Adapters.UikitCarouselCase(golden);
        var carousel = panel.GetLogicalDescendants().OfType<Carousel>().Single();
        configure?.Invoke(carousel);
        var host = CaseHost.Open(golden, panel);
        return (host, carousel, host.Window.GetVisualDescendants().OfType<CarouselTrack>().Single());
    }

    private static CarouselButton Nav<T>(CaseHost host) where T : CarouselButton =>
        host.Window.GetVisualDescendants().OfType<T>().Single();

    [Test]
    public async Task The_carousel_buttons_stop_at_the_ends_unless_the_carousel_wraps()
    {
        var golden = Case("uikit-carousel/nav.selected-0/normal/light");
        var (host, carousel, _) = OpenCarousel(golden);
        using var _h = host;
        var previous = Nav<CarouselPrevious>(host);
        var next = Nav<CarouselNext>(host);
        await Assert.That((previous.IsEffectivelyEnabled, next.IsEffectivelyEnabled)).IsEqualTo((false, true));
        carousel.SelectedIndex = 2;
        await Assert.That((previous.IsEffectivelyEnabled, next.IsEffectivelyEnabled)).IsEqualTo((true, false));
        carousel.WrapSelection = true;
        await Assert.That((previous.IsEffectivelyEnabled, next.IsEffectivelyEnabled)).IsEqualTo((true, true));
        Click(host, golden, next);
        await Assert.That(carousel.SelectedIndex).IsEqualTo(0);
        next.Carousel = null;
        await Assert.That(next.IsEffectivelyEnabled).IsFalse();
    }

    // carousel.rs focus_after_pointer_click: the arrow keys go on paging, without a ring.
    [Test]
    public async Task A_click_on_next_pages_and_gives_the_carousel_focus_without_a_ring()
    {
        var golden = Case("uikit-carousel/nav.selected-0/normal/light");
        var (host, carousel, track) = OpenCarousel(golden);
        using var _h = host;
        Click(host, golden, Nav<CarouselNext>(host));
        await Assert.That(carousel.SelectedIndex).IsEqualTo(1);
        await Assert.That(carousel.IsFocused).IsTrue();
        await Assert.That(host.Part<Border>("PART_FocusRing").IsEffectivelyVisible).IsFalse();
        host.PressKey("right");
        Advance(host, 1000);
        await Assert.That(carousel.SelectedIndex).IsEqualTo(2);
        await Assert.That(track.TrackOffset).IsEqualTo(2 * 256);
    }

    // state.rs update_drag / finish_drag: 2px to lock, then the pointer, then the nearest slide.
    [Test]
    public async Task A_drag_follows_the_pointer_and_selects_the_nearest_slide()
    {
        var golden = Case("uikit-carousel/drag.base/pressed-at-200-76+drag-at-196-76+drag-at-120-76/light");
        var (host, carousel, track) = OpenCarousel(golden);
        using var _h = host;
        host.Drive(golden, "pressed-at-200-76+drag-at-198-76");
        await Assert.That(track.TrackOffset).IsEqualTo(0);
        host.Drive(golden, "drag-at-196-76");
        await Assert.That((track.TrackOffset, track.IsInteracting)).IsEqualTo((4d, true));
        host.Drive(golden, "drag-at-130-76+release+wait-1000ms");
        await Assert.That((carousel.SelectedIndex, track.TrackOffset)).IsEqualTo((0, 0d));

        host.Drive(golden, "pressed-at-200-76+drag-at-196-76+drag-at-60-76");
        await Assert.That(track.TrackOffset).IsEqualTo(140);
        host.Drive(golden, "release");
        await Assert.That(carousel.SelectedIndex).IsEqualTo(1);
        Advance(host, 1000);
        await Assert.That(track.TrackOffset).IsEqualTo(256);

        // The track does not go past its ends.
        carousel.SelectedIndex = 0;
        Advance(host, 1000);
        host.Drive(golden, "pressed-at-100-76+drag-at-104-76+drag-at-200-76");
        await Assert.That(track.TrackOffset).IsEqualTo(0);
        host.Drive(golden, "release");
    }

    // scroll_mask.rs: a drag along the axis or across it takes the press from the slide's button.
    [Test]
    public async Task A_drag_clicks_nothing_on_the_slide_and_a_drag_across_the_axis_is_given_up()
    {
        var golden = Case("uikit-carousel/nav.selected-0/normal/light");
        var clicks = 0;
        var (host, carousel, track) = OpenCarousel(golden, c =>
        {
            var slide = (Border)((IList<Border>)c.ItemsSource!)[0];
            var button = new Button { Content = "Open", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center };
            button.Click += (_, _) => clicks++;
            slide.Child = button;
        });
        using var _h = host;
        var at = Center(host, host.Window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, "Open")));
        string P(double dx, double dy) => $"{at.X + dx:0.##}-{at.Y + dy:0.##}";

        host.Drive(golden, $"click-at-{P(0, 0)}");
        await Assert.That(clicks).IsEqualTo(1);
        host.Drive(golden, $"pressed-at-{P(0, 0)}+drag-at-{P(-4, 0)}+drag-at-{P(-20, 0)}+release");
        await Assert.That(clicks).IsEqualTo(1);
        Advance(host, 1000);
        host.Drive(golden, $"pressed-at-{P(0, 0)}+drag-at-{P(1, 6)}+drag-at-{P(-30, 10)}+release");
        await Assert.That((clicks, track.TrackOffset, carousel.SelectedIndex)).IsEqualTo((1, 0d, 0));
    }

    // state.rs handle_wheel_step: one notch, one slide; the rest of its burst stays here.
    [Test]
    public async Task A_wheel_notch_steps_one_slide_and_keeps_the_rest_of_its_burst()
    {
        var golden = Case("uikit-carousel/nav.selected-0/normal/light");
        var (host, carousel, _) = OpenCarousel(golden);
        using var _h = host;
        var at = new Point(184, 76);
        void Wheel(double dx, double dy)
        {
            host.Window.MouseWheel(at, new Vector(dx, dy));
            host.Flush();
        }

        // A horizontal carousel ignores a vertical wheel.
        Wheel(0, -1);
        await Assert.That(carousel.SelectedIndex).IsEqualTo(0);
        Wheel(-1, 0);
        Wheel(-1, 0);
        await Assert.That(carousel.SelectedIndex).IsEqualTo(1);
        Advance(host, 28);
        Wheel(-1, 0);
        await Assert.That(carousel.SelectedIndex).IsEqualTo(2);
        Advance(host, 28);
        Wheel(1, 0);
        await Assert.That(carousel.SelectedIndex).IsEqualTo(1);
    }

    // scroll_mask.rs: a trackpad's deltas move the slides, and the gesture snaps when it goes quiet.
    [Test]
    public async Task A_trackpad_scroll_moves_the_slides_and_snaps_when_it_goes_quiet()
    {
        var golden = Case("uikit-carousel/nav.selected-0/normal/light");
        var (host, carousel, track) = OpenCarousel(golden);
        using var _h = host;
        for (var i = 0; i < 4; i++)
        {
            host.Window.MouseWheel(new Point(184, 76), new Vector(-0.8, 0));
            host.Flush();
            Advance(host, 16);
        }
        await Assert.That((track.TrackOffset, carousel.SelectedIndex, track.IsInteracting)).IsEqualTo((160d, 0, true));
        Advance(host, 28);
        await Assert.That((carousel.SelectedIndex, track.IsInteracting)).IsEqualTo((1, false));
        Advance(host, 1000);
        await Assert.That(track.TrackOffset).IsEqualTo(256);
    }

    // A vertical carousel in a scrolled page (scroll_mask.rs tests).
    private static (CaseHost Host, Carousel Carousel, ScrollViewer Page) OpenNestedVertical(int selected)
    {
        var golden = Case("uikit-carousel/vertical.selected-0/normal/light");
        var carousel = new Carousel
        {
            Width = 240,
            Height = 120,
            ItemsSource = new[] { new Border { Height = 120 }, new Border { Height = 120 } },
            SelectedIndex = selected,
            PageTransition = new SpringSlide { Orientation = PageSlide.SlideAxis.Vertical },
        };
        Carousels.SetTracksPointer(carousel, true);
        var page = new ScrollViewer
        {
            Width = 240,
            Height = 160,
            Content = new StackPanel { Children = { carousel, new Border { Height = 400 } } },
        };
        return (CaseHost.Open(golden, page), carousel, page);
    }

    [Test]
    public async Task A_vertical_carousel_hands_a_notch_at_its_end_to_the_page_but_keeps_a_burst()
    {
        var (host, carousel, page) = OpenNestedVertical(1);
        using (host)
        {
            host.Window.MouseWheel(new Point(184, 80), new Vector(0, -1));
            host.Flush();
            await Assert.That(carousel.SelectedIndex).IsEqualTo(1);
            await Assert.That(page.Offset.Y).IsGreaterThan(0);
        }
        (host, carousel, page) = OpenNestedVertical(0);
        using (host)
        {
            host.Window.MouseWheel(new Point(184, 80), new Vector(0, -1));
            host.Window.MouseWheel(new Point(184, 80), new Vector(0, -1));
            host.Flush();
            await Assert.That((carousel.SelectedIndex, page.Offset.Y)).IsEqualTo((1, 0d));
            Advance(host, 28);
            host.Window.MouseWheel(new Point(184, 80), new Vector(0, -1));
            host.Flush();
            await Assert.That(carousel.SelectedIndex).IsEqualTo(1);
            await Assert.That(page.Offset.Y).IsGreaterThan(0);
        }
    }

    [Test]
    public async Task A_vertical_carousel_hands_a_trackpad_gesture_that_starts_at_its_end_to_the_page()
    {
        var (host, carousel, page) = OpenNestedVertical(1);
        using var _h = host;
        host.Window.MouseWheel(new Point(184, 80), new Vector(0, -0.5));
        host.Window.MouseWheel(new Point(184, 80), new Vector(0, -0.5));
        host.Flush();
        await Assert.That(carousel.SelectedIndex).IsEqualTo(1);
        await Assert.That(page.Offset.Y).IsGreaterThan(0);
    }

    // state.rs adjacent_loop_target and settle_loop_motion.
    [Test]
    public async Task A_looping_carousel_goes_on_past_the_last_slide_and_settles_in_its_cycle()
    {
        var golden = Case("uikit-carousel/loop.selected-2/normal/light");
        var (host, carousel, track) = OpenCarousel(golden);
        using var _h = host;
        Click(host, golden, Nav<CarouselNext>(host));
        await Assert.That(carousel.SelectedIndex).IsEqualTo(0);
        Advance(host, 50);
        await Assert.That(track.TrackOffset).IsGreaterThan(2 * 256);
        Advance(host, 1000);
        await Assert.That(track.TrackOffset).IsEqualTo(0);
        Click(host, golden, Nav<CarouselPrevious>(host));
        Advance(host, 50);
        await Assert.That((carousel.SelectedIndex, track.TrackOffset < 0)).IsEqualTo((2, true));
        Advance(host, 1000);
        await Assert.That(track.TrackOffset).IsEqualTo(2 * 256);
    }

    // ---- The tab bar ----

    private static IEnumerable<T> Find<T>(CaseHost host) where T : Visual => host.Window.GetVisualDescendants().OfType<T>();

    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task The_tab_menu_lists_the_tabs_and_selects_one(bool tabControl)
    {
        var golden = Case("tabs-bar/menu-disabled.base/click-at-326-24/light");
        SelectingItemsControl tabs = tabControl ? Adapters.TabBarParts(Adapters.TabControl(golden), golden) : (SelectingItemsControl)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, tabs);
        host.Drive(golden, "click-at-326-24");
        var items = Find<MenuItem>(host).ToList();
        await Assert.That(string.Join(", ", items.Select(i => $"{i.Header}{(i.IsChecked ? " ✓" : "")}{(i.IsEnabled ? "" : " (disabled)")}")))
            .IsEqualTo("Account, Profile ✓, Settings (disabled)");
        Click(host, golden, items[0]);
        await Assert.That(tabs.SelectedIndex).IsEqualTo(0);
    }

    [Test]
    public async Task An_icon_tab_is_listed_by_its_icon()
    {
        var golden = Case("tabs/icon.tab.medium/normal/light");
        var tabs = (TabStrip)Adapters.Create(golden);
        tabs.Classes.Add("menu");
        using var host = CaseHost.Open(golden, tabs);
        Click(host, golden, host.Part<Button>("PART_MenuButton"));
        var icons = Find<MenuItem>(host).Select(i => i.Header).OfType<PathIcon>().ToList();
        await Assert.That(icons.Count).IsEqualTo(3);
        await Assert.That(icons[1].Data).IsSameReferenceAs(((PathIcon)((TabStripItem)tabs.ContainerFromIndex(1)!).Content!).Data);
    }

    // tab_bar.rs overflow_x_scroll: the tabs scroll, the selected one shows, and the
    // indicator stays on it; the prefix and suffix stay where they are.
    [Test]
    public async Task Tabs_that_overflow_scroll_and_the_selected_one_shows_with_its_indicator()
    {
        var golden = Case("tabs-bar/bar.pill/normal/light");
        var tabs = (TabStrip)Adapters.Create(golden);
        tabs.Width = 300;
        for (var i = 4; i <= 10; i++)
        {
            tabs.Items.Add(new TabStripItem { Content = $"Tab {i}" });
        }
        using var host = CaseHost.Open(golden, tabs);
        var scroller = host.Part<ScrollViewer>("PART_Scroller");
        var prefix = host.Part<ContentPresenter>("PART_Prefix");
        var prefixAt = prefix.TranslatePoint(default, host.Window);
        await Assert.That(scroller.Extent.Width).IsGreaterThan(scroller.Viewport.Width);
        tabs.SelectedIndex = 9;
        Advance(host, 1000);
        var last = tabs.ContainerFromIndex(9)!;
        var left = last.TranslatePoint(default, scroller)!.Value.X;
        await Assert.That(scroller.Offset.X).IsGreaterThan(0);
        await Assert.That(left).IsGreaterThanOrEqualTo(0);
        await Assert.That(left + last.Bounds.Width).IsLessThanOrEqualTo(scroller.Viewport.Width + 0.01);
        var indicator = host.Part<Panel>("PART_Indicator");
        await Assert.That(indicator.TranslatePoint(default, last)!.Value.X).IsEqualTo(0).Within(0.01);
        await Assert.That(prefix.TranslatePoint(default, host.Window)).IsEqualTo(prefixAt);
    }

    // The row's clip cuts the tabs where they are scrolled away and spares the focus rings elsewhere.
    [Test]
    public async Task The_tab_row_clip_spares_the_focus_ring_where_no_tab_is_hidden()
    {
        var golden = Case("tabs-bar/overflow.tab/normal/light");
        var tabs = (TabStrip)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, tabs);
        var presenter = host.Part<ScrollViewer>("PART_Scroller").GetVisualDescendants().OfType<ScrollContentPresenter>().First();
        Rect Clip() => ((RectangleGeometry)presenter.Clip!).Rect;
        await Assert.That(Clip()).IsEqualTo(new Rect(-3, -3, presenter.Bounds.Width + 3, presenter.Bounds.Height + 6));
        presenter.Offset = new Vector(presenter.Extent.Width - presenter.Viewport.Width, 0);
        host.Flush();
        await Assert.That(Clip()).IsEqualTo(new Rect(0, -3, presenter.Bounds.Width + 3, presenter.Bounds.Height + 6));
    }

    // ---- Slots ----

    [Test]
    public async Task A_separator_label_and_a_group_footer_show_only_when_set()
    {
        var golden = Case("separator/label.base/normal/light");
        var separator = (Separator)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, separator);
        await Assert.That(separator.Classes.Contains(":labeled")).IsTrue();
        await Assert.That(separator.Bounds.Height).IsEqualTo(27.5);
        Separators.SetLabel(separator, null);
        host.Flush();
        await Assert.That(separator.Classes.Contains(":labeled")).IsFalse();
        await Assert.That(separator.Bounds.Height).IsEqualTo(1);
        await Assert.That(host.Part<Border>("PART_Label").IsVisible).IsFalse();

        var footerCase = Case("groupbox/footer.fill/normal/light");
        var group = (GroupBox)Adapters.Create(footerCase);
        using var groupHost = CaseHost.Open(footerCase, group);
        var footer = groupHost.Part<ContentPresenter>("PART_FooterPresenter");
        await Assert.That(footer.IsVisible).IsTrue();
        GroupBoxes.SetFooter(group, null);
        await Assert.That(footer.IsVisible).IsFalse();
    }
}
