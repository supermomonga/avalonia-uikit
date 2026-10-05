using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Notifications;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Infrastructure;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// What uikit:NotificationList and uikit:TitleBar do beyond their look: GPUI's
/// notification lifecycle and stack (notification.rs, base/toast.rs and their
/// tests), and the title bar's window controls, drag areas and double click
/// (title_bar.rs). GPUI on macOS draws no caption buttons (R14), so they are
/// checked here.
/// </summary>
public class ShellBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    private sealed record Kind;

    private sealed record OtherKind;

    private static (CaseHost Host, NotificationList List) OpenList(double height = 360)
    {
        var golden = Case("uikit-notification/type.info/wait-1000ms/light") with { Viewport = new Size(430, height) };
        var list = new NotificationList { Width = 430, Height = height };
        return (CaseHost.Open(golden, list), list);
    }

    private static Point Center(CaseHost host, Visual visual) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), host.Window)!.Value;

    private static Point TopOf(CaseHost host, Visual visual) => visual.TranslatePoint(default, host.Window)!.Value;

    private static void Wait(int ms) => VirtualTime.Advance(TimeSpan.FromMilliseconds(ms));

    private static NotificationStack StackOf(NotificationItem item) => (NotificationStack)item.Parent!;

    /// <summary>The list's notifications by message, oldest first.</summary>
    private static string Messages(NotificationList list) => string.Join(",", list.Notifications.Select(n => n.Message));

    // notification.rs: 400ms to enter, then 5s (Duration::from_secs(5)) on the
    // 50ms lifecycle clock, then the 200ms exit before the toast is gone.
    [Test]
    public async Task A_notification_hides_itself_5s_after_it_came_in()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var item = list.Show(NotificationType.Information, "Saved.");
            Wait(5350);
            await Assert.That(item.IsClosing).IsFalse();
            Wait(50);
            await Assert.That(item.IsClosing).IsTrue();
            Wait(190);
            await Assert.That(list.Notifications.Contains(item)).IsTrue();
            Wait(20);
            host.Flush();
            await Assert.That(list.Notifications).IsEmpty();
            await Assert.That(host.Window.GetVisualDescendants().OfType<NotificationItem>()).IsEmpty();
        }
    }

    // notification.rs push: an action turns autohide off; autohide(true) after it turns it back on.
    [Test]
    public async Task A_notification_with_an_action_stays_unless_it_hides_itself()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var action = list.Show(new NotificationItem(null, "Retry?") { Action = new Button { Content = "Retry" } });
            var hiding = list.Show(new NotificationItem(null, "Retry?") { Action = new Button { Content = "Retry" }, AutoHide = true });
            var staying = list.Show(new NotificationItem(null, "Kept.") { AutoHide = false });
            Wait(6000);
            await Assert.That(action.IsClosing).IsFalse();
            await Assert.That(staying.IsClosing).IsFalse();
            await Assert.That(hiding.IsClosing).IsTrue();
            // notification.rs: the action is a small button.
            await Assert.That(((Button)action.Action!).Classes).Contains("small");
        }
    }

    // base/toast.rs advance(now, paused): the countdown stops while the pointer
    // is over a stack (notification.md: "pauses while the pointer is over the
    // notifications") and goes on from where it was once the pointer leaves.
    [Test]
    public async Task The_pointer_over_the_notifications_pauses_their_countdown()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var item = list.Show(NotificationType.Information, "Saved.");
            Wait(1000);
            host.Window.MouseMove(Center(host, item));
            host.Flush();
            await Assert.That(list.IsExpanded).IsTrue();
            Wait(10_000);
            await Assert.That(item.IsClosing).IsFalse();
            host.Window.MouseMove(new Point(5, 300));
            host.Flush();
            await Assert.That(list.IsExpanded).IsFalse();
            // 600ms of the 5s ran before the pointer came.
            Wait(4350);
            await Assert.That(item.IsClosing).IsFalse();
            Wait(50);
            await Assert.That(item.IsClosing).IsTrue();
        }
    }

    // notification.rs focus_pauses_autohide_and_present_phase_is_projected: the
    // focus in a stack spreads it and pauses; focus elsewhere resumes.
    [Test]
    public async Task The_focus_in_a_stack_spreads_it_and_pauses_the_countdown()
    {
        var golden = Case("uikit-notification/type.info/wait-1000ms/light") with { Viewport = new Size(430, 360) };
        var other = new TextBox { Width = 100, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom };
        var list = new NotificationList();
        using var host = CaseHost.Open(golden, new Panel { Width = 430, Height = 360, Children = { other, list } });
        {
            var item = list.Show(NotificationType.Information, "Saved.");
            Wait(400);
            StackOf(item).Focus(NavigationMethod.Tab);
            host.Flush();
            await Assert.That(list.IsExpanded).IsTrue();
            Wait(5000);
            await Assert.That(item.IsClosing).IsFalse();
            other.Focus();
            host.Flush();
            await Assert.That(list.IsExpanded).IsFalse();
            Wait(5050);
            await Assert.That(item.IsClosing).IsTrue();
        }
    }

    // base/toast.rs manager_replaces_duplicate_ids_as_the_newest_toast: the same
    // id replaces at once (no exit, no on_close) and is the newest.
    [Test]
    public async Task Showing_a_notification_with_the_same_id_replaces_it_as_the_newest()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var closed = 0;
            var first = list.Show(new NotificationItem(null, "Uploading…") { Id = typeof(Kind) });
            first.NotificationClosed += (_, _) => closed++;
            var other = list.Show(new NotificationItem(null, "Other") { Id = typeof(OtherKind) });
            var second = list.Show(new NotificationItem(NotificationType.Success, "Uploaded.") { Id = typeof(Kind) });
            host.Flush();
            await Assert.That(Messages(list)).IsEqualTo("Other,Uploaded.");
            await Assert.That(first.GetVisualParent()).IsNull();
            await Assert.That(closed).IsEqualTo(0);
            // Without an id, notifications never replace each other.
            list.Show("A");
            list.Show("A");
            await Assert.That(list.Notifications.Count).IsEqualTo(4);
        }
    }

    // notification.rs close_by_type_removes_id_and_all_id1_of_same_type,
    // close_with_id_and_element_id_removes_only_matching_key,
    // close_by_type_with_no_match_is_noop and clear.
    [Test]
    public async Task Remove_closes_by_id_or_by_id_and_key()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var plain = list.Show(new NotificationItem(null, "plain") { Id = typeof(Kind), AutoHide = false });
            var a = list.Show(new NotificationItem(null, "a") { Id = typeof(Kind), Key = 1, AutoHide = false });
            var b = list.Show(new NotificationItem(null, "b") { Id = typeof(Kind), Key = 2, AutoHide = false });
            var bar = list.Show(new NotificationItem(null, "bar") { Id = typeof(OtherKind), AutoHide = false });
            list.Remove(typeof(Kind), 1);
            await Assert.That(a.IsClosing).IsTrue();
            await Assert.That(new[] { plain, b, bar }.Any(n => n.IsClosing)).IsFalse();
            list.Remove("nothing");
            await Assert.That(new[] { plain, b, bar }.Any(n => n.IsClosing)).IsFalse();
            list.Remove(typeof(Kind));
            await Assert.That(plain.IsClosing && b.IsClosing).IsTrue();
            await Assert.That(bar.IsClosing).IsFalse();
            Wait(250);
            host.Flush();
            await Assert.That(Messages(list)).IsEqualTo("bar");
            list.Clear();
            await Assert.That(bar.IsClosing).IsTrue();
        }
    }

    // base/toast.rs manager_limit_keeps_ending_toasts_mounted: the newest open
    // notifications up to the limit, and every closing one, show; an older one
    // shows again when a newer one closes.
    [Test]
    public async Task Only_the_newest_open_notifications_show_up_to_the_limit()
    {
        var (host, list) = OpenList();
        using (host)
        {
            list.MaxItems = 1;
            var a = list.Show(new NotificationItem(null, "a") { AutoHide = false });
            var b = list.Show(new NotificationItem(null, "b") { AutoHide = false });
            var c = list.Show(new NotificationItem(null, "c") { AutoHide = false });
            await Assert.That(string.Join(",", new[] { a, b, c }.Select(n => n.IsVisible))).IsEqualTo("False,False,True");
            c.Close();
            await Assert.That(string.Join(",", new[] { a, b, c }.Select(n => n.IsVisible))).IsEqualTo("False,True,True");
            Wait(250);
            host.Flush();
            await Assert.That(Messages(list)).IsEqualTo("a,b");
            await Assert.That(b.IsVisible).IsTrue();
        }
    }

    // notification.rs per_notification_placement_stacks_by_anchor: one stack per
    // placement; the stack goes with its last notification; the list's
    // placement is the default (grouped).
    [Test]
    public async Task Each_placement_has_its_own_stack()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var top = list.Show(new NotificationItem(null, "default") { AutoHide = false });
            var bottom = list.Show(new NotificationItem(null, "bottom") { Placement = NotificationPlacement.BottomLeft, AutoHide = false });
            host.Flush();
            await Assert.That(list.GetVisualChildren().Count()).IsEqualTo(2);
            await Assert.That(StackOf(top).Placement).IsEqualTo(NotificationPlacement.TopRight);
            await Assert.That(StackOf(bottom).Placement).IsEqualTo(NotificationPlacement.BottomLeft);
            // GPUI's margins: 50px from the top, 16px from the other edges.
            await Assert.That(TopOf(host, StackOf(top))).IsEqualTo(new Point(430 - 16 - 382, 50));
            await Assert.That(TopOf(host, StackOf(bottom)).X).IsEqualTo(16);
            bottom.Close();
            Wait(250);
            host.Flush();
            await Assert.That(list.GetVisualChildren().Count()).IsEqualTo(1);
            // A notification without its own placement follows the list's.
            list.Placement = NotificationPlacement.BottomRight;
            host.Flush();
            await Assert.That(StackOf(top).Placement).IsEqualTo(NotificationPlacement.BottomRight);
            await Assert.That(list.GetVisualChildren().Count()).IsEqualTo(1);
        }
    }

    // notification.rs on_click and on_aux_click: a click closes the card only
    // when it has a click handler (then calls it), a middle click always, the
    // close button always; a click on the action is the action's.
    [Test]
    public async Task Clicks_close_a_notification_as_gpui()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var plain = list.Show(new NotificationItem(null, "No handler") { AutoHide = false });
            Wait(1000);
            var at = Center(host, plain);
            host.Window.MouseDown(at, MouseButton.Left);
            host.Window.MouseUp(at, MouseButton.Left);
            await Assert.That(plain.IsClosing).IsFalse();
            host.Window.MouseDown(at, MouseButton.Middle);
            host.Window.MouseUp(at, MouseButton.Middle);
            await Assert.That(plain.IsClosing).IsTrue();
            Wait(250);

            var clicks = 0;
            var clicked = new NotificationItem(null, "Handled") { AutoHide = false };
            clicked.Click += (_, _) => clicks++;
            list.Show(clicked);
            Wait(1000);
            at = Center(host, clicked);
            host.Window.MouseDown(at, MouseButton.Left);
            host.Window.MouseUp(at, MouseButton.Left);
            await Assert.That(clicks).IsEqualTo(1);
            await Assert.That(clicked.IsClosing).IsTrue();
            Wait(250);

            var action = new Button { Content = "Retry" };
            var acted = 0;
            action.Click += (_, _) => acted++;
            var withAction = new NotificationItem(null, "With an action") { Action = action };
            withAction.Click += (_, _) => clicks++;
            list.Show(withAction);
            Wait(1000);
            at = Center(host, action);
            host.Window.MouseMove(at);
            host.Window.MouseDown(at, MouseButton.Left);
            host.Window.MouseUp(at, MouseButton.Left);
            await Assert.That(acted).IsEqualTo(1);
            await Assert.That(clicks).IsEqualTo(1);
            await Assert.That(withAction.IsClosing).IsFalse();
            var close = withAction.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_CloseButton");
            at = Center(host, close);
            host.Window.MouseMove(at);
            host.Flush();
            await Assert.That(close.Opacity).IsEqualTo(1);
            host.Window.MouseDown(at, MouseButton.Left);
            host.Window.MouseUp(at, MouseButton.Left);
            await Assert.That(withAction.IsClosing).IsTrue();
            await Assert.That(clicks).IsEqualTo(1);
        }
    }

    // base/toast.rs stack_geometry_anchors_newest_item_and_supports_variable_heights.
    [Test]
    public async Task The_stack_geometry_is_gpuis()
    {
        var (collapsed, expanded, top) = NotificationStack.StackGeometry([40, 60, 80], 12, 12, false);
        await Assert.That(collapsed).IsEqualTo(104);
        await Assert.That(expanded).IsEqualTo(204);
        await Assert.That(top).IsEquivalentTo([(24d, 164d), (12d, 92d), (0d, 0d)]);
        var (_, _, bottom) = NotificationStack.StackGeometry([40, 60, 80], 12, 12, true);
        await Assert.That(bottom).IsEquivalentTo([(40d, 0d), (32d, 52d), (24d, 124d)]);

        var (tallCollapsed, _, tallTop) = NotificationStack.StackGeometry([180, 60], 14, 14, false);
        await Assert.That(tallCollapsed).IsEqualTo(194);
        await Assert.That(tallTop[0].Collapsed).IsEqualTo(14);
        await Assert.That(tallTop[0].Collapsed - tallTop[1].Collapsed).IsEqualTo(14);
        var (_, _, tallBottom) = NotificationStack.StackGeometry([180, 60], 14, 14, true);
        await Assert.That(tallBottom[0].Collapsed >= 0 && tallBottom[0].Collapsed + 180 <= tallCollapsed).IsTrue();
        await Assert.That(tallBottom[1].Collapsed + 60 - (tallBottom[0].Collapsed + 180)).IsEqualTo(14);
    }

    // base/toast.rs stack_expands_for_hover_and_focus and
    // keyed_stack_reflow_moves_from_the_current_visual_position: a new card
    // pushes the older one back 14px (and narrower) on the spring, from where it
    // was; the pointer over the stack spreads it and away collapses it.
    [Test]
    public async Task The_stack_moves_on_springs_and_spreads_under_the_pointer()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var first = list.Show(new NotificationItem(null, "First") { AutoHide = false });
            Wait(1000);
            var initial = TopOf(host, first);
            var width = first.Bounds.Width;
            await Assert.That(initial).IsEqualTo(new Point(32, 50));
            var second = list.Show(new NotificationItem(null, "Second") { AutoHide = false });
            host.Flush();
            await Assert.That(TopOf(host, first).Y).IsEqualTo(initial.Y);
            Wait(150);
            var middle = TopOf(host, first).Y;
            await Assert.That(middle > initial.Y && middle < initial.Y + 14).IsTrue();
            Wait(1000);
            await Assert.That(TopOf(host, first).Y).IsEqualTo(initial.Y + 14);
            await Assert.That(first.Bounds.Width).IsLessThan(second.Bounds.Width);
            await Assert.That(second.Bounds.Width).IsEqualTo(width);

            var stack = StackOf(first);
            host.Window.MouseMove(Center(host, second));
            host.Flush();
            await Assert.That(stack.IsExpanded).IsTrue();
            Wait(1000);
            // Spread: the newest on top, the older one 14px below it at full width.
            await Assert.That(TopOf(host, first).Y).IsEqualTo(50 + second.Bounds.Height + 14);
            await Assert.That(first.Bounds.Width).IsEqualTo(width);
            host.Window.MouseMove(new Point(5, 340));
            host.Flush();
            await Assert.That(stack.IsExpanded).IsFalse();
        }
    }

    // A fourth card back is not painted while the stack is collapsed (ToastStack: invisible).
    [Test]
    public async Task A_collapsed_stack_shows_three_cards()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var items = Enumerable.Range(0, 4).Select(i => list.Show(new NotificationItem(null, $"#{i}") { AutoHide = false })).ToList();
            Wait(1500);
            await Assert.That(string.Join(",", items.Select(i => i.Opacity))).IsEqualTo("0,1,1,1");
            await Assert.That(items[0].IsHitTestVisible).IsFalse();
            host.Window.MouseMove(Center(host, items[3]));
            Wait(1500);
            await Assert.That(string.Join(",", items.Select(i => i.Opacity))).IsEqualTo("1,1,1,1");
        }
    }

    // notification.rs: a type's icon, the app's icon without a type, or none.
    [Test]
    public async Task A_notification_shows_its_types_icon_or_its_own()
    {
        var (host, list) = OpenList();
        using (host)
        {
            var typed = list.Show(new NotificationItem(NotificationType.Success, "Paid.") { Icon = IconName.Bell });
            var own = list.Show(new NotificationItem(null, "Mail.") { Icon = IconName.Bell });
            var none = list.Show(new NotificationItem(null, "Plain."));
            host.Flush();
            PathIcon TypeIcon(NotificationItem n) => n.GetVisualDescendants().OfType<PathIcon>().Single(i => i.Name == "PART_Icon");
            await Assert.That(TypeIcon(typed).IsEffectivelyVisible).IsTrue();
            await Assert.That(ReferenceEquals(TypeIcon(typed).Data, host.Window.FindResource("UIKit.Icon.CircleCheck"))).IsTrue();
            await Assert.That(own.GetVisualDescendants().OfType<AvaloniaUIKit.Icon>().Single().Kind).IsEqualTo(IconName.Bell);
            await Assert.That(TypeIcon(own).IsVisible).IsFalse();
            await Assert.That(TypeIcon(none).IsVisible).IsFalse();
            await Assert.That(none.GetVisualDescendants().OfType<AvaloniaUIKit.Icon>()).IsEmpty();
            // NotificationCard's own type follows.
            await Assert.That(typed.NotificationType).IsEqualTo(NotificationType.Success);
        }
    }

    // A list for a TopLevel sits over its content in the adorner layer, as
    // WindowNotificationManager does, and takes no input outside its cards.
    [Test]
    public async Task A_list_for_a_window_sits_in_its_adorner_layer()
    {
        var golden = Case("uikit-notification/type.info/wait-1000ms/light") with { Viewport = new Size(430, 360) };
        var button = new Button { Content = "Under", Width = 430, Height = 360 };
        using var host = CaseHost.Open(golden, button);
        var list = new NotificationList(host.Window);
        host.Flush();
        await Assert.That(list.GetVisualParent()).IsTypeOf<Avalonia.Controls.Primitives.AdornerLayer>();
        await Assert.That(list.Bounds.Size).IsEqualTo(new Size(430, 360));
        list.Show(new NotificationItem(null, "Over") { AutoHide = false });
        Wait(1000);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        host.Window.MouseDown(new Point(100, 300), MouseButton.Left);
        host.Window.MouseUp(new Point(100, 300), MouseButton.Left);
        await Assert.That(clicks).IsEqualTo(1);
    }

    private static (CaseHost Host, TitleBar Bar) OpenBar(TitleBar bar, double width = 480)
    {
        var golden = Case("uikit-titlebar/bar.base/normal/light") with { Viewport = new Size(width, 34) };
        bar.Width = width;
        return (CaseHost.Open(golden, bar), bar);
    }

    private static List<Button> CaptionButtons(TitleBar bar) =>
        bar.GetVisualDescendants().OfType<Button>().Where(b => b.Name?.StartsWith("PART_", StringComparison.Ordinal) == true && b.IsEffectivelyVisible).ToList();

    // title_bar.rs ControlIcon: 34 x 33 at the bar's right end with 14px icons,
    // secondary when hovered, danger for close; restore while maximized.
    [Test]
    public async Task Caption_buttons_take_gpui_sizes_and_colors()
    {
        var (host, bar) = OpenBar(new TitleBar { ShowsCaptionButtons = true });
        using (host)
        {
            var buttons = CaptionButtons(bar);
            await Assert.That(string.Join(",", buttons.Select(b => b.Name))).IsEqualTo("PART_MinimizeButton,PART_MaximizeButton,PART_CloseButton");
            await Assert.That(buttons.Select(b => new Rect(b.TranslatePoint(default, bar)!.Value, b.Bounds.Size))).IsEquivalentTo([new Rect(378, 0, 34, 33), new Rect(412, 0, 34, 33), new Rect(446, 0, 34, 33)]);
            await Assert.That(buttons.SelectMany(b => b.GetVisualDescendants().OfType<PathIcon>()).All(i => i.Bounds.Size == new Size(14, 14))).IsTrue();
            await Assert.That(buttons.Select(WindowDecorationProperties.GetElementRole)).IsEquivalentTo(
                [WindowDecorationsElementRole.MinimizeButton, WindowDecorationsElementRole.MaximizeButton, WindowDecorationsElementRole.CloseButton]);

            Color Brush(string key) => ((ISolidColorBrush)host.Window.FindResource(host.Window.ActualThemeVariant, key)!).Color;
            Color? Fill(Button b) => (b.Background as ISolidColorBrush)?.Color;
            host.Window.MouseMove(new Point(463, 16));
            host.Flush();
            await Assert.That(Fill(buttons[2])).IsEqualTo(Brush("UIKit.Danger"));
            host.Window.MouseMove(new Point(429, 16));
            host.Flush();
            await Assert.That(Fill(buttons[1])).IsEqualTo(Brush("UIKit.SecondaryHover"));
            await Assert.That(Fill(buttons[2])).IsEqualTo(Colors.Transparent);
        }
    }

    // title_bar.rs: minimize, maximize and restore (zoom_window), close.
    [Test]
    public async Task Caption_buttons_minimize_maximize_and_close_the_window()
    {
        var (host, bar) = OpenBar(new TitleBar { ShowsCaptionButtons = true });
        using (host)
        {
            var buttons = CaptionButtons(bar);
            void Click(Button button)
            {
                var at = Center(host, button);
                host.Window.MouseMove(at);
                host.Window.MouseDown(at, MouseButton.Left);
                host.Window.MouseUp(at, MouseButton.Left);
                host.Flush();
            }
            var icon = buttons[1].GetVisualDescendants().OfType<PathIcon>().Single();
            await Assert.That(ReferenceEquals(icon.Data, host.Window.FindResource("UIKit.Icon.WindowMaximize"))).IsTrue();
            Click(buttons[1]);
            await Assert.That(host.Window.WindowState).IsEqualTo(WindowState.Maximized);
            await Assert.That(ReferenceEquals(icon.Data, host.Window.FindResource("UIKit.Icon.WindowRestore"))).IsTrue();
            Click(buttons[1]);
            await Assert.That(host.Window.WindowState).IsEqualTo(WindowState.Normal);
            Click(buttons[0]);
            await Assert.That(host.Window.WindowState).IsEqualTo(WindowState.Minimized);
            host.Window.WindowState = WindowState.Normal;
            var closed = false;
            host.Window.Closed += (_, _) => closed = true;
            Click(buttons[2]);
            await Assert.That(closed).IsTrue();
        }
    }

    // title_bar.rs WindowControls: only the controls the window supports, and
    // none by default unless the window extends into its decorations.
    [Test]
    public async Task Caption_buttons_follow_the_window()
    {
        var (host, bar) = OpenBar(new TitleBar());
        using (host)
        {
            await Assert.That(CaptionButtons(bar)).IsEmpty();
            bar.ShowsCaptionButtons = true;
            host.Window.CanMinimize = false;
            host.Flush();
            await Assert.That(string.Join(",", CaptionButtons(bar).Select(b => b.Name))).IsEqualTo("PART_MaximizeButton,PART_CloseButton");
            host.Window.CanMaximize = false;
            host.Flush();
            await Assert.That(string.Join(",", CaptionButtons(bar).Select(b => b.Name))).IsEqualTo("PART_CloseButton");
        }
    }

    // title_bar.rs: the bar's children spread along it (justify_between),
    // vertically centered in the 33px over the rule, 12px in (80px beside
    // macOS's traffic lights when the window extends into its decorations).
    [Test]
    public async Task Children_spread_along_the_bar()
    {
        var title = new TextBlock { Text = "My Application", FontSize = 14, LineHeight = 22.5 };
        var button = new Button { Classes = { "ghost", "small", "icon-only" }, Content = Adapters.Icon("bell") };
        var (host, bar) = OpenBar(new TitleBar { Items = { title, button }, ShowsCaptionButtons = true });
        using (host)
        {
            await Assert.That(bar.Padding).IsEqualTo(new Thickness(12, 0, 0, 1));
            var at = title.TranslatePoint(default, bar)!.Value;
            await Assert.That(at.X).IsEqualTo(12);
            // Centered over the rule, to a device pixel (the layout rounds the half).
            await Assert.That(Math.Abs(at.Y + title.Bounds.Height / 2 - 16.5)).IsLessThanOrEqualTo(0.25);
            var end = button.TranslatePoint(new Point(button.Bounds.Width, 0), bar)!.Value;
            await Assert.That(end.X).IsEqualTo(480 - 3 * 34);
        }
    }

    // The bar is the system's title bar area on Windows and macOS (none
    // elsewhere); focusable controls in it take their own input (User), text
    // and the bar's empty areas drag.
    [Test]
    public async Task The_bar_marks_its_drag_area_and_its_controls()
    {
        var title = new TextBlock { Text = "Title" };
        var button = new Button { Content = "Menu" };
        var (host, bar) = OpenBar(new TitleBar { Items = { title, button } });
        using (host)
        {
            var root = bar.GetVisualDescendants().OfType<Panel>().First(p => p.Name == "PART_Root");
            var drawn = bar.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_TitleBar");
            await Assert.That(WindowDecorationProperties.GetElementRole(root)).IsEqualTo(TitleBar.BarRole);
            await Assert.That(WindowDecorationProperties.GetElementRole(drawn)).IsEqualTo(TitleBar.BarRole);
            await Assert.That(TitleBar.BarRole).IsEqualTo(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
                ? WindowDecorationsElementRole.TitleBar : WindowDecorationsElementRole.None);
            await Assert.That(WindowDecorationProperties.GetElementRole(button)).IsEqualTo(WindowDecorationsElementRole.User);
            await Assert.That(WindowDecorationProperties.GetElementRole(title)).IsEqualTo(WindowDecorationsElementRole.None);
        }
    }

    // title_bar.rs on_double_click: zoom_window; a double click on a control in the bar is the control's.
    [Test]
    public async Task A_double_click_on_the_bar_maximizes_and_restores_the_window()
    {
        var button = new Button { Content = "Menu" };
        var (host, bar) = OpenBar(new TitleBar { Items = { new TextBlock { Text = "Title" }, button } });
        using (host)
        {
            void DoubleClick(Point at)
            {
                host.Window.MouseMove(at);
                host.Window.MouseDown(at, MouseButton.Left);
                host.Window.MouseUp(at, MouseButton.Left);
                host.Window.MouseDown(at, MouseButton.Left);
                host.Window.MouseUp(at, MouseButton.Left);
                host.Flush();
            }
            // Each pair somewhere else, so it is not the third and fourth click of a run.
            DoubleClick(new Point(240, 17));
            await Assert.That(host.Window.WindowState).IsEqualTo(WindowState.Maximized);
            DoubleClick(new Point(200, 17));
            await Assert.That(host.Window.WindowState).IsEqualTo(WindowState.Normal);
            DoubleClick(Center(host, button));
            await Assert.That(host.Window.WindowState).IsEqualTo(WindowState.Normal);
        }
    }
}
