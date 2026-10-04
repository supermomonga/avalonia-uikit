using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Toolbar (toolbar.rs, base/toolbar.rs): a transparent row of
/// commands and any other content, vertically centered, with
/// <see cref="ToolbarSpacer"/>s sharing the width left over. Sizes are the
/// classes <c>xsmall</c>, none (GPUI's default Small) and <c>medium</c>
/// (<c>large</c> is the same). The toolbar's size goes on its controls (GPUI's
/// <c>child</c>): the size class on every templated control but a Separator,
/// and plain buttons become ghost and compact. A control with
/// <see cref="TakesSizeProperty"/> false keeps its own (GPUI's <c>content</c>),
/// as do text, panels, separators and spacers. When the focus is on one of its
/// controls, Left and Right move it to the previous or next focusable control,
/// wrapping at the ends; a control that takes the arrow keys itself (a TextBox)
/// keeps them. Tab moves through the controls as usual.
/// </summary>
public class Toolbar : Panel
{
    /// <summary>The space around the row (set by the size classes).</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<Toolbar>();

    /// <summary>The gap between items (set by the size classes).</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        StackPanel.SpacingProperty.AddOwner<Toolbar>();

    /// <summary>
    /// Whether a control in a toolbar or toolbar group takes its size (GPUI's
    /// <c>child</c>; true by default). False keeps the control's own size and
    /// variant (GPUI's <c>content</c>).
    /// </summary>
    public static readonly AttachedProperty<bool> TakesSizeProperty =
        AvaloniaProperty.RegisterAttached<Toolbar, Control, bool>("TakesSize", true);

    static Toolbar()
    {
        AffectsMeasure<Toolbar>(PaddingProperty, SpacingProperty);
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<Toolbar>(AutomationControlType.ToolBar);
        TakesSizeProperty.Changed.AddClassHandler<Control>((control, _) => ToolbarLayout.Resize(control.Parent as Panel));
    }

    /// <summary>Creates a toolbar.</summary>
    public Toolbar()
    {
        Classes.CollectionChanged += (_, _) => ToolbarLayout.Resize(this);
        Children.CollectionChanged += (_, e) => ToolbarLayout.OnChildrenChanged(this, e);
    }

    /// <inheritdoc cref="PaddingProperty"/>
    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    /// <inheritdoc cref="SpacingProperty"/>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>Gets whether the control takes the toolbar's size.</summary>
    public static bool GetTakesSize(Control control) => control.GetValue(TakesSizeProperty);

    /// <summary>Sets whether the control takes the toolbar's size.</summary>
    public static void SetTakesSize(Control control, bool value) => control.SetValue(TakesSizeProperty, value);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) =>
        ToolbarLayout.Measure(this, availableSize, Spacing, Padding);

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize) =>
        ToolbarLayout.Arrange(this, finalSize, Spacing, Padding);

    // base/toolbar.rs: Left and Right move the focus along the bar, wrapping.
    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.KeyModifiers != KeyModifiers.None || e.Key is not (Key.Left or Key.Right))
        {
            return;
        }
        var stops = this.GetVisualDescendants().OfType<InputElement>()
            .Where(c => c.Focusable && c.IsEffectivelyEnabled && c.IsEffectivelyVisible && KeyboardNavigation.GetIsTabStop(c))
            .ToList();
        var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
        var current = stops.FindIndex(s => s == focused || s.IsVisualAncestorOf(focused));
        if (current < 0)
        {
            return;
        }
        var next = (current + (e.Key == Key.Right ? 1 : stops.Count - 1)) % stops.Count;
        stops[next].Focus(NavigationMethod.Directional);
        e.Handled = true;
    }
}

/// <summary>
/// GPUI Kit's ToolbarGroup (toolbar.rs): a run of related controls inside a
/// <see cref="Toolbar"/>, announced as one group (its accessible name is
/// AutomationProperties.Name, GPUI's <c>label</c>). It takes the toolbar's
/// size and passes its own to its controls as the toolbar does; on its own its
/// size is the classes <c>xsmall</c>, none (small) and <c>medium</c>. No gap
/// by default (<see cref="Spacing"/>).
/// </summary>
public class ToolbarGroup : Panel
{
    /// <summary>The gap between items.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        StackPanel.SpacingProperty.AddOwner<ToolbarGroup>();

