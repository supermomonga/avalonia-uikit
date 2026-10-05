using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's ListItem (list/list_item.rs): a row of a <see cref="ListView"/>
/// or a <see cref="Tree"/>. A hovered row takes the list hover color, a
/// selected one the active color; a secondary-selected row (the right-clicked
/// one) gets a 1px frame in the selection color; a confirmed row shows its
/// <see cref="CheckIcon"/>. An active row (selected, secondary-selected or
/// confirmed) shows no hover, and a disabled row only muted text.
/// </summary>
[PseudoClasses(":selected", ":secondary-selected", ":confirmed")]
public class ListItem : ContentControl
{
    /// <summary>Whether the row is the selected one (GPUI's selected).</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<ListItem, bool>(nameof(IsSelected));

    /// <summary>Whether the row is the right-clicked one (GPUI's secondary_selected).</summary>
    public static readonly StyledProperty<bool> IsSecondarySelectedProperty =
        AvaloniaProperty.Register<ListItem, bool>(nameof(IsSecondarySelected));

    /// <summary>Whether the row is confirmed: it shows <see cref="CheckIcon"/> (GPUI's confirmed).</summary>
    public static readonly StyledProperty<bool> IsConfirmedProperty =
        AvaloniaProperty.Register<ListItem, bool>(nameof(IsConfirmed));

    /// <summary>
    /// The icon a confirmed row shows at its end, in a 20px column every row
    /// with an icon keeps (GPUI's check_icon). None by default.
    /// </summary>
    public static readonly StyledProperty<Geometry?> CheckIconProperty =
        AvaloniaProperty.Register<ListItem, Geometry?>(nameof(CheckIcon));

    /// <summary>Content after the row's content and check icon (GPUI's suffix).</summary>
    public static readonly StyledProperty<object?> SuffixProperty =
        AvaloniaProperty.Register<ListItem, object?>(nameof(Suffix));

    static ListItem()
    {
        IsSelectedProperty.Changed.AddClassHandler<ListItem>((x, e) => x.PseudoClasses.Set(":selected", e.GetNewValue<bool>()));
        IsSecondarySelectedProperty.Changed.AddClassHandler<ListItem>((x, e) => x.PseudoClasses.Set(":secondary-selected", e.GetNewValue<bool>()));
        IsConfirmedProperty.Changed.AddClassHandler<ListItem>((x, e) => x.PseudoClasses.Set(":confirmed", e.GetNewValue<bool>()));
    }

    /// <inheritdoc cref="IsSelectedProperty"/>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <inheritdoc cref="IsSecondarySelectedProperty"/>
    public bool IsSecondarySelected
    {
        get => GetValue(IsSecondarySelectedProperty);
        set => SetValue(IsSecondarySelectedProperty, value);
    }

    /// <inheritdoc cref="IsConfirmedProperty"/>
    public bool IsConfirmed
    {
        get => GetValue(IsConfirmedProperty);
        set => SetValue(IsConfirmedProperty, value);
    }

    /// <inheritdoc cref="CheckIconProperty"/>
    public Geometry? CheckIcon
    {
        get => GetValue(CheckIconProperty);
        set => SetValue(CheckIconProperty, value);
    }

    /// <inheritdoc cref="SuffixProperty"/>
    public object? Suffix
    {
        get => GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }
}
