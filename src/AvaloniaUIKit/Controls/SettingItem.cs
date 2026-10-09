using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's SettingItem (crates/component/src/setting/item.rs): a row in a
/// <see cref="SettingGroup"/> with a <see cref="Title"/> (14px) and a
/// <see cref="Description"/> (14px, muted) beside its content, the field: a
/// ToggleSwitch, CheckBox, TextBox, NumericUpDown, DropDownButton, ComboBox or
/// any control. Horizontally (the default <see cref="Orientation"/>) the text
/// takes up to 60% of the row and the field sits at its end, centered;
/// vertically the field is under the text, the width of the row. Every item
/// of a page narrower than 480px is vertical. The theme gives a TextBox 256px
/// and a NumericUpDown 128px in a horizontal row, as GPUI's fields. An item
/// without a title is a custom item: its content fills the row.
/// <para>
/// With a <see cref="DefaultValue"/>, the item follows the value of a
/// ToggleButton (ToggleSwitch, CheckBox), TextBox, NumericUpDown, RangeBase
/// (Slider), SelectingItemsControl (ComboBox, ListBox) or
/// <see cref="Select"/> content: <see cref="IsModified"/> says whether it
/// differs from the default, and a reset sets the default back. For other
/// content, bind <see cref="IsModified"/> and handle <see cref="Reset"/> or
/// <see cref="ResetCommand"/> (GPUI's <c>on_reset</c>). A disabled item is
/// dimmed to 50% and its content disabled.
/// </para>
/// <para>
/// The search matches the title, a string or TextBlock description and the
/// comma-separated <see cref="Keywords"/>; an item without a title matches
/// only its keywords.
/// </para>
/// </summary>
[TemplatePart("PART_Layout", typeof(SettingItemPanel))]
[PseudoClasses(":horizontal", ":vertical", ":custom", ":unmatched", ":modified")]
public class SettingItem : ContentControl
{
    /// <summary>The item's title (GPUI's <c>SettingItem::new(title, …)</c>); without one the content fills the row.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<SettingItem, string?>(nameof(Title));

    /// <summary>Muted text under the title (GPUI's <c>description</c>): a string, or inlines in a TextBlock.</summary>
    public static readonly StyledProperty<object?> DescriptionProperty =
        AvaloniaProperty.Register<SettingItem, object?>(nameof(Description));

    /// <summary>More words the search matches (GPUI's <c>keywords</c>), separated by commas; never shown.</summary>
    public static readonly StyledProperty<string?> KeywordsProperty =
        AvaloniaProperty.Register<SettingItem, string?>(nameof(Keywords));

    /// <summary>Whether the field is beside the text (Horizontal, the default) or under it (GPUI's <c>layout</c>).</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<SettingItem, Orientation>(nameof(Orientation));

    /// <summary>The field's default value (GPUI's <c>default_value</c>), for the field types the item follows.</summary>
    public static readonly StyledProperty<object?> DefaultValueProperty =
        AvaloniaProperty.Register<SettingItem, object?>(nameof(DefaultValue));

    /// <summary>
    /// Whether the field differs from its default: the page shows its reset
    /// button while one of its items is. Kept by the item for a field it
    /// follows; bind it for other content.
    /// </summary>
    public static readonly StyledProperty<bool> IsModifiedProperty =
        AvaloniaProperty.Register<SettingItem, bool>(nameof(IsModified));

    /// <summary>A command the page's reset runs for the item (GPUI's <c>on_reset</c>).</summary>
    public static readonly StyledProperty<ICommand?> ResetCommandProperty =
        AvaloniaProperty.Register<SettingItem, ICommand?>(nameof(ResetCommand));

    /// <summary>Raised when the page's reset button resets the item, after its field took its default.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ResetEvent =
        RoutedEvent.Register<SettingItem, RoutedEventArgs>(nameof(Reset), RoutingStrategies.Bubble);

    private Control? _field;

