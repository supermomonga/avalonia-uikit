using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace AvaloniaUIKit.Demo.ControlCatalog.Xaml;

/// <summary>
/// Which control of a live demo each element of its XAML made, found by walking the
/// XAML and the demo's logical tree side by side: an element's controls are among the
/// logical children of its parent's control, in order, of the element's type, and agree
/// with what the element says about them (content, header, name, classes). Elements
/// that make no control in the logical tree (templates, resources, flyouts, tooltips)
/// have none.
/// </summary>
public sealed class DemoMap
{
    // Properties whose XAML value tells controls of one type apart; a mismatch rules a control out.
    private static readonly HashSet<string> Identity = ["Content", "Header", "Label", "Title", "Name", "Kind", "Tag", "Gesture"];

    // Properties that usually tell controls apart, but that controls may rewrite (a mask, a filter).
    private static readonly HashSet<string> Hints = ["Text", "Watermark", "PlaceholderText", "Value", "Icon"];

    // Property elements whose content is not in the element's logical tree.
    private static readonly HashSet<string> Detached =
    [
        "Resources", "Styles", "DataTemplates", "KeyBindings", "Transitions", "Flyout", "ContextFlyout", "ContextMenu",
        "Tip", "Clip", "RenderTransform", "OpacityMask", "Effect", "ColumnDefinitions", "RowDefinitions",
        "GestureRecognizers", "Animations", "Card", "DataContext", "ItemsPanel", "Inlines",
    ];

    // Elements that describe controls rather than being them, and text inlines.
    private static readonly HashSet<string> Descriptions =
    [
        "DataTemplate", "ControlTemplate", "TreeDataTemplate", "ItemsPanelTemplate", "Style", "Styles", "ControlTheme",
        "ResourceDictionary", "StyleInclude", "Setter", "MenuFlyout", "Flyout",
        "Run", "Span", "Bold", "Italic", "Underline", "LineBreak", "InlineUIContainer",
    ];

    private readonly Dictionary<XamlElement, Control> _controls = [];
    private readonly Dictionary<Control, XamlElement> _elements = [];

    private DemoMap()
    {
    }

    /// <summary>The elements that have a control, in document order.</summary>
    public IEnumerable<XamlElement> Elements => _controls.Keys.OrderBy(e => e.Index);

    /// <summary>Maps <paramref name="document"/>'s elements to the controls of <paramref name="root"/>, the control its root made.</summary>
    public static DemoMap Build(XamlDocument document, Control root)
    {
        var map = new DemoMap();
        if (document.Root is { } element)
        {
            map.Map(element, root);
        }
        return map;
    }

    /// <summary>The control <paramref name="element"/> made, or null.</summary>
    public Control? ControlOf(XamlElement element) => _controls.GetValueOrDefault(element);

    /// <summary>The element of <paramref name="visual"/> or of its nearest visual ancestor that has one.</summary>
    public XamlElement? ElementAt(Visual? visual)
    {
        for (var v = visual; v is not null; v = v.GetVisualParent())
        {
            if (v is Control control && _elements.TryGetValue(control, out var element))
            {
                return element;
            }
        }
        return null;
    }

    /// <summary>The elements whose controls a XAML element's children make, in order.</summary>
    public static IEnumerable<XamlElement> ObjectChildren(XamlElement element)
    {
        foreach (var child in element.Children)
        {
            if (child.IsPropertyElement)
            {
                var property = child.LocalName[(child.LocalName.LastIndexOf('.') + 1)..];
                if (Detached.Contains(property) || property.EndsWith("Template", StringComparison.Ordinal)
                    || property.EndsWith("Theme", StringComparison.Ordinal) || property.EndsWith("Brush", StringComparison.Ordinal)
                    || property is "Background" or "Foreground" or "Fill" or "Stroke")
                {
                    continue;
                }
                foreach (var value in child.Children)
                {
                    if (IsObject(value))
                    {
                        yield return value;
                    }
                }
            }
            else if (IsObject(child))
            {
                yield return child;
            }
        }
    }

