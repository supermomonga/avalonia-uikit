using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's ButtonGroup (button/button_group.rs): Buttons in a row (or a
/// column, <see cref="StackPanel.Orientation"/>) that share their inner edges.
/// The group's variant, <c>outline</c>, <c>compact</c> and size classes go on
/// every button. A button shows as selected with its <c>selected</c> class;
/// a click on a button raises <see cref="Click"/> with the indices the
/// selection becomes (GPUI's <c>on_click</c>): the clicked button alone, or
/// with <see cref="Multiple"/> the selection with the clicked one toggled.
/// As in GPUI, the app applies the selection (the buttons' <c>selected</c>
/// classes). Disabling the group disables its buttons.
/// </summary>
public class ButtonGroup : StackPanel
{
    /// <summary>Whether a click toggles the button in the selection rather than selecting it alone (GPUI's default is false).</summary>
    public static readonly StyledProperty<bool> MultipleProperty =
        AvaloniaProperty.Register<ButtonGroup, bool>(nameof(Multiple));

    /// <summary>Raised when a button of the group is clicked, with the selection that click makes.</summary>
    public static readonly RoutedEvent<ButtonGroupClickEventArgs> ClickEvent =
        RoutedEvent.Register<ButtonGroup, ButtonGroupClickEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    static ButtonGroup()
    {
        // ButtonGroup::layout: Axis::Horizontal by default.
        OrientationProperty.OverrideDefaultValue<ButtonGroup>(Orientation.Horizontal);
    }

    /// <summary>Creates a group.</summary>
    public ButtonGroup()
    {
        Classes.CollectionChanged += (_, _) => PassClasses();
        Children.CollectionChanged += OnChildrenChanged;
        AddHandler(Button.ClickEvent, OnButtonClick);
    }

    /// <inheritdoc cref="MultipleProperty"/>
    public bool Multiple
    {
        get => GetValue(MultipleProperty);
        set => SetValue(MultipleProperty, value);
    }

    /// <inheritdoc cref="ClickEvent"/>
    public event EventHandler<ButtonGroupClickEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>The indices of the buttons with the <c>selected</c> class, in order.</summary>
    public IReadOnlyList<int> SelectedIndices =>
        [.. Children.Select((child, ix) => (child, ix)).Where(c => c.child.Classes.Contains("selected")).Select(c => c.ix)];

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var child in e.OldItems?.OfType<Control>() ?? [])
        {
            GroupClasses.Clear(child);
        }
        PassClasses();
    }

    // button_group.rs: size, variant, compact and outline go on each child.
    private void PassClasses()
    {
        var classes = GroupClasses.Pick(this, GroupClasses.ButtonVariants, GroupClasses.Sizes);
        foreach (var child in Children)
        {
            GroupClasses.Apply(child, classes);
        }
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        var ix = e.Source is Button button ? Children.IndexOf(button) : -1;
        if (ix < 0)
        {
            return;
        }
        // button_group.rs: single selection selects the clicked button alone;
        // multiple removes it from the selection, or adds it at the end.
        var next = SelectedIndices.ToList();
        if (Multiple)
        {
            if (!next.Remove(ix))
            {
                next.Add(ix);
            }
        }
        else
        {
            next = [ix];
        }
        RaiseEvent(new ButtonGroupClickEventArgs(ClickEvent, next));
    }
}

/// <summary>The selection a click on a <see cref="ButtonGroup"/>'s button makes.</summary>
public class ButtonGroupClickEventArgs(RoutedEvent routedEvent, IReadOnlyList<int> selectedIndices) : RoutedEventArgs(routedEvent)
{
    /// <summary>The indices of the buttons selected after the click.</summary>
    public IReadOnlyList<int> SelectedIndices { get; } = selectedIndices;
}