    static SettingItem()
    {
        TitleProperty.Changed.AddClassHandler<SettingItem>((x, _) =>
        {
            x.UpdateLayoutState();
            x.Changed();
        });
        DescriptionProperty.Changed.AddClassHandler<SettingItem>((x, _) => x.Changed());
        KeywordsProperty.Changed.AddClassHandler<SettingItem>((x, _) => x.Changed());
        OrientationProperty.Changed.AddClassHandler<SettingItem>((x, _) => x.UpdateLayoutState());
        SettingPage.IsStackedProperty.Changed.AddClassHandler<SettingItem>((x, _) => x.UpdateLayoutState());
        ContentProperty.Changed.AddClassHandler<SettingItem>((x, _) => x.WatchField());
        DefaultValueProperty.Changed.AddClassHandler<SettingItem>((x, _) => x.UpdateModified());
        IsModifiedProperty.Changed.AddClassHandler<SettingItem>((x, e) =>
        {
            x.PseudoClasses.Set(":modified", e.GetNewValue<bool>());
            (x.Parent?.Parent as SettingPage)?.UpdateResettable();
        });
    }

    /// <summary>Creates an item.</summary>
    public SettingItem() => UpdateLayoutState();

    /// <inheritdoc cref="TitleProperty"/>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <inheritdoc cref="DescriptionProperty"/>
    public object? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <inheritdoc cref="KeywordsProperty"/>
    public string? Keywords
    {
        get => GetValue(KeywordsProperty);
        set => SetValue(KeywordsProperty, value);
    }

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <inheritdoc cref="DefaultValueProperty"/>
    public object? DefaultValue
    {
        get => GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }

    /// <inheritdoc cref="IsModifiedProperty"/>
    public bool IsModified
    {
        get => GetValue(IsModifiedProperty);
        set => SetValue(IsModifiedProperty, value);
    }

    /// <inheritdoc cref="ResetCommandProperty"/>
    public ICommand? ResetCommand
    {
        get => GetValue(ResetCommandProperty);
        set => SetValue(ResetCommandProperty, value);
    }

    /// <inheritdoc cref="ResetEvent"/>
    public event EventHandler<RoutedEventArgs>? Reset
    {
        add => AddHandler(ResetEvent, value);
        remove => RemoveHandler(ResetEvent, value);
    }

    /// <summary>
    /// Whether the search keeps the item (item.rs is_match): its title, a
    /// string or TextBlock description or a keyword contains
    /// <paramref name="query"/>, ignoring case; without a title, only a
    /// keyword, or an empty query.
    /// </summary>
    public bool Matches(string query)
    {
        var q = query.ToLowerInvariant();
        var keywords = (Keywords ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (keywords.Any(k => k.ToLowerInvariant().Contains(q, StringComparison.Ordinal)))
        {
            return true;
        }
        if (Title is not { } title)
        {
            return q.Length == 0;
        }
        var description = Description switch
        {
            string s => s,
            TextBlock block => block.Inlines is { Count: > 0 } inlines ? inlines.Text : block.Text,
            _ => null,
        };
        return title.ToLowerInvariant().Contains(q, StringComparison.Ordinal)
            || (description?.ToLowerInvariant().Contains(q, StringComparison.Ordinal) ?? false);
    }

    /// <summary>Marks the item as kept or hidden by <paramref name="query"/> and says which.</summary>
    internal bool Filter(string query)
    {
        var match = Matches(query);
        PseudoClasses.Set(":unmatched", !match);
        return match;
    }

    /// <summary>
    /// Sets a followed field back to <see cref="DefaultValue"/>, then raises
    /// <see cref="Reset"/> and runs <see cref="ResetCommand"/> (item.rs reset).
    /// </summary>
    internal void ResetValue()
    {
        if (IsSet(DefaultValueProperty) && _field is not null)
        {
            SettingFields.Reset(_field, DefaultValue);
        }
        RaiseEvent(new RoutedEventArgs(ResetEvent));
        if (ResetCommand is { } command && command.CanExecute(null))
        {
            command.Execute(null);
        }
    }

    /// <summary>Gives the content the settings' size classes.</summary>
    internal void PassClasses()
    {
        if (_field is not null)
        {
            GroupClasses.Apply(_field, Settings.Of(this)?.FieldClasses() ?? []);
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        PassClasses();
    }

    private void Changed() => (Parent as SettingGroup)?.Changed();

    // item.rs render_item: a page's narrow layout makes every item vertical.
    private void UpdateLayoutState()
    {
        var vertical = Orientation == Orientation.Vertical || GetValue(SettingPage.IsStackedProperty);
        PseudoClasses.Set(":horizontal", !vertical);
        PseudoClasses.Set(":vertical", vertical);
        PseudoClasses.Set(":custom", Title is null);
    }

    private void WatchField()
    {
        if (_field is not null)
        {
            _field.PropertyChanged -= OnFieldPropertyChanged;
            GroupClasses.Clear(_field);
        }
        _field = Content as Control;
        if (_field is not null)
        {
            _field.PropertyChanged += OnFieldPropertyChanged;
            PassClasses();
        }
        UpdateModified();
    }

    private void OnFieldPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_field is not null && e.Property == SettingFields.ValueProperty(_field))
        {
            UpdateModified();
        }
    }

