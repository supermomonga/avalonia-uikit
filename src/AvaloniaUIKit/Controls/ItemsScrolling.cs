using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI's scroll_to_item for Avalonia's lists: a <see cref="ListBox"/> or
/// any <see cref="ItemsControl"/> in a scroll viewer, virtualized or not, as
/// GPUI's VirtualList (base/virtual_list.rs). <see cref="ListView"/> and
/// <see cref="Tree"/> scroll their rows the same way.
/// </summary>
public static class ItemsScrolling
{
    private static readonly ConditionalWeakTable<ItemsControl, PendingScroll> s_pending = new();

    /// <summary>
    /// Scrolls item <paramref name="index"/> into view as GPUI's
    /// VirtualListScrollHandle::scroll_to_item does: <see cref="ScrollStrategy.Center"/>
    /// puts it in the middle of the viewport (as far as the content allows); any
    /// other strategy scrolls an item out of view to the nearer edge and leaves
    /// one in view where it is. An item that is not realized yet is realized
    /// first. Before the first layout, the scroll waits for one (GPUI defers it
    /// to the next frame); a later call replaces a waiting one.
    /// </summary>
    public static void ScrollToItem(this ItemsControl items, int index, ScrollStrategy strategy = ScrollStrategy.Top)
    {
        ArgumentNullException.ThrowIfNull(items);
        var request = s_pending.GetValue(items, owner => new PendingScroll(owner));
        request.Set(index, strategy);
    }

    /// <summary>The scroll viewer of <paramref name="items"/>' presenter, if it scrolls.</summary>
    internal static ScrollViewer? ViewerOf(ItemsControl items) =>
        items.Presenter?.GetVisualAncestors().TakeWhile(v => v != items).OfType<ScrollViewer>().FirstOrDefault();

    /// <summary>Whether <paramref name="items"/> is laid out enough to scroll to an item.</summary>
    internal static bool CanScroll(ItemsControl items, ScrollViewer? viewer) =>
        viewer is not null && viewer.IsArrangeValid && viewer.Viewport.Height > 0 && items.Presenter?.Panel is not null;

    /// <summary>
    /// Scrolls item <paramref name="index"/> of <paramref name="items"/> by
    /// <paramref name="strategy"/>, read by <paramref name="mode"/>.
    /// </summary>
    internal static void Scroll(ItemsControl items, ScrollViewer viewer, int index, ScrollStrategy strategy, RowScrollMode mode)
    {
        if (index < 0 || index >= items.ItemCount)
        {
            return;
        }
        var height = viewer.Viewport.Height;
        var top = viewer.Offset.Y;
        bool above, below;
        if (items.ContainerFromIndex(index) is { } shown && RowSpan(viewer, shown) is { } span)
        {
            // uniform_list.rs: is the row above or below the rows in view.
            above = span.Top < top - 0.01;
            below = span.Bottom > top + height + 0.01;
        }
        else
        {
            var first = items.GetRealizedContainers().Select(items.IndexFromContainer).Where(i => i >= 0).DefaultIfEmpty(-1).Min();
            above = first < 0 || index < first;
            below = !above;
        }
        var place = strategy;
        switch (mode)
        {
            case RowScrollMode.VirtualList when strategy != ScrollStrategy.Center:
                // virtual_list.rs: anything but Center keeps a row in view and
                // moves one out of view to the nearer edge.
                if (!above && !below)
                {
                    return;
                }
                place = above ? ScrollStrategy.Top : ScrollStrategy.Bottom;
                break;
            case RowScrollMode.UniformList:
                // uniform_list.rs (non-strict): a row in full view stays.
                if (!above && !below)
                {
                    return;
                }
                if (strategy == ScrollStrategy.Nearest)
                {
                    place = above ? ScrollStrategy.Top : ScrollStrategy.Bottom;
                }
                break;
            case RowScrollMode.Strict when strategy == ScrollStrategy.Nearest:
                if (!above && !below)
                {
                    return;
                }
                place = above ? ScrollStrategy.Top : ScrollStrategy.Bottom;
                break;
        }

        // Realizes the row at its place in the panel, then puts it where the strategy says.
        items.ScrollIntoView(index);
        if (items.ContainerFromIndex(index) is not { } container || RowSpan(viewer, container) is not { } row)
        {
            return;
        }
        var target = place switch
        {
            ScrollStrategy.Top => row.Top,
            ScrollStrategy.Center => row.Top + row.Height / 2 - height / 2,
            _ => row.Bottom - height,
        };
        var max = Math.Max(0, viewer.Extent.Height - height);
        viewer.Offset = new Vector(viewer.Offset.X, Math.Clamp(target, 0, max));
    }

    /// <summary>
    /// A container's box in the space of the scroll viewer's offset: its place
    /// in the scrolled content (which the scroll moves as a whole, so a scroll
    /// not laid out yet does not move it) below the content's margin and the
    /// presenter's padding.
    /// </summary>
    internal static Rect? RowSpan(ScrollViewer viewer, Control container)
    {
        if (viewer.Presenter is not { Child: { } content } presenter || container.TranslatePoint(default, content) is not { } origin)
        {
            return null;
        }
        var top = origin.Y + content.Margin.Top + presenter.Padding.Top;
        return new Rect(0, top, container.Bounds.Width, container.Bounds.Height);
    }

    /// <summary>A scroll that waits for <see cref="Owner"/>'s layout.</summary>
    private sealed class PendingScroll(ItemsControl owner)
    {
        private (int Index, ScrollStrategy Strategy)? _request;
        private bool _waiting;

        public ItemsControl Owner { get; } = owner;

        public void Set(int index, ScrollStrategy strategy)
        {
            _request = (index, strategy);
            if (!TryApply() && !_waiting)
            {
                _waiting = true;
                Owner.LayoutUpdated += OnLayoutUpdated;
                Owner.InvalidateMeasure();
            }
        }

        private void OnLayoutUpdated(object? sender, EventArgs e)
        {
            if (TryApply())
            {
                _waiting = false;
                Owner.LayoutUpdated -= OnLayoutUpdated;
            }
        }

        private bool TryApply()
        {
            var viewer = ViewerOf(Owner);
            if (_request is not { } request || !CanScroll(Owner, viewer))
            {
                return _request is null;
            }
            _request = null;
            Scroll(Owner, viewer!, request.Index, request.Strategy, RowScrollMode.VirtualList);
            return true;
        }
    }
}
