using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using AvaloniaUIKit.Demo.ControlCatalog.Xaml;

namespace AvaloniaUIKit.Demo.ControlCatalog.Inspector;

/// <summary>A property the grid shows, with the attribute XAML writes it as.</summary>
public sealed record PropertyItem(AvaloniaProperty Property, string AttributeName);

/// <summary>A titled group of the grid's properties.</summary>
public sealed record PropertyGroup(string Title, bool IsExpandedByDefault, IReadOnlyList<PropertyItem> Items);

/// <summary>
/// The properties the grid edits on a control, from Avalonia's property registry (no
/// reflection, ADR 11): those its type and its base types register, grouped by the type
/// that registers them, and the attached properties of AvaloniaUIKit and of the control's
/// panel that apply to it.
/// </summary>
public static class PropertyCatalog
{
    // Properties that are not about the control's look or behavior, or that the grid edits elsewhere.
    private static readonly HashSet<string> Hidden =
    [
        "Name", "DataContext", "Theme", "Template", "Tag", "Classes", "ContentTemplate", "ItemTemplate", "Cursor",
        "FocusAdorner", "ContextMenu", "ContextFlyout", "Flyout", "Command", "CommandParameter", "RenderTransform",
        "RenderTransformOrigin", "OpacityMask", "Effect", "Clip", "Transitions", "ItemsSource", "ItemsPanel",
        "SelectedItem", "SelectedItems", "Selection", "DisplayMemberBinding", "ItemContainerTheme", "Styles",
    ];

    // The base types every control shares, by the group their properties go in.
    private static readonly Dictionary<Type, string> BaseGroups = new()
    {
        [typeof(TemplatedControl)] = "Appearance",
        [typeof(Control)] = "Control",
        [typeof(InputElement)] = "Input",
        [typeof(Avalonia.Interactivity.Interactive)] = "Input",
        [typeof(Layoutable)] = "Layout",
        [typeof(Visual)] = "Visual",
        [typeof(StyledElement)] = "Other",
        [typeof(Avalonia.Animation.Animatable)] = "Other",
        [typeof(AvaloniaObject)] = "Other",
    };

    private static readonly string[] BaseOrder = ["Appearance", "Layout", "Input", "Visual", "Control", "Other"];

    /// <summary>The AvaloniaUIKit attached properties the documentation describes, and the controls they go on.</summary>
    internal static readonly (AvaloniaProperty Property, Type Host)[] UIKitAttached =
    [
        (Buttons.IsLoadingProperty, typeof(Button)),
        (Buttons.LoadingIconProperty, typeof(Button)),
        (Buttons.TakesFocusOnPointerProperty, typeof(Button)),
        (Inputs.PatternProperty, typeof(TextBox)),
        (Inputs.MaskPatternProperty, typeof(TextBox)),
        (Inputs.CleanOnEscapeProperty, typeof(TextBox)),
        (Inputs.TabSizeProperty, typeof(TextBox)),
        (Inputs.HardTabsProperty, typeof(TextBox)),
        (Tabs.ClosableProperty, typeof(TabStrip)),
        (Tabs.ClosableProperty, typeof(TabControl)),
        (Tabs.ClosableProperty, typeof(TabStripItem)),
        (Tabs.ClosableProperty, typeof(TabItem)),
        (Tabs.ReorderableProperty, typeof(TabStrip)),
        (Tabs.ReorderableProperty, typeof(TabControl)),
        (Tabs.DragGroupProperty, typeof(TabStrip)),
        (Tabs.DragGroupProperty, typeof(TabControl)),
        (Tabs.PrefixProperty, typeof(TabStrip)),
        (Tabs.PrefixProperty, typeof(TabControl)),
        (Tabs.SuffixProperty, typeof(TabStrip)),
        (Tabs.SuffixProperty, typeof(TabControl)),
        (Separators.LabelProperty, typeof(Separator)),
        (Spinners.IconProperty, typeof(ProgressBar)),
        (ProgressCircles.ContentProperty, typeof(ProgressBar)),
        (GroupBoxes.FooterProperty, typeof(GroupBox)),
        (Scrollbars.ShowOnHoverProperty, typeof(ScrollViewer)),
        (Carousels.TracksPointerProperty, typeof(Carousel)),
        (Form.LabelOrientationProperty, typeof(Form)),
        (Form.LabelWidthProperty, typeof(Form)),
        (Form.FieldGapProperty, typeof(Form)),
        (Sidebar.IsIconCollapsedProperty, typeof(SidebarMenu)),
    ];

