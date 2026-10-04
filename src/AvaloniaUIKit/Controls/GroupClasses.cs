using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// Passes a group's style classes on to its children, as GPUI Kit's groups
/// pass their size and variant to each child (button_group.rs, toggle.rs,
/// accordion.rs, toolbar.rs). The classes a child got from its group are
/// remembered, so they leave the child when the group drops them or the child
/// leaves the group; a class the child had of its own stays.
/// </summary>
internal static class GroupClasses
{
    /// <summary>The variants and modifiers of a button, as the Button theme names them.</summary>
    public static readonly string[] ButtonVariants =
        ["primary", "secondary", "danger", "warning", "success", "info", "ghost", "link", "text", "outline", "compact"];

    /// <summary>The size classes.</summary>
    public static readonly string[] Sizes = ["xsmall", "small", "large"];

    private static readonly AttachedProperty<string[]?> AppliedProperty =
        AvaloniaProperty.RegisterAttached<Control, string[]?>("Applied", typeof(GroupClasses));

    /// <summary>The classes of <paramref name="group"/> that are among <paramref name="kinds"/>.</summary>
    public static List<string> Pick(Control group, params string[][] kinds) =>
        [.. group.Classes.Where(c => kinds.Any(k => k.Contains(c)))];

    /// <summary>Gives <paramref name="child"/> exactly <paramref name="classes"/> from its group.</summary>
    public static void Apply(Control child, IReadOnlyCollection<string> classes)
    {
        var old = child.GetValue(AppliedProperty) ?? [];
        foreach (var name in old)
        {
            if (!classes.Contains(name))
            {
                child.Classes.Remove(name);
            }
        }
        var applied = new List<string>();
        foreach (var name in classes)
        {
            if (old.Contains(name))
            {
                applied.Add(name);
            }
            else if (!child.Classes.Contains(name))
            {
                child.Classes.Add(name);
                applied.Add(name);
            }
        }
        child.SetValue(AppliedProperty, applied.Count == 0 ? null : applied.ToArray());
    }

    /// <summary>Takes back the classes <paramref name="child"/> got from its group.</summary>
    public static void Clear(Control child) => Apply(child, []);
}
