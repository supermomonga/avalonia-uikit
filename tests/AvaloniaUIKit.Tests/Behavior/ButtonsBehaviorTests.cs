using System.Windows.Input;
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
/// What the button and group controls of ADR 30 do: uikit:Buttons (loading,
/// pointer focus), ButtonGroup, ToggleGroup, Accordion and Toolbar, each with
/// the GPUI Kit rule it ports.
/// </summary>
public class ButtonsBehaviorTests
{
    private static GoldenCase Case(string id) => GoldenManifest.Get(id);

    /// <summary>A host the size of an input-group case, with <paramref name="content"/> at its anchor.</summary>
    private static CaseHost Host(Control content) =>
        CaseHost.Open(Case("input-group-loading/keeps-focus.base/focus+click/light"), content);

    private static void ClickAt(CaseHost host, Visual target)
    {
        var center = target.TranslatePoint(new Point(target.Bounds.Width / 2, target.Bounds.Height / 2), host.Window)!.Value;
        host.Window.MouseMove(center);
        host.Window.MouseDown(center, MouseButton.Left);
        host.Window.MouseUp(center, MouseButton.Left);
        host.Flush();
    }

    private static void Key(CaseHost host, PhysicalKey key)
    {
        host.Window.KeyPressQwerty(key, RawInputModifiers.None);
        host.Window.KeyReleaseQwerty(key, RawInputModifiers.None);
        host.Flush();
    }

    private sealed class CountingCommand : ICommand
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

    // button.rs: a loading button "is not waiting for another click": the pointer,
    // Enter and Space do nothing, and it keeps its own look (not :disabled).
    [Test]
    public async Task A_loading_button_ignores_the_pointer_and_keys()
    {
        var command = new CountingCommand();
        var button = new Button { Content = "Save", Command = command };
        Buttons.SetIsLoading(button, true);
        using var host = Host(button);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        ClickAt(host, button);
        await Assert.That(button.IsPressed).IsFalse();
        host.Drive(Case("input-group-loading/keeps-focus.base/focus+click/light"), "focus");
        await Assert.That(button.IsFocused).IsTrue();
        Key(host, PhysicalKey.Enter);
        Key(host, PhysicalKey.Space);
        await Assert.That(clicks).IsEqualTo(0);
        await Assert.That(command.Count).IsEqualTo(0);
        await Assert.That(button.IsEffectivelyEnabled).IsTrue();

        Buttons.SetIsLoading(button, false);
        ClickAt(host, button);
        Key(host, PhysicalKey.Enter);
        await Assert.That(clicks).IsEqualTo(2);
        await Assert.That(command.Count).IsEqualTo(2);
    }

    // dropdown_button.rs: the action half is the app's Button, loading on its own;
    // the menu half stays live.
    [Test]
    public async Task A_loading_split_button_ignores_its_action_but_opens_its_menu()
    {
        var golden = Case("split-loading/open.default/click-at-291-24/light");
        var command = new CountingCommand();
        var split = (SplitButton)Adapters.Create(golden);
        split.Command = command;
        using var host = CaseHost.Open(golden, split);
        var clicks = 0;
        split.Click += (_, _) => clicks++;
        host.Drive(golden, "click-at-230-24");
        host.Drive(golden, "focus+key-enter+key-space");
        await Assert.That((clicks, command.Count)).IsEqualTo((0, 0));
        await Assert.That(split.Flyout!.IsOpen).IsFalse();
        host.Drive(golden, "click-at-291-24");
        await Assert.That(split.Flyout!.IsOpen).IsTrue();
        split.Flyout.Hide();
        Buttons.SetIsLoading(split, false);
        host.Drive(golden, "click-at-230-24");
        await Assert.That((clicks, command.Count)).IsEqualTo((1, 1));
    }

