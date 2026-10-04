using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Dock.Controls.ProportionalStackPanel;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's split handles (resizable.rs) for Dock.Avalonia's
/// ProportionalStackPanelSplitter.
/// </summary>
public static class DockSplitters
{
    /// <summary>
    /// On a splitter: it straddles the boundary over its neighbours, as GPUI's
    /// handle does. The splitter's item is raised above the panels (its band
    /// and hairline overlap them), and the splitter gets the class
    /// <c>columns</c> or <c>rows</c> from its panel's orientation, for the
    /// theme to lay the band out.
    /// </summary>
    public static readonly AttachedProperty<bool> StraddlesProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Straddles", typeof(DockSplitters));

    static DockSplitters()
    {
        StraddlesProperty.Changed.AddClassHandler<Control>((splitter, e) =>
        {
            splitter.AttachedToVisualTree -= OnAttached;
            if (e.GetNewValue<bool>())
            {
                splitter.AttachedToVisualTree += OnAttached;
                if (splitter.IsAttachedToVisualTree())
                {
                    Update(splitter);
                }
            }
        });
    }

    /// <summary>Gets whether the splitter straddles its boundary.</summary>
    public static bool GetStraddles(Control splitter) => splitter.GetValue(StraddlesProperty);

    /// <summary>Sets whether the splitter straddles its boundary.</summary>
    public static void SetStraddles(Control splitter, bool value) => splitter.SetValue(StraddlesProperty, value);

    private static void OnAttached(object? sender, VisualTreeAttachmentEventArgs e) => Update((Control)sender!);

    private static void Update(Control splitter)
    {
        // The panel's child is the splitter's item container (or the splitter itself).
        Visual item = splitter;
        while (item.GetVisualParent() is { } parent and not ProportionalStackPanel)
        {
            item = parent;
        }
        if (item.GetVisualParent() is not ProportionalStackPanel panel)
        {
            return;
        }
        if (item is Control control)
        {
            control.ZIndex = 1;
        }
        var rows = panel.Orientation == Orientation.Vertical;
        splitter.Classes.Set("rows", rows);
        splitter.Classes.Set("columns", !rows);
    }
}
