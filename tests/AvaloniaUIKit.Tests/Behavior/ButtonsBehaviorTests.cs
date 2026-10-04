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
}
