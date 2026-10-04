using Avalonia.Controls;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What the Dock theme's parts do, beyond the frames the reference renders
/// show: the toolbar's menu, the close buttons, and dropping in GPUI's zones.
/// </summary>
public class DockBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    private static IDock Root(DockControl dock) => (IDock)dock.Layout!;

    private static IEnumerable<IDockable> All(IDockable dockable) =>
        dockable is IDock { VisibleDockables: { } children } ? children.SelectMany(All).Prepend(dockable) : [dockable];

    private static IDock OwnerOf(DockControl dock, string id) =>
        (IDock)All(Root(dock)).First(d => d.Id == id).Owner!;

    // tab_panel.rs: the ellipsis menu ends with Close, which closes the group's active panel.
    [Test]
    public async Task The_toolbar_menu_closes_the_active_panel()
    {
        var golden = Case("dock/layout.base/normal/light");
        var dock = (DockControl)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, dock);
        // The Explorer group's toolbar: x = 16 + 240 - 8 - 10, y = 16 + 16.
        host.Drive(golden, "click-at-238-32");
        var close = host.Window.GetVisualDescendants().OfType<MenuItem>().Last(i => i.IsEffectivelyVisible);
        await Assert.That(close.Header).IsEqualTo(Avalonia.Application.Current!.FindResource("ToolChromeControlCloseString"));
        close.Command!.Execute(close.CommandParameter);
        host.Flush();
        await Assert.That(OwnerOf(dock, "Search").VisibleDockables!.Select(d => d.Id)).IsEquivalentTo(["Search"]);
    }

    [Test]
    public async Task A_tab_close_button_closes_its_document()
    {
        var golden = Case("dock/close-button.base/at-101-32/light");
        var dock = (DockControl)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, dock);
        host.Drive(golden, "click-at-101-32");
        await Assert.That(string.Join(", ", OwnerOf(dock, "Search").VisibleDockables!.Select(d => d.Id))).IsEqualTo("Search");
    }

    // drag.rs: the left 35% of a group splits it, the dragged panel on the left.
    [Test]
    public async Task Dropping_in_the_left_zone_splits_the_group()
    {
        var golden = Case("dock/drop.base/pressed-at-56-32+drag-at-66-32+drag-at-66-90+drag-at-281-130+drag-at-280-130+wait-500ms/light");
        var dock = (DockControl)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, dock);
        host.Drive(golden, "pressed-at-56-32+drag-at-66-32+drag-at-66-90+drag-at-281-130+drag-at-280-130+release");
        var explorer = OwnerOf(dock, "Explorer");
        var editor = OwnerOf(dock, "Editor");
        var split = (IDock)explorer.Owner!;
        await Assert.That(split).IsSameReferenceAs(editor.Owner);
        await Assert.That(split.VisibleDockables!.IndexOf(explorer)).IsLessThan(split.VisibleDockables!.IndexOf(editor));
        await Assert.That(((IProportionalDock)split).Orientation).IsEqualTo(Orientation.Horizontal);
    }
}
