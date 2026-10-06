using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace AvaloniaUIKit;

/// <summary>
/// Closing, adding and dragging the tabs of a TabStrip or TabControl (ADR 33),
/// what Tabalonia's TabsControl used to add: a close button in each tab (the
/// GPUI Kit tabs story's closable tab), an add button after the last tab,
/// reordering by drag, moving tabs between bars of a group and out into new
/// windows. They change the bar's ItemsSource (a list that can change) or its
/// Items.
/// </summary>
public static partial class Tabs
{
    /// <summary>
    /// Shows a close button in the tabs (on a TabStrip or TabControl, its tabs;
    /// on a tab, that tab): the tabs story's closable tab, padded px_2 with a
    /// ghost xsmall Close button as its suffix. The button raises
    /// <see cref="TabClosingEvent"/> and, unless it is cancelled, removes the
    /// tab's item; closing the selected tab selects the next one.
    /// </summary>
    public static readonly AttachedProperty<bool> ClosableProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Closable", typeof(Tabs), inherits: true);

    /// <summary>
    /// Shows an add button after the last tab (Avalonia-only: GPUI's TabBar has
    /// none). The button adds the item the function returns at the end and
    /// selects it; a null item adds nothing.
    /// </summary>
    public static readonly AttachedProperty<Func<object?>?> NewTabFactoryProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, Func<object?>?>("NewTabFactory", typeof(Tabs));

    /// <summary>
    /// Lets the tabs be dragged along the bar to reorder them. The dragged tab
    /// follows the pointer over the others, with the :dragging pseudo-class
    /// (the theme draws the selected tab's indicator on it, or, without one,
    /// the active tab's background), and the others make room for it; the
    /// drop moves its item. A bar that overflows scrolls while the tab is held
    /// at its ends.
    /// </summary>
    public static readonly AttachedProperty<bool> ReorderableProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, bool>("Reorderable", typeof(Tabs));

    /// <summary>
    /// Names the group of bars a dragged tab can move between (with
    /// <see cref="ReorderableProperty"/>): held over another bar of the group,
    /// the tab moves to it. Bars in different windows exchange tabs; in other
    /// top levels (a browser's views), only bars in the same one do.
    /// </summary>
    public static readonly AttachedProperty<string?> DragGroupProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, string?>("DragGroup", typeof(Tabs));

    /// <summary>
    /// Creates the window a tab dragged off every bar of its group moves to
    /// (with <see cref="DragGroupProperty"/>; in a window only). The window must
    /// hold a reorderable TabStrip or TabControl of the same group, empty. It
    /// follows the pointer until the drop; held over a bar, the tab moves back.
    /// A window made here closes when its last tab is dragged out or closed, and
    /// dragging its only tab moves the window.
    /// </summary>
    public static readonly AttachedProperty<Func<object?, Window?>?> DetachedWindowFactoryProperty =
        AvaloniaProperty.RegisterAttached<SelectingItemsControl, Func<object?, Window?>?>("DetachedWindowFactory", typeof(Tabs));

    /// <summary>
    /// Raised on the TabStrip or TabControl when a tab's close button is
    /// pressed, before the tab's item is removed. Set
    /// <see cref="TabClosingEventArgs.Cancel"/> to keep it.
    /// </summary>
    public static readonly RoutedEvent<TabClosingEventArgs> TabClosingEvent =
        RoutedEvent.Register<TabClosingEventArgs>("TabClosing", RoutingStrategies.Bubble, typeof(Tabs));

    /// <summary>Gets whether the tabs show a close button.</summary>
    public static bool GetClosable(Control element) => element.GetValue(ClosableProperty);

    /// <summary>Sets whether the tabs show a close button.</summary>
    public static void SetClosable(Control element, bool value) => element.SetValue(ClosableProperty, value);

    /// <summary>Gets the function the add button gets a new tab's item from.</summary>
    public static Func<object?>? GetNewTabFactory(SelectingItemsControl element) => element.GetValue(NewTabFactoryProperty);

    /// <summary>Sets the function the add button gets a new tab's item from.</summary>
    public static void SetNewTabFactory(SelectingItemsControl element, Func<object?>? value) => element.SetValue(NewTabFactoryProperty, value);

    /// <summary>Gets whether the tabs can be dragged.</summary>
    public static bool GetReorderable(SelectingItemsControl element) => element.GetValue(ReorderableProperty);

    /// <summary>Sets whether the tabs can be dragged.</summary>
    public static void SetReorderable(SelectingItemsControl element, bool value) => element.SetValue(ReorderableProperty, value);

    /// <summary>Gets the group of bars the tabs can be dragged between.</summary>
    public static string? GetDragGroup(SelectingItemsControl element) => element.GetValue(DragGroupProperty);

    /// <summary>Sets the group of bars the tabs can be dragged between.</summary>
    public static void SetDragGroup(SelectingItemsControl element, string? value) => element.SetValue(DragGroupProperty, value);