    /// <summary>The groups of properties the grid shows for <paramref name="control"/>.</summary>
    public static IReadOnlyList<PropertyGroup> For(Control control)
    {
        var groups = new List<PropertyGroup>();
        var registry = AvaloniaPropertyRegistry.Instance;
        var seen = new HashSet<AvaloniaProperty>();
        var byBase = new Dictionary<string, List<PropertyItem>>();
        for (var type = control.GetType(); type is not null && typeof(AvaloniaObject).IsAssignableFrom(type); type = type.BaseType)
        {
            // The registry lists a type's properties with its base types'; a type's own are the difference.
            var inherited = type.BaseType is { } baseType && typeof(AvaloniaObject).IsAssignableFrom(baseType)
                ? registry.GetRegistered(baseType).ToHashSet()
                : [];
            var own = registry.GetRegistered(type)
                .Where(p => !inherited.Contains(p) && seen.Add(p) && IsShown(p, control))
                // An attached property a type adds as its own (Button's HotKey) is written by its name;
                // one the type itself owns (Sidebar.IsIconCollapsed) with its owner.
                .Select(p => new PropertyItem(p, p.IsAttached && p.OwnerType == type ? AttributeName(p) : p.Name))
                .ToList();
            if (own.Count == 0)
            {
                continue;
            }
            if (BaseGroups.TryGetValue(type, out var title))
            {
                if (!byBase.TryGetValue(title, out var items))
                {
                    byBase[title] = items = [];
                }
                items.AddRange(own);
            }
            else
            {
                groups.Add(new PropertyGroup(type.Name, true, Sorted(own)));
            }
        }

        var attached = Attached(control).Where(p => IsShown(p.Property, control)).ToList();
        if (attached.Count > 0)
        {
            groups.Add(new PropertyGroup("Attached", true, Sorted(attached)));
        }
        foreach (var title in BaseOrder)
        {
            if (byBase.TryGetValue(title, out var items))
            {
                groups.Add(new PropertyGroup(title, false, Sorted(items)));
            }
        }
        return groups;
    }

    /// <summary>The attribute XAML sets <paramref name="property"/> with: its name, or <c>Owner.Name</c> with the owner's prefix.</summary>
    public static string AttributeName(AvaloniaProperty property)
    {
        if (!property.IsAttached)
        {
            return property.Name;
        }
        var prefix = property.OwnerType.Namespace == "AvaloniaUIKit" ? "uikit:" : "";
        return $"{prefix}{property.OwnerType.Name}.{property.Name}";
    }

    private static IEnumerable<PropertyItem> Attached(Control control)
    {
        foreach (var (property, host) in UIKitAttached)
        {
            if (host.IsInstanceOfType(control))
            {
                yield return new PropertyItem(property, AttributeName(property));
            }
        }
        if (control.Parent is Toolbar or ToolbarGroup)
        {
            yield return new PropertyItem(Toolbar.TakesSizeProperty, AttributeName(Toolbar.TakesSizeProperty));
        }
        switch (control.Parent)
        {
            case DockPanel:
                yield return new PropertyItem(DockPanel.DockProperty, "DockPanel.Dock");
                break;
            case Grid:
                yield return new PropertyItem(Grid.RowProperty, "Grid.Row");
                yield return new PropertyItem(Grid.ColumnProperty, "Grid.Column");
                yield return new PropertyItem(Grid.RowSpanProperty, "Grid.RowSpan");
                yield return new PropertyItem(Grid.ColumnSpanProperty, "Grid.ColumnSpan");
                break;
            case Canvas:
                yield return new PropertyItem(Canvas.LeftProperty, "Canvas.Left");
                yield return new PropertyItem(Canvas.TopProperty, "Canvas.Top");
                break;
        }
        yield return new PropertyItem(ToolTip.TipProperty, "ToolTip.Tip");
    }

    private static bool IsShown(AvaloniaProperty property, Control control)
    {
        if (property.IsReadOnly || Hidden.Contains(property.Name))
        {
            return false;
        }
        if (XamlValues.IsEditable(property.PropertyType))
        {
            return true;
        }
        // Content of any type: text while it is text or nothing.
        return property.PropertyType == typeof(object) && control.GetValue(property) is null or string;
    }

    private static List<PropertyItem> Sorted(IEnumerable<PropertyItem> items) =>
        [.. items.OrderBy(i => i.AttributeName, StringComparer.OrdinalIgnoreCase)];
}
