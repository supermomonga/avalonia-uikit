using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What the layout controls of ADR 30 do: uikit:ResizablePanelGroup, Sheet
/// and Sidebar, each with the GPUI Kit rule it ports.
/// </summary>
public class LayoutBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    // MARK: ResizablePanelGroup

    /// <summary>A 432x136 window with the control at (16, 8).</summary>
    private static CaseHost ResizableHost(Control content) =>
        CaseHost.Open(Case("uikit-resizable/chain.base/normal/light"), content);

    private static ResizablePanelGroup Group(double length, params double[] sizes)
    {
        var group = new ResizablePanelGroup { Width = length, Height = 120 };
        foreach (var size in sizes)
        {
            group.Children.Add(size > 0 ? new ResizablePanel { Size = size } : new ResizablePanel());
        }
        return group;
    }

    private static double[] Lengths(ResizablePanelGroup group) =>
        [.. group.Children.Select(c => c.IsVisible ? Math.Round(c.Bounds.Width, 3) : 0)];

    private static double[] Rounded(IReadOnlyList<double> sizes) => [.. sizes.Select(s => Math.Round(s, 3))];

    private static void Drag(CaseHost host, params Point[] path)
    {
        host.Window.MouseMove(path[0]);
        host.Window.MouseDown(path[0], MouseButton.Left);
        host.Flush();
        foreach (var at in path[1..])
        {
            host.Window.MouseMove(at, RawInputModifiers.LeftMouseButton);
            host.Flush();
        }
        host.Window.MouseUp(path[^1], MouseButton.Left);
        host.Flush();
    }

    // panel.rs: a sized panel keeps its size on the first layout (flex_none); the
    // panels without one start from the whole length and shrink alike.
    [Test]
    public async Task Panels_without_a_size_share_what_the_sized_ones_leave()
    {
        var group = Group(400, 100, 0, 0);
        using var host = ResizableHost(group);
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 100, 150, 150 });
        await Assert.That(Rounded(group.Sizes)).IsEquivalentTo(new double[] { 100, 150, 150 });
        // The size range wins: a third panel 100..120 leaves the rest to the second.
        var ranged = Group(400, 100, 0, 0);
        ((ResizablePanel)ranged.Children[2]).MaxSize = 120;
        using var other = ResizableHost(ranged);
        await Assert.That(Lengths(ranged)).IsEquivalentTo(new double[] { 100, 180, 120 });
        // Two sized panels around one hidden from the start: once laid out they grow
        // alike to fill the group, as on GPUI's second frame.
        var hidden = Group(400, 120, 0, 120);
        hidden.Children[1].IsVisible = false;
        using var third = ResizableHost(hidden);
        await Assert.That(Lengths(hidden)).IsEquivalentTo(new double[] { 200, 0, 200 });
    }

    // resize_panel_at_handle: growing a panel takes room from the panels after it in
    // turn, each down to its minimum; past that the handle stays. The drag starts on
    // the first move past 2px and resizes from the next; a mouse up reports the sizes.
    [Test]
    public async Task Dragging_a_handle_resizes_in_a_chain_and_reports_once()
    {
        var group = Group(400, 120, 140, 0);
        using var host = ResizableHost(group);
        var reports = new List<double[]>();
        group.Resized += (_, e) => reports.Add(Rounded(e.Sizes));
        Drag(host, new Point(136, 68), new Point(146, 68), new Point(216, 68));
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 200, 100, 100 });
        Drag(host, new Point(216, 68), new Point(226, 68), new Point(300, 68));
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 200, 100, 100 });
        await Assert.That(reports.Count).IsEqualTo(2);
        await Assert.That(reports[0]).IsEquivalentTo(new double[] { 200, 100, 100 });

        // Shrinking the second panel below its minimum shrinks the first; the third takes the room.
        var shrink = Group(400, 120, 140, 0);
        using var other = ResizableHost(shrink);
        Drag(other, new Point(276, 68), new Point(266, 68), new Point(196, 68));
        await Assert.That(Lengths(shrink)).IsEquivalentTo(new double[] { 100, 100, 200 });
    }

    // div.rs: a move within 2px of the press is not a drag (no resize, no report);
    // the move that starts the drag does not resize.
    [Test]
    public async Task A_drag_starts_past_two_pixels_and_resizes_from_the_next_move()
    {
        var group = Group(320, 120, 0);
        using var host = ResizableHost(group);
        var reports = 0;
        group.Resized += (_, _) => reports++;
        Drag(host, new Point(136, 68), new Point(138, 68));
        await Assert.That(reports).IsEqualTo(0);
        Drag(host, new Point(136, 68), new Point(150, 68));
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 120, 200 });
        await Assert.That(reports).IsEqualTo(1);
        Drag(host, new Point(136, 68), new Point(150, 68), new Point(166, 68));
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 150, 170 });
    }

    // ResizableState::resize_panel: the drag's rules, clamped to the size range; the
    // last panel resizes the one before it. Resized follows each call.
    [Test]
    public async Task ResizePanel_clamps_and_redistributes()
    {
        var group = Group(400, 120, 140, 0);
        ((ResizablePanel)group.Children[0]).MaxSize = 180;
        using var host = ResizableHost(group);
        var reports = new List<double[]>();
        group.Resized += (_, e) => reports.Add(Rounded(e.Sizes));
        group.ResizePanel(0, 300);
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 180, 100, 120 });
        // The last panel grows by shrinking the ones before it in turn.
        group.ResizePanel(2, 200);
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 100, 100, 200 });
        group.ResizePanel(2, 100);
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 100, 200, 100 });
        await Assert.That(reports.Count).IsEqualTo(3);
        await Assert.That(reports[^1]).IsEquivalentTo(new double[] { 100, 200, 100 });
    }

    // visible(false): the hidden panel takes no room and no handle; the others grow
    // by equal parts and get their lengths back when it returns.
    [Test]
    public async Task A_hidden_panel_gives_its_room_and_gets_it_back()
    {
        var group = Group(400, 120, 140, 0);
        using var host = ResizableHost(group);
        group.Children[1].IsVisible = false;
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 190, 0, 210 });
        var handles = group.GetVisualChildren().OfType<GridSplitter>().ToList();
        await Assert.That(handles.Count(h => h.IsVisible)).IsEqualTo(1);
        group.Children[1].IsVisible = true;
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 120, 140, 140 });
        await Assert.That(handles.Count(h => h.IsVisible)).IsEqualTo(2);
    }

    // adjust_to_container_size: a new group length scales every panel by the same ratio.
    [Test]
    public async Task A_new_group_length_scales_the_panels()
    {
        var group = Group(400, 120, 140, 0);
        using var host = ResizableHost(group);
        group.Width = 800;
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 240, 280, 280 });
    }

    // insert_panel: a new panel takes its size and the others give room in proportion;
    // remove_panel: the others fill its room in proportion.
    [Test]
    public async Task Adding_and_removing_panels_redistributes_in_proportion()
    {
        var group = Group(400, 0, 0);
        using var host = ResizableHost(group);
        group.Children.Add(new ResizablePanel { Size = 100 });
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 150, 150, 100 });
        group.Children.RemoveAt(2);
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 200, 200 });
        await Assert.That(group.GetVisualChildren().OfType<GridSplitter>().Count()).IsEqualTo(1);
    }

    // The handles are GridSplitters with its theme. They take no focus (GPUI's do
    // not); one an app makes focusable moves with the arrow keys.
    [Test]
    public async Task A_focused_handle_moves_with_the_arrow_keys()
    {
        var group = Group(320, 120, 0);
        using var host = ResizableHost(group);
        var handle = group.GetVisualChildren().OfType<GridSplitter>().Single();
        await Assert.That(handle.ResizeDirection).IsEqualTo(GridResizeDirection.Columns);
        await Assert.That(handle.Focusable).IsFalse();
        handle.Focusable = true;
        handle.Focus();
        host.PressKey("right");
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 130, 190 });
        host.PressKey("left");
        host.PressKey("left");
        host.Flush();
        await Assert.That(Lengths(group)).IsEquivalentTo(new double[] { 110, 210 });
        group.Orientation = Orientation.Vertical;
        host.Flush();
        await Assert.That(handle.ResizeDirection).IsEqualTo(GridResizeDirection.Rows);
    }

    // MARK: Sheet

    /// <summary>A 560x400 window with <paramref name="content"/> at its origin.</summary>
    private static CaseHost SheetHost(Control content) =>
        CaseHost.Open(Case("uikit-sheet/open.right/click+wait-200ms/light"), content);

    /// <summary>Shows <paramref name="sheet"/> and lets its 150ms slide finish.</summary>
    private static void Open(CaseHost host, Sheet sheet, Visual anchor)
    {
        sheet.Show(anchor);
        host.Flush();
        AvaloniaUIKit.Tests.Infrastructure.VirtualTime.Advance(TimeSpan.FromMilliseconds(200));
        host.Flush();
    }

    private static void Press(CaseHost host, Point at, MouseButton button = MouseButton.Left)
    {
        host.Window.MouseMove(at);
        host.Window.MouseDown(at, button);
        host.Window.MouseUp(at, button);
        host.Flush();
    }

    // root.rs: opening focuses the sheet (and traps Tab in it); closing gives the
    // focus back. sheet.rs: Escape closes it.
    [Test]
    public async Task A_sheet_takes_the_focus_and_gives_it_back_on_escape()
    {
        var box = new TextBox { Width = 120 };
        using var host = SheetHost(new StackPanel { Children = { box } });
        box.Focus();
        var first = new TextBox();
        var second = new TextBox();
        var sheet = new Sheet { Title = "Settings", Content = new StackPanel { Children = { first, second } } };
        var closed = 0;
        sheet.Closed += (_, _) => closed++;
        Open(host, sheet, box);
        await Assert.That(sheet.IsOpen).IsTrue();
        await Assert.That(sheet.IsKeyboardFocusWithin).IsTrue();
        await Assert.That(Sheet.GetActive(box)).IsEqualTo(sheet);
        // Tab stays inside: the close button, the fields, round again.
        var seen = new List<IInputElement?>();
        for (var i = 0; i < 4; i++)
        {
            host.PressKey("tab");
            host.Flush();
            seen.Add(host.Window.FocusManager!.GetFocusedElement());
        }
        await Assert.That(seen.All(e => e is Visual v && v.GetVisualAncestors().Contains(sheet))).IsTrue();
        var close = sheet.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_CloseButton");
        await Assert.That(seen[0] == close && seen[1] == first && seen[2] == second && seen[3] == close).IsTrue();
        host.PressKey("escape");
        host.Flush();
        await Assert.That(sheet.IsOpen).IsFalse();
        await Assert.That(closed).IsEqualTo(1);
        await Assert.That(box.IsFocused).IsTrue();
        await Assert.That(Sheet.GetActive(box)).IsNull();
    }

    // root.rs active_sheet: one sheet at a time. The new one closes the open one and
    // keeps the focus it was to give back.
    [Test]
    public async Task Showing_a_sheet_closes_the_open_one()
    {
        var box = new TextBox { Width = 120 };
        using var host = SheetHost(new StackPanel { Children = { box } });
        box.Focus();
        var first = new Sheet { Title = "First" };
        var second = new Sheet { Title = "Second", Placement = DrawerPlacement.Left };
        var closed = new List<string>();
        first.Closed += (_, _) => closed.Add("first");
        second.Closed += (_, _) => closed.Add("second");
        Open(host, first, box);
        Open(host, second, box);
        await Assert.That(first.IsOpen).IsFalse();
        await Assert.That(second.IsOpen).IsTrue();
        await Assert.That(host.Window.GetVisualDescendants().OfType<Sheet>().Count()).IsEqualTo(1);
        await Assert.That(Sheet.GetActive(box)).IsEqualTo(second);
        second.Close();
        host.Flush();
        await Assert.That(closed).IsEquivalentTo(new[] { "first", "second" });
        await Assert.That(box.IsFocused).IsTrue();
    }

    // base/sheet.rs: the overlay takes every press; the left button closes the sheet
    // unless it is not closable or there is no overlay. The close button closes it.
    [Test]
    public async Task The_overlay_closes_the_sheet_when_it_may()
    {
        var clicks = 0;
        var button = new Button { Content = "Page", Width = 120 };
        button.Click += (_, _) => clicks++;
        using var host = SheetHost(new StackPanel { Children = { button } });
        var page = button.TranslatePoint(new Point(10, 10), host.Window)!.Value;

        var sheet = new Sheet { Title = "Settings" };
        Open(host, sheet, button);
        Press(host, page, MouseButton.Right);
        await Assert.That(sheet.IsOpen).IsTrue();
        Press(host, page);
        await Assert.That(sheet.IsOpen).IsFalse();

        foreach (var kept in new[] { new Sheet { IsOverlayClosable = false }, new Sheet { HasOverlay = false } })
        {
            Open(host, kept, button);
            Press(host, page);
            await Assert.That(kept.IsOpen).IsTrue();
            var close = kept.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_CloseButton");
            Press(host, close.TranslatePoint(new Point(close.Bounds.Width / 2, close.Bounds.Height / 2), host.Window)!.Value);
            await Assert.That(kept.IsOpen).IsFalse();
        }
        await Assert.That(clicks).IsEqualTo(0);
    }

    // sheet.rs: the size is pixels or a part of the window (DefiniteLength), along the
    // placement; GPUI's title-bar offset (UIKit.Sheet.Margin) is not for a bottom sheet.
    [Test]
    public async Task A_sheet_is_its_size_from_its_edge()
    {
        var area = new Border { Width = 560, Height = 400 };
        using var host = SheetHost(area);
        host.Window.Resources["UIKit.Sheet.Margin"] = new Thickness(0, 34, 0, 0);
        Rect Surface(Sheet sheet)
        {
            Open(host, sheet, area);
            var surface = sheet.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Surface");
            return new Rect(surface.TranslatePoint(default, host.Window)!.Value, surface.Bounds.Size);
        }
        await Assert.That(Surface(new Sheet { Size = RelativeScalar.Parse("50%") })).IsEqualTo(new Rect(280, 34, 280, 366));
        await Assert.That(Surface(new Sheet { Placement = DrawerPlacement.Top, Size = RelativeScalar.Parse("120") })).IsEqualTo(new Rect(0, 34, 560, 120));
        await Assert.That(Surface(new Sheet { Placement = DrawerPlacement.Bottom })).IsEqualTo(new Rect(0, 50, 560, 350));
        await Assert.That(Surface(new Sheet { Placement = DrawerPlacement.Left, Size = RelativeScalar.Parse("25%") })).IsEqualTo(new Rect(0, 34, 140, 366));
    }

    // MARK: Sidebar

    /// <summary>A 432x576 window with the sidebar story's menu case built in it.</summary>
    private static (CaseHost Host, AvaloniaUIKit.Sidebar Sidebar) SidebarStory(string id = "uikit-sidebar/menu.base/normal/light")
    {
        var golden = Case(id);
        var host = CaseHost.Open(golden, Adapters.Create(golden));
        return (host, host.Window.GetVisualDescendants().OfType<AvaloniaUIKit.Sidebar>().Single());
    }

    private static SidebarMenuItem MenuItem(AvaloniaUIKit.Sidebar sidebar, string label) =>
        sidebar.GetVisualDescendants().OfType<SidebarMenuItem>().Single(i => i.Label == label);

    private static T Part<T>(Control control, string name) where T : Control =>
        control.GetVisualDescendants().OfType<T>().First(c => c.Name == name);

    private static void Click(CaseHost host, Visual target, MouseButton button = MouseButton.Left)
    {
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), host.Window)!.Value;
        Press(host, center, button);
    }

    // mod.rs: collapsing to icons moves the clip from the expanded width to 48px over
    // 200ms; the parts collapse at once (labels, group labels and carets go, and an
    // item with an icon has its label as a tooltip). Offcanvas goes to 0, keeps the
    // labels and then the content goes; None ignores the collapsed state.
    [Test]
    public async Task Collapsing_reaches_the_parts_and_moves_the_width()
    {
        var (host, sidebar) = SidebarStory();
        using var _ = host;
        var clip = Part<Panel>(sidebar, "PART_Clip");
        var playground = MenuItem(sidebar, "Playground");
        var history = MenuItem(sidebar, "History");
        await Assert.That(clip.Bounds.Width).IsEqualTo(220);
        sidebar.IsCollapsed = true;
        host.Flush();
        await Assert.That(AvaloniaUIKit.Sidebar.GetIsIconCollapsed(playground)).IsTrue();
        await Assert.That(playground.Classes.Contains(":collapsed")).IsTrue();
        await Assert.That(Part<TextBlock>(playground, "PART_Label").IsEffectivelyVisible).IsFalse();
        await Assert.That(history.IsEffectivelyVisible).IsFalse();
        await Assert.That(ToolTip.GetTip(Part<Panel>(playground, "PART_Row"))).IsEqualTo("Playground");
        await Assert.That(sidebar.GetVisualDescendants().OfType<SidebarGroup>().All(g => !Part<Border>(g, "PART_LabelHost").IsVisible)).IsTrue();
        AvaloniaUIKit.Tests.Infrastructure.VirtualTime.Advance(TimeSpan.FromMilliseconds(100));
        host.Flush();
        await Assert.That(clip.Bounds.Width).IsGreaterThan(48).And.IsLessThan(220);
        AvaloniaUIKit.Tests.Infrastructure.VirtualTime.Advance(TimeSpan.FromMilliseconds(150));
        host.Flush();
        await Assert.That(clip.Bounds.Width).IsEqualTo(48);

        sidebar.Collapsible = SidebarCollapsible.Offcanvas;
        AvaloniaUIKit.Tests.Infrastructure.VirtualTime.Advance(TimeSpan.FromMilliseconds(250));
        host.Flush();
        await Assert.That(clip.Bounds.Width).IsEqualTo(0);
        await Assert.That(AvaloniaUIKit.Sidebar.GetIsIconCollapsed(playground)).IsFalse();
        await Assert.That(ToolTip.GetTip(Part<Panel>(playground, "PART_Row"))).IsNull();
        // Once the width has gone, so has the content (out of the tab order).
        await Assert.That(playground.IsEffectivelyVisible).IsFalse();

        sidebar.Collapsible = SidebarCollapsible.None;
        AvaloniaUIKit.Tests.Infrastructure.VirtualTime.Advance(TimeSpan.FromMilliseconds(250));
        host.Flush();
        await Assert.That(clip.Bounds.Width).IsEqualTo(220);
        await Assert.That(playground.IsEffectivelyVisible).IsTrue();
    }

    // menu.rs: a click on the row raises the click (and runs the command); with
    // click_to_open it opens the submenu, with click_to_toggle it flips it on every
    // click; the caret flips it without clicking the item. A disabled item takes no click.
    [Test]
    public async Task Menu_items_click_and_open_their_submenus()
    {
        var (host, sidebar) = SidebarStory();
        using var _ = host;
        var models = MenuItem(sidebar, "Models");
        var row = Part<Panel>(models, "PART_Row");
        var command = new Command();
        var clicks = 0;
        models.Click += (_, _) => clicks++;
        models.Command = command;
        Click(host, Part<TextBlock>(models, "PART_Label"));
        await Assert.That((clicks, command.Count, models.IsOpen)).IsEqualTo((1, 1, false));
        Click(host, Part<Button>(models, "PART_Caret"));
        await Assert.That((clicks, models.IsOpen)).IsEqualTo((1, true));
        await Assert.That(MenuItem(sidebar, "Genesis").IsEffectivelyVisible).IsTrue();
        Click(host, Part<Button>(models, "PART_Caret"));
        await Assert.That(models.IsOpen).IsFalse();

        models.ClickToOpen = true;
        Click(host, row);
        Click(host, row);
        await Assert.That((clicks, models.IsOpen)).IsEqualTo((3, true));
        models.ClickToOpen = false;
        models.ClickToToggle = true;
        Click(host, row);
        await Assert.That(models.IsOpen).IsFalse();
        Click(host, row);
        await Assert.That(models.IsOpen).IsTrue();

        var travel = MenuItem(sidebar, "Travel");
        var travelClicks = 0;
        travel.Click += (_, _) => travelClicks++;
        Click(host, Part<Panel>(travel, "PART_Row"));
        await Assert.That(travelClicks).IsEqualTo(0);
    }

    // menu.rs context_menu: the item's context menu opens on a right click on its row.
    [Test]
    public async Task A_menu_item_opens_its_context_menu()
    {
        var (host, sidebar) = SidebarStory();
        using var _ = host;
        var playground = MenuItem(sidebar, "Playground");
        var menu = new ContextMenu { Items = { new MenuItem { Header = "About" } } };
        playground.ContextMenu = menu;
        // A nested item without a menu of its own does not open its parent's.
        Click(host, Part<Panel>(MenuItem(sidebar, "History"), "PART_Row"), MouseButton.Right);
        await Assert.That(menu.IsOpen).IsFalse();
        Click(host, Part<Panel>(playground, "PART_Row"), MouseButton.Right);
        await Assert.That(menu.IsOpen).IsTrue();
        menu.Close();
    }

    // Items that are not SidebarMenuItems get one; a SidebarMenu outside a Sidebar
    // collapses through the inherited Sidebar.IsIconCollapsed.
    [Test]
    public async Task A_menu_makes_items_of_data_and_collapses_on_its_own()
    {
        var menu = new SidebarMenu
        {
            ItemsSource = new[] { "Inbox", "Drafts" },
            ItemTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((_, _) => new TextBlock()),
        };
        using var host = SheetHost(new Border { Width = 220, Child = menu });
        var items = menu.GetVisualDescendants().OfType<SidebarMenuItem>().ToList();
        await Assert.That(items.Count).IsEqualTo(2);
        AvaloniaUIKit.Sidebar.SetIsIconCollapsed(menu, true);
        host.Flush();
        await Assert.That(items.All(AvaloniaUIKit.Sidebar.GetIsIconCollapsed)).IsTrue();
    }

    // As a SplitView's pane the sidebar follows it: closed is collapsed, to icons in
    // the compact modes and off the layout in the others, on the pane's side; the
    // SplitView draws the surface.
    [Test]
    public async Task A_sidebar_in_a_split_view_follows_its_pane()
    {
        var sidebar = new AvaloniaUIKit.Sidebar { Items = { new SidebarMenu { Items = { new SidebarMenuItem { Label = "Home" } } } } };
        var split = new SplitView
        {
            Width = 400,
            Height = 300,
            DisplayMode = SplitViewDisplayMode.CompactInline,
            IsPaneOpen = false,
            Pane = sidebar,
        };
        using var host = SheetHost(split);
        await Assert.That(sidebar.Classes.Contains(":pane")).IsTrue();
        await Assert.That((sidebar.Collapsible, sidebar.IsCollapsed, sidebar.Side)).IsEqualTo((SidebarCollapsible.Icon, true, Side.Left));
        await Assert.That(AvaloniaUIKit.Sidebar.GetIsIconCollapsed(sidebar.GetVisualDescendants().OfType<SidebarMenuItem>().Single())).IsTrue();
        // Inside the SplitView's surface and its 1px border.
        await Assert.That(Part<Border>(sidebar, "PART_Surface").Bounds.Width).IsEqualTo(split.CompactPaneLength - 1);
        split.IsPaneOpen = true;
        split.DisplayMode = SplitViewDisplayMode.Inline;
        split.PanePlacement = SplitViewPanePlacement.Right;
        host.Flush();
        await Assert.That((sidebar.Collapsible, sidebar.IsCollapsed, sidebar.Side)).IsEqualTo((SidebarCollapsible.Offcanvas, false, Side.Right));
    }

    // SidebarToggleButton: a click flips the sidebar it is bound to, and its icon
    // shows what a click will do.
    [Test]
    public async Task The_toggle_button_flips_the_sidebar()
    {
        var golden = Case("uikit-sidebar/toggle.base/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var sidebar = host.Window.GetVisualDescendants().OfType<AvaloniaUIKit.Sidebar>().Single();
        var toggle = host.Window.GetVisualDescendants().OfType<SidebarToggleButton>().Single();
        var icon = (PathIcon)toggle.Content!;
        await Assert.That(ReferenceEquals(icon.Data, host.Window.FindResource("UIKit.Icon.PanelLeftClose"))).IsTrue();
        Click(host, toggle);
        await Assert.That((sidebar.IsCollapsed, toggle.IsCollapsed)).IsEqualTo((true, true));
        await Assert.That(ReferenceEquals(icon.Data, host.Window.FindResource("UIKit.Icon.PanelLeftOpen"))).IsTrue();
    }

    // header.rs: a header is selected or hovered in the accent; it is a Button, so a
    // Flyout is its dropdown menu.
    [Test]
    public async Task A_sidebar_header_opens_its_menu()
    {
        var (host, sidebar) = SidebarStory();
        using var _ = host;
        var header = (SidebarHeader)sidebar.Header!;
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "Twitter Inc." } } };
        header.Flyout = flyout;
        Click(host, header);
        await Assert.That(flyout.IsOpen).IsTrue();
        flyout.Hide();
        header.IsSelected = true;
        await Assert.That(header.Classes.Contains(":selected")).IsTrue();
    }

    private sealed class Command : System.Windows.Input.ICommand
    {
        public int Count { get; private set; }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => Count++;
    }
}
