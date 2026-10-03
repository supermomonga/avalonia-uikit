using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What the controls this library adds (ADR 19) do beyond their look: the
/// GPUI Kit behavior each one ports, and the notation and colors they compute.
/// </summary>
public class ControlBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    [Test]
    public async Task A_clipboard_copies_and_shows_the_check_for_two_seconds()
    {
        var golden = Case("clipboard/pointer.base/hover/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var clipboard = (AvaloniaUIKit.Clipboard)host.Control;
        var copies = 0;
        clipboard.Copied += (_, _) => copies++;
        host.Drive(golden, "click");
        await Assert.That(await TopLevel.GetTopLevel(clipboard)!.Clipboard!.TryGetTextAsync()).IsEqualTo("Copied text");
        await Assert.That(clipboard.IsCopied).IsTrue();
        // clipboard.rs: no click handler while the check shows.
        host.Drive(golden, "click");
        await Assert.That(copies).IsEqualTo(1);
        VirtualTime.Advance(TimeSpan.FromMilliseconds(1990));
        await Assert.That(clipboard.IsCopied).IsTrue();
        VirtualTime.Advance(TimeSpan.FromMilliseconds(20));
        await Assert.That(clipboard.IsCopied).IsFalse();
    }

    [Test]
    public async Task An_alert_asks_to_close_and_stays_until_the_app_hides_it()
    {
        var golden = Case("alert/close.default/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var alert = (Alert)host.Control;
        var requests = 0;
        alert.CloseRequested += (_, _) => requests++;
        host.Drive(golden, "click-at-350-36");
        await Assert.That(requests).IsEqualTo(1);
        await Assert.That(alert.IsVisible).IsTrue();
    }

    // rating.rs: a click sets the value to the star, or clears down to the star
    // before when it is already filled; a disabled rating ignores the pointer.
    [Test]
    [Arguments("click-at-86-22", 4)]
    [Arguments("click-at-46-22", 1)]
    [Arguments("click-at-26-22", 0)]
    public async Task A_click_on_a_star_sets_the_rating(string state, int value)
    {
        var golden = Case("rating/pointer.base/at-86-22/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, state);
        await Assert.That(((Rating)host.Control).Value).IsEqualTo(value);
    }

    [Test]
    public async Task A_disabled_rating_ignores_a_click()
    {
        var golden = Case("rating/disabled.base/disabled/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, "click-at-86-22");
        await Assert.That(((Rating)host.Control).Value).IsEqualTo(3);
    }

    [Test]
    public async Task A_breadcrumb_item_clicks_unless_disabled()
    {
        var golden = Case("breadcrumb/disabled.base/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var items = ((Breadcrumb)host.Control).Items.OfType<BreadcrumbItem>().ToArray();
        var clicked = new List<string>();
        foreach (var item in items)
        {
            item.Click += (s, _) => clicked.Add((string)((BreadcrumbItem)s!).Content!);
        }
        foreach (var item in items)
        {
            var center = item.TranslatePoint(new Avalonia.Point(item.Bounds.Width - 4, item.Bounds.Height / 2), host.Window)!.Value;
            host.Drive(golden, $"click-at-{(int)center.X}-{(int)center.Y}");
        }
        await Assert.That(string.Join(",", clicked)).IsEqualTo("Home,2025,Report");
    }

    [Test]
    public async Task An_avatar_group_shows_up_to_its_limit_then_the_ellipsis()
    {
        var golden = Case("avatargroup/ellipsis.medium/normal/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var group = (AvatarGroup)host.Control;
        await Assert.That(group.Items.OfType<Avatar>().Count(a => a.IsVisible)).IsEqualTo(3);
        await Assert.That(group.IsCut).IsTrue();
        group.Limit = 5;
        await Assert.That(group.Items.OfType<Avatar>().Count(a => a.IsVisible)).IsEqualTo(5);
        await Assert.That(group.IsCut).IsFalse();
    }

    // avatar.rs extract_text_initials.
    [Test]
    [Arguments("Jason Lee", "JL")]
    [Arguments("zed", "ZE")]
    [Arguments("Alice", "AL")]
    [Arguments("Ada B Lovelace", "AB")]
    [Arguments("⋯", "⋯")]
    public async Task Avatar_initials_follow_gpui(string name, string initials) =>
        await Assert.That(Avatar.ExtractInitials(name)).IsEqualTo(initials);

    // kbd.rs test_format, for both platforms.
    [Test]
    [Arguments("cmd-a", true, "⌘A")]
    [Arguments("cmd-enter", true, "⌘⏎")]
    [Arguments("shift-pagedown", true, "⇧Page Down")]
    [Arguments("shift-space", true, "⇧Space")]
    [Arguments("cmd-ctrl-a", true, "⌃⌘A")]
    [Arguments("cmd-alt-backspace", true, "⌥⌘⌫")]
    [Arguments("shift-delete", true, "⇧⌫")]
    [Arguments("cmd-ctrl-shift-alt-a", true, "⌃⌥⇧⌘A")]
    [Arguments("a", false, "A")]
    [Arguments("ctrl-a", false, "Ctrl+A")]
    [Arguments("shift-space", false, "Shift+Space")]
    [Arguments("ctrl-alt-shift-win-a", false, "Ctrl+Alt+Shift+Win+A")]
    [Arguments("ctrl-shift-backspace", false, "Ctrl+Shift+Backspace")]
    [Arguments("alt-delete", false, "Alt+Delete")]
    [Arguments("alt-tab", false, "Alt+Tab")]
    [Arguments("f12", false, "F12")]
    public async Task Kbd_writes_keys_as_gpui(string stroke, bool mac, string text) =>
        await Assert.That(Kbd.Format(Adapters.GpuiGesture(stroke), mac)).IsEqualTo(text);

    // hover_card.rs: open 600ms after the pointer enters, kept while the pointer
    // is on the card, closed 300ms after it leaves both.
    [Test]
    public async Task A_hover_card_opens_after_600ms_and_stays_while_the_pointer_is_on_it()
    {
        var golden = Case("hovercard/open.base/hover+wait-700ms/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var card = (HoverCard)host.Control;
        host.Drive(golden, "hover");
        VirtualTime.Advance(TimeSpan.FromMilliseconds(599));
        await Assert.That(card.IsOpen).IsFalse();
        VirtualTime.Advance(TimeSpan.FromMilliseconds(1));
        await Assert.That(card.IsOpen).IsTrue();
        // Onto the card (it opens 4px under the trigger): it stays open.
        host.Drive(golden, "at-145-70+wait-1000ms");
        await Assert.That(card.IsOpen).IsTrue();
        host.Drive(golden, "at-300-190+wait-299ms");
        await Assert.That(card.IsOpen).IsTrue();
        VirtualTime.Advance(TimeSpan.FromMilliseconds(1));
        await Assert.That(card.IsOpen).IsFalse();
    }

    [Test]
    [Arguments("stepper/pointer.base/at-246-28/light", "click-at-246-28", 1)]
    [Arguments("stepper/pointer.base/at-246-28/light", "click-at-465-60", 2)]
    [Arguments("stepper/pointer.base/disabled+at-246-28/light", "click-at-246-28", 0)]
    public async Task A_click_on_a_step_selects_it_unless_disabled(string id, string state, int selected)
    {
        var golden = Case(id);
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        host.Drive(golden, state);
        await Assert.That(((Stepper)host.Control).SelectedIndex).IsEqualTo(selected);
    }

    // description_list.rs test_group_item_rows.
    [Test]
    public async Task Description_items_group_into_rows_as_gpui()
    {
        int[] spans = [1, 2, 1, 1, 1, 3, 1];
        var rows = DescriptionList.GroupRows(spans.Select(s => new DescriptionItem { Span = s }), 3);
        await Assert.That(string.Join(",", rows.Select(r => r.Count))).IsEqualTo("2,3,1,1");
    }
}