    static ToolbarGroup()
    {
        AffectsMeasure<ToolbarGroup>(SpacingProperty);
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<ToolbarGroup>(AutomationControlType.Group);
    }

    /// <summary>Creates a group.</summary>
    public ToolbarGroup()
    {
        Classes.CollectionChanged += (_, _) => ToolbarLayout.Resize(this);
        Children.CollectionChanged += (_, e) => ToolbarLayout.OnChildrenChanged(this, e);
    }

    /// <inheritdoc cref="SpacingProperty"/>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) =>
        ToolbarLayout.Measure(this, availableSize, Spacing, default);

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize) =>
        ToolbarLayout.Arrange(this, finalSize, Spacing, default);
}

/// <summary>
/// A flexible space in a <see cref="Toolbar"/> (GPUI's <c>content(div().flex_1())</c>):
/// the spacers share the width the other items leave, pushing the items after them to the end.
/// </summary>
public class ToolbarSpacer : Control
{
}

/// <summary>The row layout and the size passing of <see cref="Toolbar"/> and <see cref="ToolbarGroup"/>.</summary>
internal static class ToolbarLayout
{
    /// <summary>Measures the visible children in a row, spacers taking no width.</summary>
    public static Size Measure(Panel panel, Size available, double spacing, Thickness padding)
    {
        var inner = available.Deflate(padding);
        double width = 0, height = 0;
        var count = 0;
        foreach (var child in panel.Children)
        {
            if (!child.IsVisible)
            {
                continue;
            }
            child.Measure(child is ToolbarSpacer ? new Size(0, inner.Height) : new Size(double.PositiveInfinity, inner.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            count++;
        }
        return new Size(width + spacing * Math.Max(0, count - 1), height).Inflate(padding);
    }

    /// <summary>Arranges the children left to right, centered on the row (GPUI's items_center); spacers share the rest.</summary>
    public static Size Arrange(Panel panel, Size final, double spacing, Thickness padding)
    {
        var inner = new Rect(final).Deflate(padding);
        var visible = panel.Children.Where(c => c.IsVisible).ToList();
        var used = visible.Where(c => c is not ToolbarSpacer).Sum(c => c.DesiredSize.Width) + spacing * Math.Max(0, visible.Count - 1);
        var spacers = visible.Count(c => c is ToolbarSpacer);
        var share = spacers > 0 ? Math.Max(0, inner.Width - used) / spacers : 0;
        var x = inner.X;
        foreach (var child in visible)
        {
            var width = child is ToolbarSpacer ? share : child.DesiredSize.Width;
            var height = Math.Min(child.DesiredSize.Height, inner.Height);
            child.Arrange(new Rect(x, inner.Y + (inner.Height - height) / 2, width, height));
            x += width + spacing;
        }
        return final;
    }

    public static void OnChildrenChanged(Panel panel, NotifyCollectionChangedEventArgs e)
    {
        foreach (var child in e.OldItems?.OfType<Control>() ?? [])
        {
            GroupClasses.Clear(child);
        }
        Resize(panel);
    }

    /// <summary>Gives the children of a toolbar or toolbar group the classes of its size.</summary>
    public static void Resize(Panel? panel)
    {
        if (panel is not (Toolbar or ToolbarGroup))
        {
            return;
        }
        // toolbar.rs: Size::Large falls back to Medium; Small is the default.
        var size = panel.Classes.Contains("xsmall") ? "xsmall"
            : panel.Classes.Contains("medium") || panel.Classes.Contains("large") ? "medium"
            : "small";
        foreach (var child in panel.Children)
        {
            GroupClasses.Apply(child, ChildClasses(child, size));
        }
    }

    // ToolbarItem::sized: prepare_for_toolbar (a Button turns ghost and compact),
    // then with_size; a group passes its size on.
    private static string[] ChildClasses(Control child, string size)
    {
        if (!Toolbar.GetTakesSize(child))
        {
            return [];
        }
        if (child is ToolbarGroup)
        {
            return [size];
        }
        if (child is not TemplatedControl || child is Separator)
        {
            return [];
        }
        string[] sized = size == "medium" ? [] : [size];
        return child is Button and not ToggleButton and not HyperlinkButton ? [.. sized, "ghost", "compact"] : sized;
    }
}