    private static bool IsObject(XamlElement element) =>
        !element.IsPropertyElement && element.Prefix != "x" && !Descriptions.Contains(element.LocalName);

    private void Map(XamlElement element, Control control)
    {
        _controls[element] = control;
        _elements[control] = element;
        var children = ObjectChildren(element).ToList();
        if (children.Count == 0)
        {
            return;
        }
        var live = control.GetLogicalChildren().OfType<Control>().ToList();
        // Per type, controls before the last one matched are not candidates: XAML order is logical order.
        var cursors = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var child in children)
        {
            var match = Pick(child, live, cursors) ?? PickBelow(child, control) ?? PickPresented(child, control);
            if (match is not null)
            {
                Map(child, match);
            }
        }
    }

    private Control? Pick(XamlElement element, List<Control> live, Dictionary<string, int> cursors)
    {
        var from = cursors.GetValueOrDefault(element.LocalName);
        for (var i = from; i < live.Count; i++)
        {
            var candidate = live[i];
            if (candidate.GetType().Name == element.LocalName && !_elements.ContainsKey(candidate) && Score(element, candidate) >= 0)
            {
                cursors[element.LocalName] = i + 1;
                return candidate;
            }
        }
        return null;
    }

    // A control that is not a logical child of its parent's control (a control moves its
    // content elsewhere): among the logical descendants, one the element names, or the
    // only one of its type.
    private Control? PickBelow(XamlElement element, Control control)
    {
        var candidates = control.GetLogicalDescendants().OfType<Control>()
            .Where(c => c.GetType().Name == element.LocalName && !_elements.ContainsKey(c))
            .Select(c => (Control: c, Score: Score(element, c)))
            .Where(c => c.Score >= 0)
            .ToList();
        return candidates.FirstOrDefault(c => c.Score > 0).Control ?? (candidates.Count == 1 ? candidates[0].Control : null);
    }

    // Content a control's template presents (an EmptyState's Media, a TextBox's
    // InnerLeftContent, a Sidebar's Header) is in the visual tree only: a control there
    // that no template made and that no element of the XAML holds.
    private Control? PickPresented(XamlElement element, Control control)
    {
        var candidates = control.GetVisualDescendants().OfType<Control>()
            .Where(c => c.GetType().Name == element.LocalName && c.TemplatedParent is null && !_elements.ContainsKey(c)
                && c.Parent is null or ContentPresenter or Control { TemplatedParent: not null })
            .Select(c => (Control: c, Score: Score(element, c)))
            .Where(c => c.Score >= 0)
            .ToList();
        return candidates.FirstOrDefault(c => c.Score > 0).Control ?? candidates.FirstOrDefault().Control;
    }

    /// <summary>
    /// How well <paramref name="control"/> agrees with <paramref name="element"/>'s attributes:
    /// -1 when it contradicts one that tells controls apart, else how many agree.
    /// </summary>
    public static int Score(XamlElement element, Control control)
    {
        var score = 0;
        foreach (var attribute in element.Attributes)
        {
            if (attribute.IsMarkupExtension)
            {
                continue;
            }
            var name = attribute.Name == "x:Name" ? "Name" : attribute.Name;
            if (name == "Classes")
            {
                foreach (var c in attribute.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!control.Classes.Contains(c))
                    {
                        return -1;
                    }
                }
                score++;
                continue;
            }
            var identity = Identity.Contains(name);
            if (!identity && !Hints.Contains(name))
            {
                continue;
            }
            if (AvaloniaPropertyRegistry.Instance.FindRegistered(control, name) is not { } property)
            {
                continue;
            }
            if (XamlValues.Matches(control.GetValue(property), attribute.Value))
            {
                score++;
            }
            else if (identity)
            {
                return -1;
            }
        }
        return score;
    }
}
