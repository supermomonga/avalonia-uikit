using System.Collections;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Infrastructure;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// Closing, adding and dragging tabs (uikit:Tabs.Closable, NewTabFactory,
/// Reorderable, DragGroup and DetachedWindowFactory; ADR 33). GPUI Kit's
/// TabBar has none of them, so there is no reference to compare: these check
/// what they do to the items. Headless windows all sit at the screen's origin,
/// so bars meant to be apart are put at different places in their windows.
/// </summary>
public class TabsEditingBehaviorTests
{
    private static readonly string[] Labels = ["Account", "Profile", "Settings"];

    /// <summary>
    /// The windows a test opens, closed at its end: a bar left open would be a
    /// drop target for the next test's tabs (every headless window is at the origin).
    /// </summary>
    private sealed class Windows : IDisposable
    {
        private readonly List<Window> _open = [];

        public Window Open(Control content)
        {
            var window = Track(new Window { Width = 480, Height = 240, Content = content });
            window.Show();
            Flush();
            return window;
        }

        public Window Track(Window window)
        {
            VirtualTime.Attach(window);
            _open.Add(window);
            return window;
        }

        // Makes the windows a dragged-off tab moves to: one bar of the group, empty.
        public Func<object?, Window?> Factory(List<Window> made, List<TabStrip> bars, string group = "files") => _ =>
        {
            var bar = Strip(new ObservableCollection<string>(), group);
            bars.Add(bar);
            var window = Track(new Window { Width = 480, Height = 240, Content = bar });
            made.Add(window);
            return window;
        };

        public void Dispose()
        {
            foreach (var window in _open)
            {
                window.Close();
            }
            Flush();
        }
    }

    private static void Flush()
    {
        VirtualTime.Tick();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    private static TabStrip Strip(IEnumerable? items = null, string? group = null)
    {
        var strip = new TabStrip
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            ItemsSource = items ?? new ObservableCollection<string>(Labels),
            SelectedIndex = 0,
        };
        Tabs.SetReorderable(strip, true);
        Tabs.SetDragGroup(strip, group);
        return strip;
    }

    private static string Order(SelectingItemsControl bar) =>
        string.Join(", ", bar.Items.Cast<object>().Select(Label));

    private static string Label(object? item) => item switch
    {
        HeaderedContentControl tab => $"{tab.Header}",
        ContentControl tab => $"{tab.Content}",
        _ => $"{item}",
    };

    private static Control Tab(SelectingItemsControl bar, int index) => bar.ContainerFromIndex(index)!;

    // A point in a tab, in its window's coordinates: fx and fy of its size.
    private static Point In(Control control, double fx = 0.5, double fy = 0.5) =>
        control.TranslatePoint(new Point(control.Bounds.Width * fx, control.Bounds.Height * fy), TopLevel.GetTopLevel(control)!)!.Value;

