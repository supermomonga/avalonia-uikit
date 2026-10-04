using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Metadata;

namespace AvaloniaUIKit;

/// <summary>
/// A section of a <see cref="Select"/>'s items (GPUI Kit's SelectGroup,
/// searchable_list/vec.rs SearchableGroup): a muted header over its rows.
/// A query keeps the rows that match it, and the section while one row does
/// or its title matches.
/// </summary>
public class SelectGroup
{
    /// <summary>The section's header text.</summary>
    public string? Title { get; set; }

    /// <summary>The section's items.</summary>
    [Content]
    public AvaloniaList<object?> Items { get; } = [];
}

/// <summary>
/// The rows of an open <see cref="Select"/> (GPUI Kit's List in the dropdown):
/// a <see cref="SelectGroupHeader"/> for each section and a
/// <see cref="SelectListItem"/> for each item, virtualized. The select owns the
/// rows' state (cursor, check, enabled); the list only presents them.
/// </summary>
public class SelectList : ItemsControl
{
    private static readonly object RowKey = new();
    private static readonly object HeaderKey = new();
    private static readonly FuncTemplate<Panel?> DefaultPanel = new(() => new VirtualizingStackPanel());

    static SelectList()
    {
        ItemsPanelProperty.OverrideDefaultValue<SelectList>(DefaultPanel);
    }

    /// <summary>The select whose view this list shows.</summary>
    internal Select? Owner { get; set; }

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = Owner?.IsHeaderAt(index) == true ? HeaderKey : RowKey;
        return true;
    }

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        recycleKey == HeaderKey ? new SelectGroupHeader() : new SelectListItem();

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        if (container is SelectGroupHeader header)
        {
            header.Content = (item as SelectGroup)?.Title;
            return;
        }
        if (container is SelectListItem row && ItemTemplate is null && Owner is not null)
        {
            // Without a template, a row shows the item's text (SearchableListItem::title).
            row.Content = Owner.TextOf(item);
        }
        else
        {
            base.PrepareContainerForItemOverride(container, item, index);
        }
        if (container is SelectListItem prepared)
        {
            Owner?.PrepareRow(prepared, index);
        }
    }

    /// <inheritdoc />
    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is SelectGroupHeader header)
        {
            header.ClearValue(ContentControl.ContentProperty);
        }
    }

    /// <summary>Brings the realized rows up to date with the select's cursor and selection.</summary>
    internal void UpdateRows()
    {
        foreach (var container in GetRealizedContainers())
        {
            if (container is SelectListItem row && IndexFromContainer(row) is var index and >= 0)
            {
                Owner?.PrepareRow(row, index);
            }
        }
    }

    /// <summary>A click on a row (pressed and released on it with the left button).</summary>
    internal void RowClicked(SelectListItem row)
    {
        if (IndexFromContainer(row) is var index and >= 0)
        {
            Owner?.Commit(index);
        }
    }
}

/// <summary>
/// A row of a <see cref="SelectList"/> (searchable_list/item.rs
/// SearchableListItemElement). It wears the ComboBoxItem look: the check mark
/// while <see cref="ListBoxItem.IsSelected"/> (the item is in the selection),
/// the accent at 70% under the pointer, muted when disabled, and the accent
/// on the keyboard cursor's row (<c>:cursor</c>, GPUI's "selected"). Rows do
/// not take focus: the search field keeps it while the popup is open.
/// </summary>
[PseudoClasses(":cursor")]
public class SelectListItem : ComboBoxItem
{
    private bool _isCursor;
    private bool _pressed;

    static SelectListItem()
    {
        FocusableProperty.OverrideDefaultValue<SelectListItem>(false);
    }

    /// <summary>Whether the row is under the list's keyboard cursor.</summary>
    public bool IsCursor
    {
        get => _isCursor;
        internal set
        {
            _isCursor = value;
            PseudoClasses.Set(":cursor", value);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pressed = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed;
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_pressed && e.InitialPressMouseButton == MouseButton.Left &&
            new Rect(Bounds.Size).Contains(e.GetPosition(this)) &&
            ItemsControl.ItemsControlFromItemContainer(this) is SelectList list)
        {
            list.RowClicked(this);
            e.Handled = true;
        }
        _pressed = false;
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pressed = false;
    }
}

/// <summary>
/// A section header of a <see cref="SelectList"/> (searchable_list/adapter.rs
/// render_section_header): the group's title in muted text_sm, padded as a row.
/// </summary>
public class SelectGroupHeader : ContentControl
{
}
