using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;
using AvaloniaUIKit.Demos;

namespace AvaloniaUIKit.Demo.ControlCatalog.Inspector;

/// <summary>
/// A live demo and its XAML: the control the demo's class makes, which element of the
/// XAML made which control (<see cref="DemoMap"/>), and the property grid's changes,
/// made both on the controls and in the XAML the catalog shows (<see cref="XamlEdits"/>).
/// </summary>
public sealed class DemoSession
{
    private readonly Dictionary<(Control Control, AvaloniaProperty Property), (bool WasSet, object? Value)> _originals = [];
    private readonly Dictionary<Control, string[]> _originalClasses = [];
    private DemoMap? _map;

    public DemoSession(CatalogComponent component, CatalogDemo demo)
    {
        Component = component;
        Demo = demo;
        Document = XamlDocument.Parse(Catalog.Xaml(demo));
        CodeBehind = Catalog.CodeBehind(demo);
        Root = Create();
        Edits.Changed += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The component the demo belongs to.</summary>
    public CatalogComponent Component { get; }

    /// <summary>The demo.</summary>
    public CatalogDemo Demo { get; }

    /// <summary>The demo's XAML as written.</summary>
    public XamlDocument Document { get; }

    /// <summary>The demo's code-behind, or null.</summary>
    public string? CodeBehind { get; }

    /// <summary>The changes to the XAML.</summary>
    public XamlEdits Edits { get; } = new();

    /// <summary>The live demo.</summary>
    public Control Root { get; private set; }

    /// <summary>Which element made which control, found when first asked for.</summary>
    public DemoMap Map => _map ??= DemoMap.Build(Document, Root);

    /// <summary>Raised when the XAML changes.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised when <see cref="Root"/> is replaced by a new demo.</summary>
    public event EventHandler? Recreated;

    /// <summary>The XAML with the changes made.</summary>
    public string Xaml => Edits.Apply(Document);

    /// <summary>Finds the controls again, after the demo has added or moved some.</summary>
    public void Remap() => _map = null;

    /// <summary>Drops every change: a new demo replaces the live one.</summary>
    public void Reset()
    {
        _originals.Clear();
        _originalClasses.Clear();
        Root = Create();
        _map = null;
        Edits.Clear();
        Recreated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// The element the property grid starts with: the first that makes one of the
    /// component's controls, else the first control that is not only a layout panel.
    /// </summary>
    public XamlElement? DefaultElement()
    {
        var map = Map;
        var mapped = map.Elements.Where(e => e != Document.Root).ToList();
        var names = Component.ControlTypeNames.ToHashSet(StringComparer.Ordinal);
        return mapped.FirstOrDefault(e => names.Contains(e.LocalName) && ClassesFit(e))
            ?? mapped.FirstOrDefault(e => map.ControlOf(e) is not (Panel or Decorator or ContentPresenter))
            ?? mapped.FirstOrDefault();
    }

    // "StackPanel.button-group": the element must have the class the component names.
    private bool ClassesFit(XamlElement element)
    {
        var required = new List<string>();
        foreach (var control in Component.Controls)
        {
            var name = control.Split(' ')[0];
            name = name[(name.IndexOf(':') + 1)..];
            if (name == element.LocalName)
            {
                return true;
            }
            if (name.StartsWith(element.LocalName + ".", StringComparison.Ordinal))
            {
                required.Add(name[(element.LocalName.Length + 1)..]);
            }
        }
        var classes = element.Attribute("Classes")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        return required.Count == 0 || required.Any(classes.Contains);
    }

    /// <summary>Whether the grid changed <paramref name="item"/> on <paramref name="control"/>.</summary>
    public bool IsEdited(Control control, PropertyItem item) => _originals.ContainsKey((control, item.Property));

    /// <summary>Whether the grid changed <paramref name="control"/>'s classes.</summary>
    public bool AreClassesEdited(Control control) => _originalClasses.ContainsKey(control);

    /// <summary>Whether <paramref name="element"/> sets <paramref name="item"/> in the XAML as written.</summary>
    public static bool IsWritten(XamlElement element, PropertyItem item) =>
        element.Attribute(item.AttributeName) is not null || IsTextContent(element, item);

    /// <summary>The value <paramref name="element"/> writes for <paramref name="item"/>, as written, or null.</summary>
    public static string? Written(XamlElement element, PropertyItem item) =>
        element.Attribute(item.AttributeName)?.Value ?? (IsTextContent(element, item) ? element.Text : null);

    /// <summary>Sets <paramref name="item"/> on <paramref name="control"/> and writes it on <paramref name="element"/>.</summary>
    /// <returns>Whether the control took the value.</returns>
    public bool Set(XamlElement element, Control control, PropertyItem item, object? value)
    {
        var key = (control, item.Property);
        var original = _originals.TryGetValue(key, out var kept) ? kept : (control.IsSet(item.Property), control.GetValue(item.Property));
        try
        {
            control.SetValue(item.Property, value);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        _originals[key] = original;
        if (XamlValues.Format(value) is { } text)
        {
            if (IsTextContent(element, item))
            {
                Edits.SetText(element.Index, text);
            }
            else
            {
                Edits.SetAttribute(element.Index, item.AttributeName, text);
            }
        }
        return true;
    }

    /// <summary>Gives <paramref name="item"/> back the value it had before the grid changed it.</summary>
    public void Revert(XamlElement element, Control control, PropertyItem item)
    {
        if (!_originals.Remove((control, item.Property), out var original))
        {
            return;
        }
        if (original.WasSet)
        {
            control.SetValue(item.Property, original.Value);
        }
        else
        {
            control.ClearValue(item.Property);
        }
        Edits.Revert(element.Index, item.AttributeName);
        if (IsTextContent(element, item))
        {
            Edits.RevertText(element.Index);
        }
    }

    /// <summary>The control's own classes (not its pseudo-classes), in order.</summary>
    public static string[] ClassesOf(Control control) => [.. control.Classes.Where(c => !c.StartsWith(':'))];

    /// <summary>Gives <paramref name="control"/> exactly <paramref name="classes"/> and writes them on <paramref name="element"/>.</summary>
    public void SetClasses(XamlElement element, Control control, IReadOnlyList<string> classes)
    {
        if (!_originalClasses.ContainsKey(control))
        {
            _originalClasses[control] = ClassesOf(control);
        }
        control.Classes.Replace([.. classes]);
        if (classes.SequenceEqual(_originalClasses[control]))
        {
            _originalClasses.Remove(control);
            Edits.Revert(element.Index, "Classes");
        }
        else
        {
            Edits.SetAttribute(element.Index, "Classes", classes.Count == 0 ? null : string.Join(' ', classes));
        }
    }

    /// <summary>Gives <paramref name="control"/> back the classes it had before the grid changed them.</summary>
    public void RevertClasses(XamlElement element, Control control)
    {
        if (_originalClasses.Remove(control, out var original))
        {
            control.Classes.Replace(original);
            Edits.Revert(element.Index, "Classes");
        }
    }

    // <TextBlock>Hello</TextBlock>: the text is the element's content, not an attribute.
    private static bool IsTextContent(XamlElement element, PropertyItem item) =>
        item.Property.Name is "Content" or "Text"
        && !item.Property.IsAttached
        && element.Attribute(item.AttributeName) is null
        && !element.IsEmpty
        && element.Children.Count == 0
        && element.Text.Length > 0;

    private Control Create()
    {
        var demo = DemoRegistry.Factories[Demo.Id]();
        // Focus rings and shadows reach outside the demo (docs/site.md, "Live demos").
        demo.ClipToBounds = false;
        return demo;
    }
}