    // fields/mod.rs is_resettable: a followed field is modified while its value is not the default.
    private void UpdateModified()
    {
        if (IsSet(DefaultValueProperty) && _field is not null && SettingFields.ValueProperty(_field) is not null)
        {
            SetCurrentValue(IsModifiedProperty, !SettingFields.HasValue(_field, DefaultValue));
        }
    }
}

/// <summary>
/// The value a <see cref="SettingItem"/> follows in the field types it knows,
/// read, compared with the default and set without reflection.
/// </summary>
internal static class SettingFields
{
    /// <summary>The property that holds the field's value, or null for a field the item does not follow.</summary>
    public static AvaloniaProperty? ValueProperty(Control field) => field switch
    {
        ToggleButton => ToggleButton.IsCheckedProperty,
        TextBox => TextBox.TextProperty,
        NumericUpDown => NumericUpDown.ValueProperty,
        RangeBase => RangeBase.ValueProperty,
        SelectingItemsControl => SelectingItemsControl.SelectedValueProperty,
        Select => Select.SelectedItemProperty,
        _ => null,
    };

    /// <summary>Whether the field's value is <paramref name="value"/> (a value of its type, or text that converts to one).</summary>
    public static bool HasValue(Control field, object? value) => field switch
    {
        ToggleButton toggle => toggle.IsChecked == ToBool(value),
        TextBox box => (box.Text ?? "") == (ToText(value) ?? ""),
        NumericUpDown number => number.Value == ToDecimal(value),
        RangeBase range => ToDouble(value) is { } d && range.Value.Equals(d),
        SelectingItemsControl selecting => ItemEquals(selecting.SelectedValue, value),
        Select select => ItemEquals(select.SelectedItem, value),
        _ => true,
    };

    /// <summary>Sets the field's value to <paramref name="value"/>.</summary>
    public static void Reset(Control field, object? value)
    {
        switch (field)
        {
            case ToggleButton toggle:
                toggle.SetCurrentValue(ToggleButton.IsCheckedProperty, ToBool(value));
                break;
            case TextBox box:
                box.SetCurrentValue(TextBox.TextProperty, ToText(value));
                break;
            case NumericUpDown number:
                number.SetCurrentValue(NumericUpDown.ValueProperty, ToDecimal(value));
                break;
            case RangeBase range when ToDouble(value) is { } d:
                range.SetCurrentValue(RangeBase.ValueProperty, d);
                break;
            case SelectingItemsControl selecting when selecting.SelectedValueBinding is not null:
                selecting.SetCurrentValue(SelectingItemsControl.SelectedValueProperty, value);
                break;
            case SelectingItemsControl selecting:
                selecting.SetCurrentValue(SelectingItemsControl.SelectedItemProperty, Find(selecting.Items, value));
                break;
            case Select select:
                select.SelectedItem = Find(select.Items, value);
                break;
        }
    }

    // An item is the value itself, or its text: a string, or a ComboBoxItem's content.
    private static bool ItemEquals(object? item, object? value) =>
        Equals(item, value) || (value is string text && ToText(item is ContentControl c ? c.Content : item) == text);

    private static object? Find(ItemCollection items, object? value) => items.Cast<object?>().FirstOrDefault(i => ItemEquals(i, value));

