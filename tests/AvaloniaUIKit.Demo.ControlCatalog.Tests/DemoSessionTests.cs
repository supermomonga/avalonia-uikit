using Avalonia.Controls;
using AvaloniaUIKit.Demo.ControlCatalog.Inspector;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Tests;

/// <summary>The property grid's changes, made on the live demo and in its XAML.</summary>
public class DemoSessionTests
{
    private static (XamlElement Element, Control Control) Find(DemoSession session, string name, string? content = null)
    {
        var element = session.Map.Elements.First(e => e.LocalName == name && (content is null || e.Attribute("Content")?.Value == content));
        return (element, session.Map.ControlOf(element)!);
    }

    private static PropertyItem Item(Control control, string attribute) =>
        PropertyCatalog.For(control).SelectMany(g => g.Items).First(i => i.AttributeName == attribute);

    [Test]
    public async Task A_written_property_changes_on_the_control_and_in_its_attribute()
    {
        using var host = DemoHost.Open("button/variants");
        var session = host.Session;
        var (element, button) = Find(session, "Button", "Primary");
        var content = Item(button, "Content");

        await Assert.That(session.Set(element, button, content, "Save")).IsTrue();
        await Assert.That(((Button)button).Content).IsEqualTo("Save");
        await Assert.That(session.Xaml).Contains("""<Button Classes="primary" Content="Save" />""");
        await Assert.That(session.IsEdited(button, content)).IsTrue();

        session.Revert(element, button, content);
        await Assert.That(((Button)button).Content).IsEqualTo("Primary");
        await Assert.That(session.Edits.IsEmpty).IsTrue();
    }

    [Test]
    public async Task A_property_the_XAML_leaves_out_is_added_and_removed_again()
    {
        using var host = DemoHost.Open("button/variants");
        var session = host.Session;
        var (element, button) = Find(session, "Button", "Danger");
        var enabled = Item(button, "IsEnabled");

        session.Set(element, button, enabled, false);
        await Assert.That(button.IsEnabled).IsFalse();
        await Assert.That(session.Xaml).Contains("""<Button Classes="danger" Content="Danger" IsEnabled="False" />""");

        session.Revert(element, button, enabled);
        await Assert.That(button.IsEnabled).IsTrue();
        await Assert.That(session.Xaml).Contains("""<Button Classes="danger" Content="Danger" />""");
    }

    [Test]
    public async Task Attached_properties_are_written_with_their_owner_and_prefix()
    {
        using var host = DemoHost.Open("button/variants");
        var session = host.Session;
        var (element, button) = Find(session, "Button", "Primary");
        var loading = Item(button, "uikit:Buttons.IsLoading");

        session.Set(element, button, loading, true);
        await Assert.That(Buttons.GetIsLoading(button)).IsTrue();
        await Assert.That(session.Xaml).Contains("""<Button Classes="primary" Content="Primary" uikit:Buttons.IsLoading="True" />""");
    }

    [Test]
    public async Task Classes_change_on_the_control_and_in_the_XAML()
    {
        using var host = DemoHost.Open("button/variants");
        var session = host.Session;
        var (element, button) = Find(session, "Button", "Primary");

        session.SetClasses(element, button, ["danger", "small"]);
        await Assert.That(DemoSession.ClassesOf(button)).IsEquivalentTo(["danger", "small"]);
        await Assert.That(session.Xaml).Contains("""<Button Classes="danger small" Content="Primary" />""");

        session.SetClasses(element, button, ["primary"]);
        await Assert.That(session.AreClassesEdited(button)).IsFalse();
        await Assert.That(session.Edits.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Reset_makes_a_new_demo_without_the_changes()
    {
        using var host = DemoHost.Open("button/variants");
        var session = host.Session;
        var (element, button) = Find(session, "Button", "Primary");
        session.Set(element, button, Item(button, "Content"), "Save");
        var before = session.Root;

        session.Reset();
        await Assert.That(session.Root).IsNotSameReferenceAs(before);
        await Assert.That(session.Edits.IsEmpty).IsTrue();
        var (_, fresh) = Find(session, "Button", "Primary");
        await Assert.That(((Button)fresh).Content).IsEqualTo("Primary");
    }

    [Test]
    public async Task The_grid_starts_with_the_component_s_control()
    {
        using var group = DemoHost.Open("button-group/demo");
        var element = group.Session.DefaultElement()!;
        await Assert.That(element.LocalName).IsEqualTo("StackPanel");
        await Assert.That(element.Attribute("Classes")!.Value).Contains("button-group");

        using var badge = DemoHost.Open("badge/count");
        await Assert.That(badge.Session.DefaultElement()!.Name).IsEqualTo("uikit:Badge");
    }

    [Test]
    public async Task The_themes_give_the_classes_of_a_control_s_look()
    {
        using var host = DemoHost.Open("button/variants");
        var (_, button) = Find(host.Session, "Button", "Primary");
        var classes = StyleClasses.For(button);
        await Assert.That(classes).Contains("primary");
        await Assert.That(classes).Contains("small");
        await Assert.That(classes).Contains("outline");
        await Assert.That(classes).Contains("rounded-full");
        await Assert.That(StyleClasses.ExcludedBy("small")).Contains("large");
        await Assert.That(StyleClasses.ExcludedBy("primary")).Contains("danger");

        using var group = DemoHost.Open("button-group/demo");
        var panel = group.Session.Map.ControlOf(group.Session.DefaultElement()!)!;
        await Assert.That(StyleClasses.For(panel)).Contains("button-group");

        // The catalog's own styles are not the themes'.
        await Assert.That(StyleClasses.For(new TextBlock())).DoesNotContain("card-title");
    }

    [Test]
    public async Task The_grid_groups_a_control_s_properties_by_the_type_that_declares_them()
    {
        using var host = DemoHost.Open("button/variants");
        var (_, button) = Find(host.Session, "Button", "Primary");
        var groups = PropertyCatalog.For(button);
        var titles = groups.Select(g => g.Title).ToList();
        await Assert.That(titles[0]).IsEqualTo("Button");
        await Assert.That(titles).Contains("ContentControl");
        await Assert.That(titles).Contains("Attached");
        await Assert.That(titles).Contains("Layout");
        var layout = groups.First(g => g.Title == "Layout").Items.Select(i => i.AttributeName).ToList();
        await Assert.That(layout).Contains("Width");
        await Assert.That(layout).Contains("Margin");
        await Assert.That(groups.SelectMany(g => g.Items).Count(i => i.AttributeName == "Content")).IsEqualTo(1);
        // Button adds HotKeyManager.HotKey as its own HotKey.
        await Assert.That(groups.First(g => g.Title == "Button").Items.Select(i => i.AttributeName)).Contains("HotKey");

        using var sidebar = DemoHost.Open("uikit-sidebar/demo");
        var bar = sidebar.Session.Map.ControlOf(sidebar.Session.DefaultElement()!)!;
        var own = PropertyCatalog.For(bar).First(g => g.Title == "Sidebar").Items.Select(i => i.AttributeName).ToList();
        await Assert.That(own).Contains("uikit:Sidebar.IsIconCollapsed");
        await Assert.That(own).Contains("ExpandedWidth");
    }
}
