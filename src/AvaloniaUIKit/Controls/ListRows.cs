using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI's ScrollStrategy (gpui elements/uniform_list.rs): where scrolling to
/// a row puts it in the viewport.
/// </summary>
public enum ScrollStrategy
{
    /// <summary>At the top of the viewport.</summary>
    Top,
    /// <summary>In the middle of the viewport, as far as the content allows.</summary>
    Center,
    /// <summary>At the bottom of the viewport, as far as the content allows.</summary>
    Bottom,
    /// <summary>At the nearer edge, and only when the row is out of view.</summary>
    Nearest,
}

/// <summary>How a scroll request reads its strategy.</summary>
internal enum RowScrollMode
{
    /// <summary>
    /// GPUI's virtual list (base/virtual_list.rs, List): Center always
    /// centers; every other strategy scrolls a row out of view to the nearer edge.
    /// </summary>
    VirtualList,

    /// <summary>
    /// GPUI's uniform list (uniform_list.rs, Tree): a row in full view stays;
    /// one out of view goes to the strategy's place (Nearest: the nearer edge).
    /// </summary>
    UniformList,

    /// <summary>The strategy's place, even for a row in view.</summary>
    Strict,
}

/// <summary>Builds and fills the containers of a <see cref="ListRows"/>.</summary>
internal interface IRowsOwner
{
    object RecycleKey(object? row);

    Control CreateRow(object recycleKey);

    void PrepareRow(Control container, object? row, int index);

    void ClearRow(Control container);
}

/// <summary>
/// The virtualized rows of a <see cref="ListView"/> or a <see cref="Tree"/>:
/// the owner's flattened rows (GPUI's RowsCache entries, TreeState entries) in
/// a VirtualizingStackPanel inside the scroll viewer. The owner makes and
/// fills the containers; this keeps GPUI's scroll_to_item rules.
/// </summary>
[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
internal sealed class ListRows : ItemsControl
{
    private (int Index, ScrollStrategy Strategy, RowScrollMode Mode)? _pendingScroll;
    private bool _waitingForLayout;

    /// <summary>The control whose rows these are.</summary>
    public IRowsOwner? Owner { get; set; }

    /// <summary>The template's scroll viewer.</summary>
    public ScrollViewer? Viewer { get; private set; }

    /// <summary>Raised after a layout pass and after a scroll: the rows in view may have changed.</summary>
    public event EventHandler? RowsInViewChanged;

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = Owner?.RecycleKey(item) ?? DefaultRecycleKey;
        return true;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        Owner?.CreateRow(recycleKey ?? DefaultRecycleKey) ?? new ContentPresenter();

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (Owner is { } owner)
        {
            owner.PrepareRow(container, item, index);
        }
        else
        {
            base.PrepareContainerForItemOverride(container, item, index);
        }
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        Owner?.ClearRow(container);
        base.ClearContainerForItemOverride(container);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (Viewer is not null)
        {
            Viewer.ScrollChanged -= OnScrollChanged;
        }
        base.OnApplyTemplate(e);
        Viewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        if (Viewer is not null)
        {
            Viewer.ScrollChanged += OnScrollChanged;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LayoutUpdated += OnLayoutUpdated;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= OnLayoutUpdated;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => RowsInViewChanged?.Invoke(this, EventArgs.Empty);

    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (_pendingScroll is not null && CanScroll())
        {
            ApplyScroll();
        }
        RowsInViewChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Scrolls row <paramref name="index"/> into view by <paramref name="strategy"/>;
    /// before the first layout, when the rows are laid out (GPUI defers it to the next frame).
    /// </summary>
    public void ScrollToRow(int index, ScrollStrategy strategy, RowScrollMode mode)
    {
        _pendingScroll = (index, strategy, mode);
        if (CanScroll())
        {
            ApplyScroll();
        }
        else if (!_waitingForLayout)
        {
            _waitingForLayout = true;
            InvalidateMeasure();
        }
    }

    /// <summary>One past the last row in view (GPUI's visible range end), or 0.</summary>
    public int VisibleEnd()
    {
        if (Viewer is not { } viewer)
        {
            return 0;
        }
        var bottom = viewer.Offset.Y + viewer.Viewport.Height;
        var end = 0;
        foreach (var container in GetRealizedContainers())
        {
            var index = IndexFromContainer(container);
            if (index >= 0 && ItemsScrolling.RowSpan(viewer, container) is { } span && span.Top < bottom)
            {
                end = Math.Max(end, index + 1);
            }
        }
        return end;
    }

    private bool CanScroll() => ItemsScrolling.CanScroll(this, Viewer);

    private void ApplyScroll()
    {
        if (_pendingScroll is not { } request || Viewer is not { } viewer)
        {
            return;
        }
        _pendingScroll = null;
        _waitingForLayout = false;
        ItemsScrolling.Scroll(this, viewer, request.Index, request.Strategy, request.Mode);
    }
}