    // IsDefault (and access keys, IsCancel) reach Button.OnClick without a key on
    // the button: the loading button raises no Click and runs no Command.
    [Test]
    public async Task A_loading_default_button_ignores_enter_in_the_window()
    {
        var command = new CountingCommand();
        var box = new TextBox { Width = 120 };
        var button = new Button { Content = "Save", Command = command, IsDefault = true };
        Buttons.SetIsLoading(button, true);
        using var host = Host(new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { box, button } });
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        box.Focus();
        host.Flush();
        Key(host, PhysicalKey.Enter);
        await Assert.That(clicks).IsEqualTo(0);
        await Assert.That(command.Count).IsEqualTo(0);
        Buttons.SetIsLoading(button, false);
        Key(host, PhysicalKey.Enter);
        await Assert.That(clicks).IsEqualTo(1);
        await Assert.That(command.Count).IsEqualTo(1);
    }

    // button.rs: "Avoid focus on mouse down." With TakesFocusOnPointer False the
    // TextBox keeps its focus and selection, and the button still clicks.
    [Test]
    public async Task A_button_that_does_not_take_focus_leaves_it_in_the_text_box()
    {
        var box = new TextBox { Width = 120, Text = "gpui" };
        var button = new Button { Content = "Copy" };
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { box, button } };
        Buttons.SetTakesFocusOnPointer(panel, false);
        using var host = Host(panel);
        var clicks = 0;
        button.Click += (_, _) => clicks++;
        box.Focus();
        box.SelectAll();
        host.Flush();
        ClickAt(host, button);
        await Assert.That(clicks).IsEqualTo(1);
        await Assert.That(box.IsFocused).IsTrue();
        await Assert.That(box.SelectedText).IsEqualTo("gpui");

        // Avalonia's default moves the focus to the pressed button.
        Buttons.SetTakesFocusOnPointer(panel, true);
        ClickAt(host, button);
        await Assert.That(button.IsFocused).IsTrue();
    }

    // The button stays a Tab stop and shows its ring on keyboard focus.
    [Test]
    public async Task A_button_that_does_not_take_focus_is_still_a_tab_stop()
    {
        var golden = Case("input-group-loading/keeps-focus.base/focus+click/light");
        var box = new TextBox { Width = 120 };
        var button = new Button { Content = "Copy" };
        Buttons.SetTakesFocusOnPointer(button, false);
        using var host = CaseHost.Open(golden, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { box, button } });
        host.Drive(golden, "focus+focus");
        await Assert.That(button.IsFocused).IsTrue();
        await Assert.That(button.Classes.Contains(":focus-visible")).IsTrue();
        await Assert.That(button.GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_FocusRing").IsVisible).IsTrue();
    }

    // The input group button in the golden case: pressing it leaves the ring on the input.
    [Test]
    public async Task An_input_group_button_leaves_the_focus_in_the_input()
    {
        var golden = Case("input-group-loading/keeps-focus.base/focus+click/light");
        using var host = CaseHost.Open(golden, Adapters.Create(golden));
        var box = (TextBox)host.Control;
        host.Drive(golden, "focus+click");
        await Assert.That(box.IsFocused).IsTrue();
    }

    // button_group.rs on_click: single selection reports the clicked button
    // alone; multiple toggles it in the selection, appending it.
    [Test]
    [Arguments(false, "0", "0")]
    [Arguments(true, "1,0", "1")]
    public async Task A_button_group_reports_the_selection_a_click_makes(bool multiple, string first, string second)
    {
        var golden = Case("uikit-buttongroup/click.base/click/light");
        var group = (ButtonGroup)Adapters.Create(golden);
        group.Multiple = multiple;
        using var host = CaseHost.Open(golden, group);
        var reports = new List<string>();
        group.Click += (_, e) => reports.Add(string.Join(",", e.SelectedIndices));
        host.Drive(golden, "click");
        host.Drive(golden, "click");
        await Assert.That(string.Join(" ", reports)).IsEqualTo($"{first} {second}");
        await Assert.That(string.Join(",", group.SelectedIndices)).IsEqualTo(second);
    }

    // The buttons keep their own click; a disabled group reports nothing.
    [Test]
    public async Task A_disabled_button_group_reports_no_click()
    {
        var golden = Case("uikit-buttongroup/click.base/click/light");
        var group = (ButtonGroup)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, group);
        var reports = 0;
        var buttonClicks = 0;
        group.Click += (_, _) => reports++;
        ((Button)group.Children[0]).Click += (_, _) => buttonClicks++;
        host.Drive(golden, "click");
        await Assert.That((reports, buttonClicks)).IsEqualTo((1, 1));
        group.IsEnabled = false;
        host.Drive(golden, "click");
        await Assert.That((reports, buttonClicks)).IsEqualTo((1, 1));
    }

    // button_group.rs: the group's size, variant, compact and outline go on each
    // button, and only those: a button's own classes stay.
    [Test]
    public async Task A_button_group_passes_its_classes_to_its_buttons()
    {
        var group = new ButtonGroup { Classes = { "outline", "small", "wide" } };
        var own = new Button { Classes = { "rounded-large" } };
        group.Children.Add(own);
        await Assert.That(string.Join(" ", own.Classes.Where(c => !c.StartsWith(':')).Order())).IsEqualTo("outline rounded-large small");
        group.Classes.Remove("small");
        group.Classes.Add("primary");
        await Assert.That(string.Join(" ", own.Classes.Where(c => !c.StartsWith(':')).Order())).IsEqualTo("outline primary rounded-large");
        group.Children.Remove(own);
        await Assert.That(string.Join(" ", own.Classes.Where(c => !c.StartsWith(':')).Order())).IsEqualTo("rounded-large");
    }

    // base/toggle.rs: every toggle is its own Tab stop and the arrow keys do
    // nothing; Space and Enter flip the focused toggle.
    [Test]
    public async Task A_toggle_group_tabs_between_toggles_and_ignores_arrow_keys()
    {
        var golden = Case("uikit-togglegroup/keys.ghost/focus/light");
        var group = (ToggleGroup)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, group);
        var toggles = group.Children.OfType<Avalonia.Controls.Primitives.ToggleButton>().ToArray();
        host.Drive(golden, "focus+key-right+key-left+key-down");
        await Assert.That(toggles[0].IsFocused).IsTrue();
        await Assert.That(string.Join(",", group.Checked)).IsEqualTo("False,True,False");
        host.Drive(golden, "focus");
        await Assert.That(toggles[1].IsFocused).IsTrue();
        // GPUI's group reports pointer clicks only (its "legacy" keyboard rule); here
        // the toggle flips on Space and the group reports it like a click.
        var reports = new List<string>();
        group.Click += (_, e) => reports.Add(string.Join(",", e.Checked));
        host.Drive(golden, "key-space");
        await Assert.That(string.Join(" ", reports)).IsEqualTo("False,False,False");
    }

    // toggle.rs on_click: the states after the clicked toggle flips.
    [Test]
    public async Task A_toggle_group_reports_the_states_a_click_leaves()
    {
        var golden = Case("uikit-togglegroup/unchecked.ghost/hover/light");
        var group = (ToggleGroup)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, group);
        var reports = new List<string>();
        group.Click += (_, e) => reports.Add(string.Join(",", e.Checked));
        host.Drive(golden, "click");
        host.Drive(golden, "click");
        await Assert.That(string.Join(" ", reports)).IsEqualTo("True,True,False False,True,False");
        group.IsEnabled = false;
        host.Drive(golden, "click");
        await Assert.That(reports.Count).IsEqualTo(2);
    }

    // toggle.rs: the group's size and variant go on every toggle.
    [Test]
    public async Task A_toggle_group_passes_its_classes_to_its_toggles()
    {
        var group = new ToggleGroup { Classes = { "outline", "large", "segmented" } };
        var toggle = new Avalonia.Controls.Primitives.ToggleButton();
        group.Children.Add(toggle);
        await Assert.That(string.Join(" ", toggle.Classes.Where(c => !c.StartsWith(':')).Order())).IsEqualTo("large outline");
    }

    // accordion.rs: without `multiple`, opening an item clears the open set;
    // on_toggle_click reports the open items after the click.
    [Test]
    public async Task An_accordion_keeps_one_item_open_and_reports_the_open_items()
    {
        var golden = Case("uikit-accordion/single.medium/normal/light");
        var accordion = (Accordion)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, accordion);
        var items = accordion.Items.OfType<Expander>().ToArray();
        var reports = new List<string>();
        accordion.ToggleClick += (_, e) => reports.Add(string.Join(",", e.OpenIndices));
        host.Drive(golden, "click-at-100-98+wait-600ms");
        await Assert.That(items.Select(i => i.IsExpanded)).IsEquivalentTo(new[] { false, true, false });
        // The third title, below the open second item.
        ClickAt(host, items[2].GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().First());
        await Assert.That(items.Select(i => i.IsExpanded)).IsEquivalentTo(new[] { false, false, true });
        // A programmatic open closes the others too.
        items[0].IsExpanded = true;
        await Assert.That(items.Select(i => i.IsExpanded)).IsEquivalentTo(new[] { true, false, false });
        await Assert.That(string.Join(" ", reports)).IsEqualTo("1 2");
    }

    [Test]
    public async Task A_multiple_accordion_opens_items_independently()
    {
        var golden = Case("uikit-accordion/card.medium.open_first/normal/light");
        var accordion = (Accordion)Adapters.Create(golden);
        using var host = CaseHost.Open(golden, accordion);
        await Assert.That(accordion.Multiple).IsTrue();
        var reports = new List<string>();
        accordion.ToggleClick += (_, e) => reports.Add(string.Join(",", e.OpenIndices));
        host.Drive(golden, "click-at-100-98+wait-600ms");
        await Assert.That(string.Join(",", accordion.OpenIndices)).IsEqualTo("0,1");
        await Assert.That(string.Join(" ", reports)).IsEqualTo("0,1");
    }

    // accordion.rs: a disabled accordion installs no toggle; its size goes on every item.
    [Test]
    public async Task A_disabled_accordion_ignores_clicks_and_passes_its_size()
    {
        var golden = Case("uikit-accordion/disabled.all/normal/light");
        var accordion = (Accordion)Adapters.Create(golden);
        accordion.Classes.Add("small");
        using var host = CaseHost.Open(golden, accordion);
        var reports = 0;
        accordion.ToggleClick += (_, _) => reports++;
        host.Drive(golden, "click-at-100-98+wait-600ms");
        await Assert.That(reports).IsEqualTo(0);
        await Assert.That(string.Join(",", accordion.OpenIndices)).IsEqualTo("0");
        await Assert.That(accordion.Items.OfType<Expander>().All(i => i.Classes.Contains("small"))).IsTrue();
    }

    // base/toolbar.rs: Left and Right move the focus along the bar and wrap; a
    // hosted text input keeps the arrow keys for its caret.
    [Test]
    public async Task A_toolbar_moves_the_focus_with_the_arrow_keys_and_wraps()
    {
        var golden = Case("uikit-toolbar/wrap.base/focus+key-left/light");
        var bar = (Toolbar)Adapters.Create(golden);
        var input = new TextBox { Width = 60, Text = "ab" };
        bar.Children.Add(input);
        using var host = CaseHost.Open(golden, bar);
        var buttons = bar.Children.OfType<Button>().ToArray();
        host.Drive(golden, "focus");
        await Assert.That(buttons[0].IsFocused).IsTrue();
        host.Drive(golden, "key-right");
        await Assert.That(buttons[1].IsFocused).IsTrue();
        await Assert.That(buttons[1].Classes.Contains(":focus-visible")).IsTrue();
        host.Drive(golden, "key-left+key-left");
        await Assert.That(input.IsFocused).IsTrue();
        input.CaretIndex = 1;
        host.Drive(golden, "key-left");
        await Assert.That((input.IsFocused, input.CaretIndex)).IsEqualTo((true, 0));
        host.Drive(golden, "key-tab");
        await Assert.That(input.IsFocused).IsFalse();
    }

    // toolbar.rs: the size goes on every control (ToolbarItem::sized), buttons turn
    // ghost and compact (prepare_for_toolbar), a group passes its size on, and
    // content keeps its own; Large falls back to Medium (no class).
    [Test]
    public async Task A_toolbar_passes_its_size_to_its_controls()
    {
        static string Own(Control c) => string.Join(" ", c.Classes.Where(n => !n.StartsWith(':')).Order());
        var button = new Button();
        var toggle = new Avalonia.Controls.Primitives.ToggleButton();
        var content = new Button { Classes = { "primary" } };
        Toolbar.SetTakesSize(content, false);
        var separator = new Separator { Classes = { "vertical" } };
        var grouped = new Button();
        var group = new ToolbarGroup { Children = { grouped } };
        var bar = new Toolbar { Classes = { "xsmall" }, Children = { button, toggle, content, separator, group, new ToolbarSpacer() } };
        await Assert.That(Own(button)).IsEqualTo("compact ghost xsmall");
        await Assert.That(Own(toggle)).IsEqualTo("xsmall");
        await Assert.That(Own(content)).IsEqualTo("primary");
        await Assert.That(Own(separator)).IsEqualTo("vertical");
        await Assert.That(Own(group)).IsEqualTo("xsmall");
        await Assert.That(Own(grouped)).IsEqualTo("compact ghost xsmall");
        bar.Classes.Remove("xsmall");
        bar.Classes.Add("large");
        await Assert.That(Own(button)).IsEqualTo("compact ghost");
        await Assert.That(Own(grouped)).IsEqualTo("compact ghost");
        Toolbar.SetTakesSize(content, true);
        await Assert.That(Own(content)).IsEqualTo("compact ghost primary");
    }

    // A ToolbarSpacer takes the width the other items leave; several share it.
    [Test]
    public async Task Toolbar_spacers_share_the_width_left()
    {
        var golden = Case("uikit-toolbar/spacer.small/normal/light");
        var first = new Border { Width = 20, Height = 10 };
        var last = new Border { Width = 20, Height = 10 };
        var spacers = new[] { new ToolbarSpacer(), new ToolbarSpacer() };
        var bar = new Toolbar { Width = 200, Children = { first, spacers[0], new Border { Width = 40, Height = 10 }, spacers[1], last } };
        using var host = CaseHost.Open(golden, bar);
        host.Flush();
        // 192 inside, 80 taken, 4 gaps of 4: 96 left, 48 each.
        await Assert.That(spacers.Select(s => s.Bounds.Width)).IsEquivalentTo(new[] { 48.0, 48.0 });
        await Assert.That(last.Bounds.Right).IsEqualTo(196.0);
    }
}

