using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>The parts of GPUI Kit's GroupBox that Avalonia's GroupBox has no slot for.</summary>
public static class GroupBoxes
{
    /// <summary>
    /// Supporting content under the group's surface (group_box.rs footer): the
    /// theme shows it 8px below the surface, from the title's leading edge, as
    /// small muted text.
    /// </summary>
    public static readonly AttachedProperty<object?> FooterProperty =
        AvaloniaProperty.RegisterAttached<GroupBox, object?>("Footer", typeof(GroupBoxes));

    /// <summary>Gets the group's footer.</summary>
    public static object? GetFooter(GroupBox element) => element.GetValue(FooterProperty);

    /// <summary>Sets the group's footer.</summary>
    public static void SetFooter(GroupBox element, object? value) => element.SetValue(FooterProperty, value);
}
