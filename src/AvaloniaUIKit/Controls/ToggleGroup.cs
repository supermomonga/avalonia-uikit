using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's ToggleGroup (button/toggle.rs, base/toggle_group.rs): ToggleButtons
/// in a row, 8px apart, or joined with the <c>segmented</c> class. The group's
/// <c>outline</c> and size classes go on every toggle (ghost is the default).
/// Each toggle flips on its own and is its own Tab stop; the arrow keys do
/// nothing (base/toggle.rs). A click on a toggle raises <see cref="Click"/>
/// with every toggle's checked state after it (GPUI's <c>on_click</c>).
/// Disabling the group disables its toggles.
/// </summary>
public class ToggleGroup : StackPanel
{
    /// <summary>Raised when a toggle of the group is clicked, with the checked states it leaves.</summary>
    public static readonly RoutedEvent<ToggleGroupClickEventArgs> ClickEvent =
        RoutedEvent.Register<ToggleGroup, ToggleGroupClickEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private static readonly string[] Variants = ["outline"];

    static ToggleGroup()
    {
        // ToggleGroup lays its toggles out along Axis::Horizontal.
        OrientationProperty.OverrideDefaultValue<ToggleGroup>(Orientation.Horizontal);
    }

    /// <summary>Creates a group.</summary>
    public ToggleGroup()
    {
        Classes.CollectionChanged += (_, _) => PassClasses();
        Children.CollectionChanged += OnChildrenChanged;
        AddHandler(Button.ClickEvent, OnToggleClick);
    }

    /// <inheritdoc cref="ClickEvent"/>
    public event EventHandler<ToggleGroupClickEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>Each child's checked state, in order (false for a child that is not a toggle).</summary>
    public IReadOnlyList<bool> Checked => [.. Children.Select(c => c is ToggleButton { IsChecked: true })];

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var child in e.OldItems?.OfType<Control>() ?? [])
        {
            GroupClasses.Clear(child);
        }
        PassClasses();
    }

    // toggle.rs: the group's size and variant go on every toggle.
    private void PassClasses()
    {
        var classes = GroupClasses.Pick(this, Variants, GroupClasses.Sizes);
        foreach (var child in Children)
        {
            GroupClasses.Apply(child, classes);
        }
    }

    // toggle.rs: the clicked toggle's state flips in the reported states. A
    // ToggleButton has flipped itself by the time it raises Click.
    private void OnToggleClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is ToggleButton toggle && Children.Contains(toggle))
        {
            RaiseEvent(new ToggleGroupClickEventArgs(ClickEvent, Checked));
        }
    }
}

/// <summary>The checked states a click on a <see cref="ToggleGroup"/>'s toggle leaves.</summary>
public class ToggleGroupClickEventArgs(RoutedEvent routedEvent, IReadOnlyList<bool> @checked) : RoutedEventArgs(routedEvent)
{
    /// <summary>Each toggle's checked state after the click, in order.</summary>
    public IReadOnlyList<bool> Checked { get; } = @checked;
}
