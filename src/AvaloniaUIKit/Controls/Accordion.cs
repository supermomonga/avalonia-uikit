using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Accordion (accordion.rs): Expanders stacked in a card (or without
/// it, <see cref="IsBordered"/>), each but the last with a separator below.
/// Only one item is open at a time unless <see cref="Multiple"/>: opening an
/// item closes the others, which reveal and hide with the Expander's spring.
/// A click on an item's title raises <see cref="ToggleClick"/> with the indices
/// of the open items (GPUI's <c>on_toggle_click</c>). The size classes go on
/// every item. Disabling the accordion disables its items.
/// </summary>
public class Accordion : ItemsControl
{
    /// <summary>Whether several items may be open at once (GPUI's default is false).</summary>
    public static readonly StyledProperty<bool> MultipleProperty =
        AvaloniaProperty.Register<Accordion, bool>(nameof(Multiple));

    /// <summary>Whether the items sit in a card with a border (GPUI's <c>bordered</c>, true by default).</summary>
    public static readonly StyledProperty<bool> IsBorderedProperty =
        AvaloniaProperty.Register<Accordion, bool>(nameof(IsBordered), true);

    /// <summary>Raised when an item's title is clicked, with the items open after it.</summary>
    public static readonly RoutedEvent<AccordionToggleEventArgs> ToggleClickEvent =
        RoutedEvent.Register<Accordion, AccordionToggleEventArgs>(nameof(ToggleClick), RoutingStrategies.Bubble);

    static Accordion()
    {
        Expander.IsExpandedProperty.Changed.AddClassHandler<Expander>((item, e) =>
        {
            if (e.GetNewValue<bool>() && item.Parent is Accordion accordion)
            {
                accordion.OnItemOpened(item);
            }
        });
    }

    /// <summary>Creates an accordion.</summary>
    public Accordion()
    {
        Classes.CollectionChanged += (_, _) => PassClasses();
        AddHandler(Button.ClickEvent, OnHeaderClick);
    }

    /// <inheritdoc cref="MultipleProperty"/>
    public bool Multiple
    {
        get => GetValue(MultipleProperty);
        set => SetValue(MultipleProperty, value);
    }

    /// <inheritdoc cref="IsBorderedProperty"/>
    public bool IsBordered
    {
        get => GetValue(IsBorderedProperty);
        set => SetValue(IsBorderedProperty, value);
    }

    /// <inheritdoc cref="ToggleClickEvent"/>
    public event EventHandler<AccordionToggleEventArgs>? ToggleClick
    {
        add => AddHandler(ToggleClickEvent, value);
        remove => RemoveHandler(ToggleClickEvent, value);
    }

    /// <summary>The indices of the open items, in order.</summary>
    public IReadOnlyList<int> OpenIndices =>
        [.. GetRealizedContainers().OfType<Expander>().Where(e => e.IsExpanded).Select(IndexFromContainer).Order()];

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        GroupClasses.Apply(container, GroupClasses.Pick(this, GroupClasses.Sizes));
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        GroupClasses.Clear(container);
    }

    // accordion.rs: the accordion's size goes on every item.
    private void PassClasses()
    {
        var classes = GroupClasses.Pick(this, GroupClasses.Sizes);
        foreach (var container in GetRealizedContainers())
        {
            GroupClasses.Apply(container, classes);
        }
    }

    // accordion.rs: opening an item without `multiple` clears the open set first.
    private void OnItemOpened(Expander opened)
    {
        if (Multiple || IndexFromContainer(opened) < 0)
        {
            return;
        }
        foreach (var item in GetRealizedContainers().OfType<Expander>())
        {
            if (item != opened && item.IsExpanded)
            {
                item.SetCurrentValue(Expander.IsExpandedProperty, false);
            }
        }
    }

    // The title row is the Expander's header toggle; it has flipped the item (and
    // the item closed the others) by the time it raises Click.
    private void OnHeaderClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is ToggleButton { TemplatedParent: Expander item } && IndexFromContainer(item) >= 0)
        {
            RaiseEvent(new AccordionToggleEventArgs(ToggleClickEvent, OpenIndices));
        }
    }
}

/// <summary>The items open after a click on an <see cref="Accordion"/> item's title.</summary>
public class AccordionToggleEventArgs(RoutedEvent routedEvent, IReadOnlyList<int> openIndices) : RoutedEventArgs(routedEvent)
{
    /// <summary>The indices of the open items, in order.</summary>
    public IReadOnlyList<int> OpenIndices { get; } = openIndices;
}