    private static T Part<T>(Control templated, string name) where T : Control =>
        templated.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == templated);

    private static void Click(Window window, Point at)
    {
        window.MouseMove(at);
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Flush();
    }

    private static void Press(Window window, Point at)
    {
        window.MouseMove(at);
        window.MouseDown(at, MouseButton.Left);
        Flush();
    }

    // Moves the pointer with the button held, in steps, as a hand does.
    private static void DragTo(Window window, Point from, Point to, int steps = 10)
    {
        for (var i = 1; i <= steps; i++)
        {
            window.MouseMove(from + (to - from) * i / steps, RawInputModifiers.LeftMouseButton);
            Flush();
        }
    }

    // As DragTo, to points on the screen: a window that follows the pointer gets them
    // at the same place in itself, as from the system.
    private static void DragOnScreen(Window window, PixelPoint from, PixelPoint to, int steps = 10)
    {
        for (var i = 1; i <= steps; i++)
        {
            var at = new PixelPoint(from.X + (to.X - from.X) * i / steps, from.Y + (to.Y - from.Y) * i / steps);
            window.MouseMove(window.PointToClient(at), RawInputModifiers.LeftMouseButton);
            Flush();
        }
    }

    private static PixelPoint OnScreen(Control control, double fx = 0.5, double fy = 0.5) =>
        TopLevel.GetTopLevel(control)!.PointToScreen(In(control, fx, fy));

    private static void Release(Window window, Point at)
    {
        window.MouseUp(at, MouseButton.Left);
        Flush();
    }

    private static void Drag(Window window, Point from, Point to)
    {
        Press(window, from);
        DragTo(window, from, to);
        Release(window, to);
    }

    [Test]
    public async Task A_closable_tab_closes_and_the_next_one_is_selected()
    {
        using var windows = new Windows();
        var strip = Strip();
        Tabs.SetClosable(strip, true);
        strip.SelectedIndex = 1;
        var window = windows.Open(strip);
        Click(window, In(Part<Button>(Tab(strip, 1), "PART_CloseButton")));
        await Assert.That(Order(strip)).IsEqualTo("Account, Settings");
        await Assert.That(strip.SelectedItem).IsEqualTo("Settings");
    }

    [Test]
    public async Task Closing_the_last_tab_selects_the_one_before()
    {
        using var windows = new Windows();
        var strip = Strip();
        Tabs.SetClosable(strip, true);
        strip.SelectedIndex = 2;
        var window = windows.Open(strip);
        Click(window, In(Part<Button>(Tab(strip, 2), "PART_CloseButton")));
        await Assert.That(Order(strip)).IsEqualTo("Account, Profile");
        await Assert.That(strip.SelectedItem).IsEqualTo("Profile");
    }

    [Test]
    public async Task Closing_a_tab_can_be_cancelled()
    {
        using var windows = new Windows();
        var strip = Strip();
        Tabs.SetClosable(strip, true);
        object? closing = null;
        Tabs.AddTabClosingHandler(strip, (_, e) =>
        {
            closing = e.Item;
            e.Cancel = true;
        });
        var window = windows.Open(strip);
        Click(window, In(Part<Button>(Tab(strip, 0), "PART_CloseButton")));
        await Assert.That(closing).IsEqualTo("Account");
        await Assert.That(Order(strip)).IsEqualTo("Account, Profile, Settings");
    }

    // On a tab, Closable is that tab's; on the bar, every tab's (inherited).
    [Test]
    public async Task Closable_on_a_tab_shows_its_button_only()
    {
        using var windows = new Windows();
        var tabs = new TabControl();
        foreach (var label in Labels)
        {
            tabs.Items.Add(new TabItem { Header = label });
        }
        Tabs.SetClosable((TabItem)tabs.Items[0]!, true);
        windows.Open(tabs);
        bool Shows(int index) => Part<Button>(Tab(tabs, index), "PART_CloseButton").IsVisible;
        await Assert.That(Shows(0)).IsTrue();
        await Assert.That(Shows(1)).IsFalse();
        Tabs.SetClosable(tabs, true);
        Tabs.SetClosable((TabItem)tabs.Items[2]!, false);
        await Assert.That(Shows(1)).IsTrue();
        await Assert.That(Shows(2)).IsFalse();
    }

    [Test]
    public async Task The_add_button_follows_the_last_tab_and_adds_a_selected_tab()
    {
        using var windows = new Windows();
        var strip = Strip();
        var added = 0;
        Tabs.SetNewTabFactory(strip, () => $"Tab {++added}");
        var window = windows.Open(strip);
        var add = Part<Button>(strip, "PART_AddButton");
        var last = Tab(strip, 2);
        await Assert.That(add.IsVisible).IsTrue();
        await Assert.That(add.TranslatePoint(default, strip)!.Value.X).IsEqualTo(last.TranslatePoint(new Point(last.Bounds.Width, 0), strip)!.Value.X + 4).Within(0.01);
        Click(window, In(add));
        await Assert.That(Order(strip)).IsEqualTo("Account, Profile, Settings, Tab 1");
        await Assert.That(strip.SelectedItem).IsEqualTo("Tab 1");
        Tabs.SetNewTabFactory(strip, null);
        Flush();
        await Assert.That(add.IsVisible).IsFalse();
    }

    // The Plus's 1px strokes lie on its center lines: at 100% they would fall between two
    // device pixels and paint both half dark (uikit:IconStrokes.SnapsToPixels).
    [Test]
    [Arguments(1.0)]
    [Arguments(2.0)]
    public async Task The_add_button_draws_its_plus_on_whole_pixels(double scale)
    {
        using var windows = new Windows();
        var strip = Strip();
        Tabs.SetNewTabFactory(strip, () => "New");
        var window = windows.Track(new Window { Width = 480, Height = 120, Content = strip });
        window.SetRenderScaling(scale);
        window.Show();
        Flush();
        var icon = Part<Button>(strip, "PART_AddButton").GetVisualDescendants().OfType<PathIcon>().First();
        using var frame = window.CaptureRenderedFrame()!;
        var image = Rendering.RgbaImage.FromFrame(frame);
        // Across the icon's middle: the horizontal stroke, solid wherever it is drawn.
        var origin = icon.TranslatePoint(default, window)!.Value * scale;
        var y = (int)Math.Floor(origin.Y + 6 * scale);
        var partial = Enumerable.Range((int)origin.X + (int)(5 * scale), (int)(2 * scale))
            .Select(x => image.Pixel(x, y)[0])
            .Count(level => level is > 60 and < 200);
        await Assert.That(partial).IsEqualTo(0);
    }

    [Test]
    public async Task A_dragged_tab_follows_the_pointer_and_the_drop_reorders_the_items()
    {
        using var windows = new Windows();
        var strip = Strip();
        var window = windows.Open(strip);
        var account = Tab(strip, 0);
        var profile = Tab(strip, 1);
        var start = In(account);
        var past = In(profile, 0.75);
        Press(window, start);
        DragTo(window, start, past);
        VirtualTime.Advance(TimeSpan.FromMilliseconds(250));
        Flush();
        // Account sits under the pointer, Profile has slid into its place.
        await Assert.That(account.TranslatePoint(default, strip)!.Value.X).IsEqualTo(past.X - start.X).Within(0.01);
        await Assert.That(profile.TranslatePoint(default, strip)!.Value.X).IsEqualTo(0).Within(0.5);
        await Assert.That(Order(strip)).IsEqualTo("Account, Profile, Settings");
        Release(window, past);
        await Assert.That(Order(strip)).IsEqualTo("Profile, Account, Settings");
        await Assert.That(strip.SelectedItem).IsEqualTo("Account");
        await Assert.That(Tab(strip, 1).RenderTransform).IsNull();
        await Assert.That(Tab(strip, 0).RenderTransform).IsNull();
    }

    // A TabControl's tabs declared in XAML are their own items: the drop moves the TabItems.
    [Test]
    public async Task A_tab_control_reorders_its_items()
    {
        using var windows = new Windows();
        var tabs = new TabControl { HorizontalAlignment = HorizontalAlignment.Left };
        foreach (var label in Labels)
        {
            tabs.Items.Add(new TabItem { Header = label, Content = new TextBlock { Text = label } });
        }
        Tabs.SetReorderable(tabs, true);
        var window = windows.Open(tabs);
        Drag(window, In(Tab(tabs, 2)), In(Tab(tabs, 0), 0.2));
        await Assert.That(Order(tabs)).IsEqualTo("Settings, Account, Profile");
        await Assert.That(Label(tabs.SelectedItem)).IsEqualTo("Settings");
        await Assert.That(((TextBlock)tabs.SelectedContent!).Text).IsEqualTo("Settings");
    }

    [Test]
    public async Task A_press_without_a_drag_only_selects()
    {
        using var windows = new Windows();
        var strip = Strip();
        var window = windows.Open(strip);
        var at = In(Tab(strip, 1));
        Press(window, at);
        DragTo(window, at, at + new Vector(3, 0), steps: 3);
        Release(window, at + new Vector(3, 0));
        await Assert.That(Order(strip)).IsEqualTo("Account, Profile, Settings");
        await Assert.That(strip.SelectedItem).IsEqualTo("Profile");
        await Assert.That(Tab(strip, 1).RenderTransform).IsNull();
    }

    [Test]
    public async Task Tabs_of_a_list_that_cannot_change_do_not_move()
    {
        using var windows = new Windows();
        var strip = Strip(Labels.ToArray());
        var window = windows.Open(strip);
        Drag(window, In(Tab(strip, 0)), In(Tab(strip, 2), 0.9));
        await Assert.That(Order(strip)).IsEqualTo("Account, Profile, Settings");
    }

    // The indicator lies under the tab row, under the labels of the tabs the dragged one
    // passes: the dragged tab draws it itself, as the indicator does, over them.
    [Test]
    [Arguments("pill", false)]
    [Arguments("segmented", false)]
    [Arguments("pill", true)]
    public async Task The_dragged_tab_draws_the_indicator_over_the_other_tabs(string variant, bool tabControl)
    {
        using var windows = new Windows();
        SelectingItemsControl bar = tabControl
            ? new TabControl { HorizontalAlignment = HorizontalAlignment.Left, ItemsSource = new ObservableCollection<string>(Labels), SelectedIndex = 0 }
            : Strip();
        Tabs.SetReorderable(bar, true);
        bar.Classes.Add(variant);
        var window = windows.Open(bar);
        var indicator = Part<Panel>(bar, "PART_Indicator");
        var fill = Part<Border>(bar, "PART_IndicatorFill");
        var dragged = Tab(bar, 0);
        var at = fill.TranslatePoint(default, dragged)!.Value;
        var size = fill.Bounds.Size;
        var start = In(dragged);
        Press(window, start);
        DragTo(window, start, start + new Vector(30, 0));
        var background = Part<Border>(dragged, "PART_Background");
        var drawn = background.TranslatePoint(default, dragged)!.Value;
        await Assert.That(indicator.IsVisible).IsFalse();
        await Assert.That(dragged.Classes.Contains(":dragging")).IsTrue();
        await Assert.That(dragged.ZIndex).IsGreaterThan(Tab(bar, 1).ZIndex);
        await Assert.That(drawn.X).IsEqualTo(at.X).Within(0.01);
        await Assert.That(drawn.Y).IsEqualTo(at.Y).Within(0.01);
        await Assert.That(background.Bounds.Width).IsEqualTo(size.Width).Within(0.01);
        await Assert.That(background.Bounds.Height).IsEqualTo(size.Height).Within(0.01);
        await Assert.That(((ISolidColorBrush)background.Background!).Color).IsEqualTo(((ISolidColorBrush)fill.Background!).Color);
        await Assert.That(background.CornerRadius).IsEqualTo(fill.CornerRadius);
        await Assert.That(background.BoxShadow).IsEqualTo(fill.BoxShadow);
        Release(window, start + new Vector(30, 0));
        await Assert.That(dragged.Classes.Contains(":dragging")).IsFalse();
        await Assert.That(indicator.IsVisible).IsTrue();
    }

    // Outline and underline tabs paint no background, and the labels of the tabs the
    // dragged one passes would show through it: it takes the active tab's, as GPUI's
    // dragged tab preview, until the drop. An underline tab, its label alone, takes it
    // half the gap further, to keep the labels apart, and above its bottom border, to
    // leave the bar's baseline.
    [Test]
    [Arguments("outline", false)]
    [Arguments("underline", true)]
    public async Task A_dragged_tab_without_a_background_takes_the_active_tab_s(string variant, bool underline)
    {
        using var windows = new Windows();
        var strip = Strip();
        strip.Classes.Add(variant);
        var window = windows.Open(strip);
        var dragged = Tab(strip, 0);
        var background = Part<Border>(dragged, "PART_Background");
        var start = In(dragged);
        Press(window, start);
        DragTo(window, start, start + new Vector(30, 0));
        await Assert.That(dragged.ZIndex).IsGreaterThan(Tab(strip, 1).ZIndex);
        await Assert.That(((ISolidColorBrush)background.Background!).Color).IsEqualTo((Color)window.FindResource("UIKit.TabActive.Color")!);
        var reach = underline ? ((StackPanel)strip.ItemsPanelRoot!).Spacing / 2 : 0;
        var border = underline ? ((TemplatedControl)dragged).BorderThickness.Bottom : 0;
        await Assert.That(new Rect(background.TranslatePoint(default, dragged)!.Value, background.Bounds.Size))
            .IsEqualTo(new Rect(dragged.Bounds.Size).Inflate(new Thickness(reach, 0, reach, -border)));
        Release(window, start + new Vector(30, 0));
        await Assert.That(((ISolidColorBrush)background.Background!).Color).IsEqualTo(Colors.Transparent);
    }

    // The underline's indicator is the line its tab's 2px bottom border leaves room for:
    // the dragged tab draws it there, over the tabs it passes.
    [Test]
    public async Task The_dragged_underline_tab_draws_the_indicator_with_its_bottom_border()
    {
        using var windows = new Windows();
        var strip = Strip();
        strip.Classes.Add("underline");
        var window = windows.Open(strip);
        var indicator = Part<Panel>(strip, "PART_Indicator");
        var fill = Part<Border>(strip, "PART_IndicatorFill");
        var dragged = Tab(strip, 0);
        var line = new Rect(fill.TranslatePoint(default, dragged)!.Value, fill.Bounds.Size);
        var start = In(dragged);
        Press(window, start);
        DragTo(window, start, start + new Vector(30, 0));
        var root = Part<Border>(dragged, "PART_LayoutRoot");
        await Assert.That(indicator.IsVisible).IsFalse();
        await Assert.That(((ISolidColorBrush)root.BorderBrush!).Color).IsEqualTo(((ISolidColorBrush)fill.Background!).Color);
        await Assert.That(root.BorderThickness).IsEqualTo(new Thickness(0, 0, 0, line.Height));
        await Assert.That(new Rect(root.TranslatePoint(default, dragged)!.Value, root.Bounds.Size)).IsEqualTo(new Rect(dragged.Bounds.Size));
        await Assert.That(line).IsEqualTo(new Rect(0, dragged.Bounds.Height - line.Height, dragged.Bounds.Width, line.Height));
        Release(window, start + new Vector(30, 0));
        await Assert.That(((ISolidColorBrush)root.BorderBrush!).Color).IsEqualTo(Colors.Transparent);
        await Assert.That(indicator.IsVisible).IsTrue();
    }

    // The tab dropped back in its place lays nothing out; the indicator comes back to it
    // all the same, at once, as to a tab dropped in another place.
    [Test]
    [Arguments(30, "Account, Profile, Settings", 0)]
    [Arguments(140, "Profile, Account, Settings", 1)]
    public async Task The_indicator_comes_back_to_the_dropped_tab_at_once(double distance, string order, int index)
    {
        using var windows = new Windows();
        var strip = Strip();
        strip.Classes.Add("pill");
        var window = windows.Open(strip);
        var indicator = Part<Panel>(strip, "PART_Indicator");
        var start = In(Tab(strip, 0));
        Press(window, start);
        DragTo(window, start, start + new Vector(distance, 0));
        Release(window, start + new Vector(distance, 0));
        await Assert.That(Order(strip)).IsEqualTo(order);
        await Assert.That(indicator.IsVisible).IsTrue();
        await Assert.That(indicator.TranslatePoint(default, strip)!.Value.X)
            .IsEqualTo(Tab(strip, index).TranslatePoint(default, strip)!.Value.X).Within(0.01);
    }

    [Test]
    public async Task An_overflowing_bar_scrolls_while_the_tab_is_held_at_its_end()
    {
        using var windows = new Windows();
        var strip = Strip(new ObservableCollection<string>(Enumerable.Range(1, 10).Select(i => $"Tab {i}")));
        strip.Width = 240;
        var window = windows.Open(strip);
        var scroller = Part<ScrollViewer>(strip, "PART_Scroller");
        var start = In(Tab(strip, 0));
        var end = new Point(strip.TranslatePoint(new Point(236, 0), window)!.Value.X, start.Y);
        Press(window, start);
        DragTo(window, start, end);
        VirtualTime.Advance(TimeSpan.FromMilliseconds(400));
        Flush();
        await Assert.That(scroller.Offset.X).IsGreaterThan(0);
        Release(window, end);
        await Assert.That(strip.Items.Cast<object>().First()).IsNotEqualTo("Tab 1");
    }

    // Two bars of one group in a window, 16px apart: a tab dragged down onto the lower one
    // moves to it, though it is nearer than the distance that takes a tab off into a window.
    [Test]
    [Arguments("files", "files", true)]
    [Arguments("files", "other", false)]
    [Arguments(null, null, false)]
    public async Task A_tab_moves_only_to_a_bar_of_its_group(string? group, string? otherGroup, bool moves)
    {
        using var windows = new Windows();
        var upper = Strip(group: group);
        var lower = Strip(new ObservableCollection<string>(["Inbox"]), otherGroup);
        var window = windows.Open(new StackPanel { Spacing = 16, Children = { upper, lower } });
        var start = In(Tab(upper, 1));
        // Over the lower bar's only tab, short of the upper bar's first tab's middle.
        var over = In(Tab(lower, 0), 0.8);
        Press(window, start);
        DragTo(window, start, over);
        Release(window, over);
        if (moves)
        {
            await Assert.That(Order(upper)).IsEqualTo("Account, Settings");
            await Assert.That(Order(lower)).IsEqualTo("Inbox, Profile");
            await Assert.That(lower.SelectedItem).IsEqualTo("Profile");
            await Assert.That(upper.SelectedItem).IsEqualTo("Settings");
        }
        else
        {
            await Assert.That(Order(upper)).IsEqualTo("Account, Profile, Settings");
            await Assert.That(Order(lower)).IsEqualTo("Inbox");
        }
    }

    // A tab moved to another bar can be dragged back before the drop.
    [Test]
    public async Task A_tab_moved_to_another_bar_can_come_back()
    {
        using var windows = new Windows();
        var upper = Strip(group: "files");
        var lower = Strip(new ObservableCollection<string>(["Inbox"]), "files");
        var window = windows.Open(new StackPanel { Spacing = 40, Children = { upper, lower } });
        var start = In(Tab(upper, 0));
        var over = new Point(start.X, In(Tab(lower, 0)).Y);
        Press(window, start);
        DragTo(window, start, over);
        await Assert.That(Order(lower)).IsEqualTo("Inbox, Account");
        // Back over the upper bar's start: before its first tab.
        var back = new Point(8, start.Y);
        DragTo(window, over, back);
        Release(window, back);
        await Assert.That(Order(upper)).IsEqualTo("Account, Profile, Settings");
        await Assert.That(Order(lower)).IsEqualTo("Inbox");
    }

    // The lower bar is in another window: windows share the screen's points.
    [Test]
    public async Task A_tab_moves_to_a_bar_of_its_group_in_another_window()
    {
        using var windows = new Windows();
        var source = Strip(group: "files");
        var target = Strip(new ObservableCollection<string>(["Inbox"]), "files");
        var window = windows.Open(source);
        windows.Open(new StackPanel { Children = { new Border { Height = 100 }, target } });
        var start = In(Tab(source, 0));
        var over = new Point(start.X, 116);
        Press(window, start);
        DragTo(window, start, over);
        Release(window, over);
        await Assert.That(Order(source)).IsEqualTo("Profile, Settings");
        await Assert.That(Order(target)).IsEqualTo("Inbox, Account");
    }

    [Test]
    public async Task A_tab_dragged_off_moves_into_a_window_that_follows_the_pointer()
    {
        using var windows = new Windows();
        var source = Strip(group: "files");
        List<Window> made = [];
        List<TabStrip> bars = [];
        Tabs.SetDetachedWindowFactory(source, windows.Factory(made, bars));
        var window = windows.Open(source);
        var start = In(Tab(source, 1));
        var off = start + new Vector(0, 120);
        Press(window, start);
        DragTo(window, start, off);
        await Assert.That(made).Count().IsEqualTo(1);
        await Assert.That(made[0].IsVisible).IsTrue();
        await Assert.That(Order(source)).IsEqualTo("Account, Settings");
        await Assert.That(Order(bars[0])).IsEqualTo("Profile");
        var position = made[0].Position;
        DragTo(window, off, off + new Vector(30, 20), steps: 2);
        await Assert.That(made[0].Position - position).IsEqualTo(new PixelPoint(30, 20));
        Release(window, off + new Vector(30, 20));
        await Assert.That(made[0].IsVisible).IsTrue();
        await Assert.That(bars[0].SelectedItem).IsEqualTo("Profile");
    }

    // Dragging a made window's only tab moves the window; held over a bar, the tab joins it
    // and the emptied window closes at the drop.
    [Test]
    public async Task A_made_window_moves_with_its_only_tab_and_closes_when_the_tab_leaves()
    {
        using var windows = new Windows();
        var home = Strip(group: "files");
        List<Window> made = [];
        List<TabStrip> bars = [];
        Tabs.SetDetachedWindowFactory(home, windows.Factory(made, bars));
        var homeWindow = windows.Open(new StackPanel { Children = { new Border { Height = 100 }, home } });
        var start = In(Tab(home, 2));
        Drag(homeWindow, start, new Point(start.X, 20));
        await Assert.That(made).Count().IsEqualTo(1);
        var floating = made[0];
        var bar = bars[0];
        Flush();

        var grab = OnScreen(Tab(bar, 0));
        var away = grab + new PixelPoint(0, 60);
        Press(floating, In(Tab(bar, 0)));
        DragOnScreen(floating, grab, away);
        await Assert.That(made).Count().IsEqualTo(1);
        var position = floating.Position;
        DragOnScreen(floating, away, away + new PixelPoint(10, 0), steps: 1);
        await Assert.That(floating.Position - position).IsEqualTo(new PixelPoint(10, 0));

        var over = OnScreen(Tab(home, 0), 0.2);
        DragOnScreen(floating, away + new PixelPoint(10, 0), over);
        // In the home bar now (the drop puts it where it is dragged to), the window hidden.
        await Assert.That(Order(home)).IsEqualTo("Account, Settings, Profile");
        await Assert.That(floating.IsVisible).IsFalse();
        var closed = false;
        floating.Closed += (_, _) => closed = true;
        Release(floating, floating.PointToClient(over));
        await Assert.That(Order(home)).IsEqualTo("Settings, Account, Profile");
        await Assert.That(closed).IsTrue();
    }

    [Test]
    public async Task A_made_window_closes_with_its_last_tab()
    {
        using var windows = new Windows();
        var home = Strip(group: "files");
        List<Window> made = [];
        List<TabStrip> bars = [];
        Tabs.SetDetachedWindowFactory(home, windows.Factory(made, bars));
        var homeWindow = windows.Open(home);
        var start = In(Tab(home, 0));
        Drag(homeWindow, start, start + new Vector(0, 120));
        var bar = bars[0];
        Tabs.SetClosable(bar, true);
        Flush();
        var closed = false;
        made[0].Closed += (_, _) => closed = true;
        Click(made[0], In(Part<Button>(Tab(bar, 0), "PART_CloseButton")));
        await Assert.That(closed).IsTrue();
        // A window the app opened stays when its bar empties.
        Tabs.SetClosable(home, true);
        Flush();
        Click(homeWindow, In(Part<Button>(Tab(home, 0), "PART_CloseButton")));
        Click(homeWindow, In(Part<Button>(Tab(home, 0), "PART_CloseButton")));
        await Assert.That(home.ItemCount).IsEqualTo(0);
        await Assert.That(homeWindow.IsVisible).IsTrue();
    }

    // Without a factory, or without a group, a tab dragged off its bar stays in it.
    [Test]
    public async Task Without_a_window_factory_a_tab_stays_in_its_bar()
    {
        using var windows = new Windows();
        var strip = Strip(group: "files");
        var window = windows.Open(strip);
        var start = In(Tab(strip, 0));
        var off = In(Tab(strip, 1), 0.9) + new Vector(0, 150);
        Drag(window, start, off);
        await Assert.That(Order(strip)).IsEqualTo("Profile, Account, Settings");
        await Assert.That(window.IsVisible).IsTrue();
    }
}
