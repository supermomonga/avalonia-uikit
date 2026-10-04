using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;
using Tabalonia.Controls;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What Tabalonia's TabsControl adds to GPUI Kit's TabBar, which the reference
/// renders cannot show: dragging, closing, adding, the menu and scrolling.
/// </summary>
public class TabaloniaBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    // The tabs' headers in order.
    private static string Order(TabsControl tabs) =>
        string.Join(", ", ((IList)tabs.ItemsSource!).Cast<DragTabItem>().Select(t => t.Header));

    private static Point Center(CaseHost host, Control control) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), host.Window)!.Value;

    private static void Click(CaseHost host, GoldenCase golden, Control control)
    {
        var at = Center(host, control);
        host.Drive(golden, $"click-at-{at.X:0.##}-{at.Y:0.##}");
    }

    // The selected pill is the indicator: it moves with the dragged tab rather than
    // springing after it, and the dropped tab keeps it.
    [Test]
    public async Task The_indicator_moves_with_a_dragged_tab()
    {
        var golden = Case("tabalonia/label.pill.medium/normal/light");
        var tabs = (TabsControl)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, tabs);
        var indicator = host.Window.GetVisualDescendants().OfType<Panel>().Single(p => p.Name == "PART_Indicator");
        var dragged = (DragTabItem)tabs.ContainerFromIndex(0)!;
        double Offset() => indicator.TranslatePoint(default, tabs)!.Value.X - dragged.TranslatePoint(default, tabs)!.Value.X;

        host.Drive(golden, "pressed-at-66-24+drag-at-110-24+drag-at-150-24");
        await Assert.That(dragged.IsDragging).IsTrue();
        await Assert.That(Offset()).IsEqualTo(0).Within(0.01);
        host.Drive(golden, "drag-at-200-24");
        await Assert.That(Offset()).IsEqualTo(0).Within(0.01);

        host.Drive(golden, "release+wait-500ms");
        await Assert.That(Order(tabs)).IsEqualTo("Profile, Account, Settings");
        await Assert.That(tabs.SelectedItem).IsSameReferenceAs(dragged);
        await Assert.That(Offset()).IsEqualTo(0).Within(0.01);
    }

    [Test]
    public async Task The_close_button_closes_its_tab()
    {
        var golden = Case("tabalonia/close.tab/at-97-24/light");
        var tabs = (TabsControl)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, tabs);
        host.Drive(golden, "click-at-97-24");
        await Assert.That(Order(tabs)).IsEqualTo("Profile, Settings");
    }

    // Avalonia-only: Tabalonia's add button sits after the last tab as a ghost xsmall button.
    [Test]
    public async Task The_add_button_adds_a_tab_after_the_last()
    {
        var golden = Case("tabalonia/label.tab.medium/normal/light");
        var tabs = (TabsControl)Adapters.Create(golden);
        tabs.ShowDefaultAddButton = true;
        tabs.NewItemFactory = () => new DragTabItem { Header = "New" };
        using var host = CaseHost.Open(golden, tabs);
        var add = host.Window.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_AddItemButton");
        await Assert.That(add.Bounds.Size).IsEqualTo(new Size(20, 20));
        await Assert.That(add.TranslatePoint(default, tabs)!.Value.X).IsEqualTo(3 * 100 + 4);
        Click(host, golden, add);
        await Assert.That(Order(tabs)).IsEqualTo("Account, Profile, Settings, New");
    }

    [Test]
    public async Task The_menu_lists_the_tabs_and_selects_one()
    {
        var golden = Case("tabalonia/menu.tab/normal/light");
        var tabs = (TabsControl)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, tabs);
        host.Drive(golden, "click-at-366-24");
        var items = host.Window.GetVisualDescendants().OfType<MenuItem>().ToList();
        await Assert.That(string.Join(", ", items.Select(i => $"{i.Header}{(i.IsChecked ? " ✓" : "")}"))).IsEqualTo("Account, Profile ✓, Settings");
        Click(host, golden, items[2]);
        await Assert.That(tabs.SelectedIndex).IsEqualTo(2);
    }

    // GPUI's bar scrolls its tabs sideways (overflow_x_scroll); the selected one is brought into view.
    [Test]
    public async Task Tabs_that_overflow_scroll_and_the_selected_one_shows()
    {
        var golden = Case("tabalonia/label.tab.medium/normal/light");
        var tabs = new TabsControl
        {
            Width = 360,
            TabItemWidth = 100,
            EnableTabDetaching = false,
            ShowDefaultAddButton = false,
            ShowDefaultCloseButton = false,
            ItemsSource = new ObservableCollection<object>(Enumerable.Range(1, 10).Select(i => new DragTabItem { Header = $"Tab {i}" })),
            SelectedIndex = 0,
        };
        using var host = CaseHost.Open(golden, tabs);
        var scroller = host.Window.GetVisualDescendants().OfType<ScrollViewer>().Single(s => s.Name == "PART_Scroller");
        await Assert.That(tabs.ContainerFromIndex(9)!.Bounds.Width).IsEqualTo(100);
        await Assert.That(scroller.Extent.Width).IsGreaterThanOrEqualTo(1000);
        tabs.SelectedIndex = 9;
        host.Flush();
        var last = tabs.ContainerFromIndex(9)!;
        var right = last.TranslatePoint(new Point(last.Bounds.Width, 0), scroller)!.Value.X;
        await Assert.That(scroller.Offset.X).IsGreaterThan(0);
        await Assert.That(right).IsLessThanOrEqualTo(scroller.Viewport.Width + 0.01);
    }

    // As TabStrip's tabs (BehaviorTests): Avalonia-only focus shows on the keyboard only.
    // (Tab after a click keeps the clicked tab focused: the case has nothing else to focus.)
    [Test]
    public async Task A_tab_shows_the_focus_ring_on_keyboard_focus_only()
    {
        var golden = Case("tabalonia/label.pill.medium/normal/light");
        static bool Ring(CaseHost host) =>
            host.Window.GetVisualDescendants().OfType<Border>().Any(b => b.Name == "PART_FocusRing" && b.IsEffectivelyVisible);
        using (var host = CaseHost.Open(golden, Adapters.Create(golden)))
        {
            host.Drive(golden, "click");
            await Assert.That(Ring(host)).IsFalse();
        }
        using (var host = CaseHost.Open(golden, Adapters.Create(golden)))
        {
            host.Drive(golden, "focus");
            await Assert.That(Ring(host)).IsTrue();
        }
    }
}
