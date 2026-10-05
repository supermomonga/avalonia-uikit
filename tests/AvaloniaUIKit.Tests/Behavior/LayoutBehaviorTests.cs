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
}
