using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using AvaloniaUIKit.Demo.ControlCatalog.Inspector;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Tests;

/// <summary>Every demo of the catalog: its XAML, the controls its elements made, and the property grid of its first control.</summary>
public class EveryDemoTests
{
    [Test]
    public async Task The_catalog_has_every_demo_of_the_registry_once()
    {
        var ids = DemoHost.Ids().ToList();
        await Assert.That(ids).IsEquivalentTo(Demos.DemoRegistry.Ids.ToList());
        await Assert.That(ids.Distinct().Count()).IsEqualTo(ids.Count);
    }

    [Test]
    public async Task Every_theme_has_code_colors()
    {
        foreach (var variant in UIKitThemeVariants.All)
        {
            await Assert.That(Catalog.CodePalettes.ContainsKey((string)variant.Key)).IsTrue();
        }
        await Assert.That(Catalog.CodePalettes.ContainsKey("Default Light")).IsTrue();
        await Assert.That(Catalog.CodePalettes.ContainsKey("Default Dark")).IsTrue();
    }

    [Test]
    [MethodDataSource(typeof(DemoHost), nameof(DemoHost.Ids))]
    public async Task The_demo_s_elements_find_their_controls_and_the_grid_edits_its_first(string id)
    {
        using var host = DemoHost.Open(id);
        var session = host.Session;
        await Assert.That(session.Document.Root).IsNotNull();
        await Assert.That(session.Document.Root!.Attribute("x:Class")).IsNotNull();

        var map = session.Map;
        await Assert.That(map.ControlOf(session.Document.Root)).IsSameReferenceAs(session.Root);
        // The root is the demo's own class (ButtonVariants); every other element makes a control of its type.
        // and sits under the control of the element it is written in.
        foreach (var element in map.Elements.Where(e => e != session.Document.Root))
        {
            var control = map.ControlOf(element)!;
            await Assert.That(control.GetType().Name).IsEqualTo(element.LocalName);
            var parent = element.Parent!;
            while (parent.IsPropertyElement)
            {
                parent = parent.Parent!;
            }
            var container = map.ControlOf(parent);
            await Assert.That(container).IsNotNull();
            await Assert.That(container!.IsVisualAncestorOf(control) || control.GetLogicalAncestors().Contains(container)).IsTrue();
        }

        var first = session.DefaultElement();
        await Assert.That(first).IsNotNull();
        var target = map.ControlOf(first!)!;
        StyleClasses.For(target);
        var groups = PropertyCatalog.For(target);
        await Assert.That(groups.Count).IsGreaterThan(0);
        foreach (var item in groups.SelectMany(g => g.Items))
        {
            var editor = PropertyEditor.Create(item.Property.PropertyType);
            editor.Show(target.GetValue(item.Property));
        }
    }

    // Elements of the demos that make objects, not controls: columns, dock models, data items.
    private static readonly HashSet<string> NotControls =
    [
        "TableViewColumn", "DataGridTextColumn", "Factory", "RootDock", "ProportionalDock", "ProportionalDockSplitter",
        "ToolDock", "Tool", "DocumentDock", "Document", "SpringSlide", "NumberMask", "DescriptionItem",
        "DescriptionSeparator", "DateRange", "DateRangePreset", "ListSection", "SelectGroup", "TreeItem",
    ];

    [Test]
    public async Task Elements_that_make_controls_find_them()
    {
        var unmapped = new List<string>();
        var total = 0;
        foreach (var id in DemoHost.Ids())
        {
            using var host = DemoHost.Open(id);
            var session = host.Session;
            foreach (var element in Descendants(session.Document.Root!).Where(e => !NotControls.Contains(e.LocalName)))
            {
                total++;
                if (session.Map.ControlOf(element) is null)
                {
                    unmapped.Add($"{id}: {element.Name} #{element.Index}");
                }
            }
        }
        Console.WriteLine($"{total - unmapped.Count} of {total} elements found their controls; not found:");
        foreach (var line in unmapped)
        {
            Console.WriteLine("  " + line);
        }
        // Left: content shown only in a closed popup or after an image fails to load.
        await Assert.That((double)unmapped.Count / total).IsLessThan(0.01);
    }

    // The elements below the root, not those below an object that is no control.
    private static IEnumerable<XamlElement> Descendants(XamlElement element)
    {
        foreach (var child in DemoMap.ObjectChildren(element))
        {
            yield return child;
            if (NotControls.Contains(child.LocalName))
            {
                continue;
            }
            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }
}
