using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's tab under a dragged panel (dock/tab_panel.rs drag_over): Dock
/// gives a tab no state while it is the drop target, only an adorner.
/// </summary>
public static class DockTargets
{
    /// <summary>
    /// On a DockTarget: while it adorns a tab's insertion marker (a Border with
    /// the class <c>uikit-dock-insert</c> in the tab's template), the tab has the
    /// class <c>uikit-drop-target</c>, for the theme to drop its right border as
    /// GPUI does.
    /// </summary>
    public static readonly AttachedProperty<bool> MarksTabProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("MarksTab", typeof(DockTargets));

    static DockTargets()
    {
        MarksTabProperty.Changed.AddClassHandler<Control>((target, e) =>
        {
            target.AttachedToLogicalTree -= OnAttached;
            target.DetachedFromLogicalTree -= OnDetached;
            if (e.GetNewValue<bool>())
            {
                target.AttachedToLogicalTree += OnAttached;
                target.DetachedFromLogicalTree += OnDetached;
            }
        });
    }

    /// <summary>Gets whether the target marks the tab it adorns.</summary>
    public static bool GetMarksTab(Control target) => target.GetValue(MarksTabProperty);

    /// <summary>Sets whether the target marks the tab it adorns.</summary>
    public static void SetMarksTab(Control target, bool value) => target.SetValue(MarksTabProperty, value);

    private static void OnAttached(object? sender, LogicalTreeAttachmentEventArgs e) => Mark((Control)sender!, e.Parent, true);

    private static void OnDetached(object? sender, LogicalTreeAttachmentEventArgs e) => Mark((Control)sender!, e.Parent, false);

    // Dock sets the adorned element as the adorner's logical parent.
    private static void Mark(Control target, ILogical? parent, bool marked)
    {
        if (parent is Border { TemplatedParent: Control tab } marker && marker.Classes.Contains("uikit-dock-insert"))
        {
            tab.Classes.Set("uikit-drop-target", marked);
        }
    }
}
