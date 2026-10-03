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
}