    /// <summary>Gets the function that creates the window a dragged-off tab moves to.</summary>
    public static Func<object?, Window?>? GetDetachedWindowFactory(SelectingItemsControl element) => element.GetValue(DetachedWindowFactoryProperty);

    /// <summary>Sets the function that creates the window a dragged-off tab moves to.</summary>
    public static void SetDetachedWindowFactory(SelectingItemsControl element, Func<object?, Window?>? value) =>
        element.SetValue(DetachedWindowFactoryProperty, value);

    /// <summary>Adds a handler for <see cref="TabClosingEvent"/>.</summary>
    public static void AddTabClosingHandler(Interactive element, EventHandler<TabClosingEventArgs> handler) =>
        element.AddHandler(TabClosingEvent, handler);

    /// <summary>Removes a handler for <see cref="TabClosingEvent"/>.</summary>
    public static void RemoveTabClosingHandler(Interactive element, EventHandler<TabClosingEventArgs> handler) =>
        element.RemoveHandler(TabClosingEvent, handler);

    private static void RegisterEditing()
    {
        Button.ClickEvent.AddClassHandler<TabStripItem>(OnTabButtonClick);
        Button.ClickEvent.AddClassHandler<TabItem>(OnTabButtonClick);
        Button.ClickEvent.AddClassHandler<TabStrip>(OnBarButtonClick);
        Button.ClickEvent.AddClassHandler<TabControl>(OnBarButtonClick);
        ReorderableProperty.Changed.AddClassHandler<SelectingItemsControl>((bar, e) => TabDragSession.Watch(bar, e.GetNewValue<bool>()));
        DragGroupProperty.Changed.AddClassHandler<SelectingItemsControl>((bar, _) => TabDragSession.Join(bar));
    }

    private static void OnTabButtonClick(Control tab, RoutedEventArgs e)
    {
        if (e.Source is Button { Name: "PART_CloseButton" } button && button.TemplatedParent == tab &&
            ItemsControl.ItemsControlFromItemContainer(tab) is SelectingItemsControl bar)
        {
            e.Handled = true;
            Close(bar, tab);
        }
    }

    private static void OnBarButtonClick(SelectingItemsControl bar, RoutedEventArgs e)
    {
        if (e.Source is not Button { Name: "PART_AddButton" } button || button.TemplatedParent != bar)
        {
            return;
        }
        e.Handled = true;
        if (GetNewTabFactory(bar) is { } factory && TabList.Of(bar) is { } items && factory() is { } item)
        {
            items.Add(item);
            bar.SelectedItem = item;
        }
    }

    private static void Close(SelectingItemsControl bar, Control tab)
    {
        var index = bar.IndexFromContainer(tab);
        if (index < 0 || TabList.Of(bar) is not { } items)
        {
            return;
        }
        var closing = new TabClosingEventArgs(TabClosingEvent, bar.ItemFromContainer(tab), tab);
        bar.RaiseEvent(closing);
        if (closing.Cancel)
        {
            return;
        }
        var selected = bar.SelectedIndex == index;
        items.RemoveAt(index);
        TabList.Removed(bar, index, selected);
        if (items.Count == 0)
        {
            TabDragSession.CloseIfEmpty(TopLevel.GetTopLevel(bar) as Window);
        }
    }

    private static void OnDragged(SelectingItemsControl bar, bool moving) => Dragged?.Invoke(bar, moving);

    /// <summary>The list a bar's tabs come from, where they can be moved.</summary>
    internal static class TabList
    {
        /// <summary>The bar's ItemsSource, or its Items without one, if the list can change.</summary>
        public static IList? Of(ItemsControl bar)
        {
            var list = bar.ItemsSource is { } source ? source as IList : bar.Items;
            return list is { IsReadOnly: false, IsFixedSize: false } ? list : null;
        }

        /// <summary>After the item at <paramref name="index"/> left the bar: the tab that took its place is selected.</summary>
        public static void Removed(SelectingItemsControl bar, int index, bool wasSelected)
        {
            if (bar.ItemCount > 0 && (wasSelected || bar.SelectedIndex < 0))
            {
                bar.SelectedIndex = Math.Min(index, bar.ItemCount - 1);
            }
        }

        /// <summary>Raises the drag notification the indicator follows.</summary>
        public static void Dragged(SelectingItemsControl bar, bool moving) => OnDragged(bar, moving);
    }
}

/// <summary>The arguments of <see cref="Tabs.TabClosingEvent"/>.</summary>
public sealed class TabClosingEventArgs : RoutedEventArgs
{
    /// <summary>Creates the arguments for the tab <paramref name="container"/> showing <paramref name="item"/>.</summary>
    public TabClosingEventArgs(RoutedEvent routedEvent, object? item, Control container)
        : base(routedEvent)
    {
        Item = item;
        Container = container;
    }

    /// <summary>The item of the tab being closed.</summary>
    public object? Item { get; }

    /// <summary>The tab being closed (its container).</summary>
    public Control Container { get; }

    /// <summary>Keeps the tab when set.</summary>
    public bool Cancel { get; set; }
}