    private static bool? ToBool(object? value) => value switch
    {
        bool b => b,
        string s when bool.TryParse(s, out var b) => b,
        _ => null,
    };

    private static string? ToText(object? value) => value switch
    {
        null => null,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private static decimal? ToDecimal(object? value) => value switch
    {
        decimal d => d,
        string s when decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
        IConvertible c => c.ToDecimal(CultureInfo.InvariantCulture),
        _ => null,
    };

    private static double? ToDouble(object? value) => value switch
    {
        double d => d,
        string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
        IConvertible c => c.ToDouble(CultureInfo.InvariantCulture),
        _ => null,
    };
}

/// <summary>
/// The layout of a <see cref="SettingItem"/> (item.rs render_item): its text
/// and its field. Horizontally the text takes the room the field leaves, up to
/// 60% of the row (<c>flex_1().max_w_3_5()</c>), and the field sits at the
/// row's end, both centered (<c>justify_between().items_center()</c>);
/// vertically they stack. <see cref="Spacing"/> is between them. Without the
/// text (a custom item) the field fills the row.
/// </summary>
public class SettingItemPanel : Panel
{
    /// <summary>Whether the field is beside the text or under it.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<SettingItemPanel, Orientation>(nameof(Orientation));

    /// <summary>The room between the text and the field (GPUI's <c>gap_3</c>, 12).</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        StackPanel.SpacingProperty.AddOwner<SettingItemPanel>();

    // item.rs max_w_3_5.
    private const double TextShare = 0.6;

    static SettingItemPanel() => AffectsMeasure<SettingItemPanel>(OrientationProperty, SpacingProperty);

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <inheritdoc cref="SpacingProperty"/>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var (text, field) = Parts();
        if (text is null || field is null)
        {
            var only = text ?? field;
            only?.Measure(availableSize);
            return only?.DesiredSize ?? default;
        }
        if (Orientation == Orientation.Vertical)
        {
            text.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            field.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            return new Size(
                Math.Max(text.DesiredSize.Width, field.DesiredSize.Width),
                text.DesiredSize.Height + Spacing + field.DesiredSize.Height);
        }
        field.Measure(new Size(availableSize.Width, availableSize.Height));
        var width = TextWidth(availableSize.Width, field.DesiredSize.Width);
        text.Measure(new Size(width, availableSize.Height));
        var desiredWidth = double.IsInfinity(availableSize.Width)
            ? text.DesiredSize.Width + Spacing + field.DesiredSize.Width
            : availableSize.Width;
        return new Size(desiredWidth, Math.Max(text.DesiredSize.Height, field.DesiredSize.Height));
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (text, field) = Parts();
        if (text is null || field is null)
        {
            (text ?? field)?.Arrange(new Rect(finalSize));
            return finalSize;
        }
        if (Orientation == Orientation.Vertical)
        {
            text.Arrange(new Rect(0, 0, finalSize.Width, text.DesiredSize.Height));
            field.Arrange(new Rect(0, text.DesiredSize.Height + Spacing, finalSize.Width, field.DesiredSize.Height));
            return finalSize;
        }
        var fieldSize = field.DesiredSize;
        var width = TextWidth(finalSize.Width, fieldSize.Width);
        var textHeight = text.DesiredSize.Height;
        text.Arrange(new Rect(0, (finalSize.Height - textHeight) / 2, width, textHeight));
        field.Arrange(new Rect(finalSize.Width - fieldSize.Width, (finalSize.Height - fieldSize.Height) / 2, fieldSize.Width, fieldSize.Height));
        return finalSize;
    }

    // flex_1 with max_w_3_5: the room left beside the field and the gap, at most 60% of the row.
    private double TextWidth(double rowWidth, double fieldWidth) =>
        double.IsInfinity(rowWidth)
            ? double.PositiveInfinity
            : Math.Max(0, Math.Min(rowWidth - fieldWidth - Spacing, rowWidth * TextShare));

    // The text and the field: the first two children, each only while visible.
    private (Control? Text, Control? Field) Parts()
    {
        var text = Children.Count > 0 && Children[0].IsVisible ? Children[0] : null;
        var field = Children.Count > 1 && Children[1].IsVisible ? Children[1] : null;
        return (text, field);
    }
}
