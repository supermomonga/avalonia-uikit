using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Metadata;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's DescriptionList (description_list.rs): labels and values in rows
/// of <see cref="Columns"/>. Items fill a row up to the columns' spans; the
/// items of a row that is not full grow to share it. Bordered by default;
/// size classes: small (and xsmall), large.
/// </summary>
[TemplatePart("PART_Rows", typeof(StackPanel))]
public class DescriptionList : TemplatedControl
{
    /// <summary>Whether labels are beside (Horizontal, the default) or above their values.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DescriptionList, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>Whether the list draws borders and filled labels (GPUI's default).</summary>
    public static readonly StyledProperty<bool> IsBorderedProperty =
        AvaloniaProperty.Register<DescriptionList, bool>(nameof(IsBordered), true);

    /// <summary>The number of columns (1 to 10; GPUI's default is 3).</summary>
    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<DescriptionList, int>(nameof(Columns), 3, coerce: (_, v) => Math.Clamp(v, 1, 10));

    /// <summary>The width of a label beside its value (GPUI's default is 120).</summary>
    public static readonly StyledProperty<double> LabelWidthProperty =
        AvaloniaProperty.Register<DescriptionList, double>(nameof(LabelWidth), 120);

    /// <summary>The padding of labels and values, set by the size classes.</summary>
    public static readonly StyledProperty<Thickness> CellPaddingProperty =
        AvaloniaProperty.Register<DescriptionList, Thickness>(nameof(CellPadding), new Thickness(8, 4));

    /// <summary>The gap between rows without borders, set by the size classes.</summary>
    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<DescriptionList, double>(nameof(RowSpacing), 4);

    private StackPanel? _rows;

    static DescriptionList()
    {
        foreach (var p in new AvaloniaProperty[] { OrientationProperty, IsBorderedProperty, ColumnsProperty, LabelWidthProperty, CellPaddingProperty, RowSpacingProperty })
        {
            p.Changed.AddClassHandler<DescriptionList>((l, _) => l.Rebuild());
        }
    }

    /// <summary>Creates a description list.</summary>
    public DescriptionList() => Items.CollectionChanged += (_, _) => Rebuild();

    /// <summary>The items, in order; a <see cref="DescriptionSeparator"/> takes a whole row.</summary>
    [Content]
    public AvaloniaList<DescriptionItem> Items { get; } = [];

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <inheritdoc cref="IsBorderedProperty"/>
    public bool IsBordered { get => GetValue(IsBorderedProperty); set => SetValue(IsBorderedProperty, value); }

    /// <inheritdoc cref="ColumnsProperty"/>
    public int Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <inheritdoc cref="LabelWidthProperty"/>
    public double LabelWidth { get => GetValue(LabelWidthProperty); set => SetValue(LabelWidthProperty, value); }

    /// <inheritdoc cref="CellPaddingProperty"/>
    public Thickness CellPadding { get => GetValue(CellPaddingProperty); set => SetValue(CellPaddingProperty, value); }

    /// <inheritdoc cref="RowSpacingProperty"/>
    public double RowSpacing { get => GetValue(RowSpacingProperty); set => SetValue(RowSpacingProperty, value); }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _rows = e.NameScope.Find<StackPanel>("PART_Rows");
        Rebuild();
    }

    /// <summary>GPUI's group_item_rows: a new row when the next span would pass the columns.</summary>
    public static List<List<DescriptionItem>> GroupRows(IEnumerable<DescriptionItem> items, int columns)
    {
        var rows = new List<List<DescriptionItem>>();
        var current = 0;
        foreach (var item in items)
        {
            var span = item is DescriptionSeparator ? columns : item.Span;
            if (rows.Count == 0)
            {
                rows.Add([]);
            }
            if (current + span > columns)
            {
                rows.Add([]);
                current = 0;
            }
            rows[^1].Add(item);
            current += span;
        }
        rows.RemoveAll(r => r.Count == 0);
        return rows;
    }

    private void Rebuild()
    {
        if (_rows is null)
        {
            return;
        }
        foreach (var row in _rows.Children.OfType<Border>())
        {
            if (row.Child is Panel panel)
            {
                // Values that are controls go back to the items when rebuilt.
                foreach (var cell in panel.Children.OfType<DescriptionCell>())
                {
                    cell.Label = null;
                    cell.Value = null;
                }
            }
        }
        _rows.Children.Clear();
        _rows.Spacing = IsBordered ? 0 : RowSpacing;
        var rows = GroupRows(Items, Columns);
        for (var r = 0; r < rows.Count; r++)
        {
            var panel = new DescriptionRowPanel { Columns = Columns };
            for (var i = 0; i < rows[r].Count; i++)
            {
                var item = rows[r][i];
                Control cell = item is DescriptionSeparator
                    ? new DescriptionCell { IsSeparator = true }
                    : new DescriptionCell { Label = item.Label, Value = item.Value };
                var c = (DescriptionCell)cell;
                c.Span = item is DescriptionSeparator ? Columns : item.Span;
                c.IsFirstColumn = i == 0;
                c.Orientation = Orientation;
                c.IsBordered = IsBordered;
                c.LabelWidth = LabelWidth;
                c.CellPadding = IsBordered ? CellPadding : default;
                panel.Children.Add(cell);
            }
            _rows.Children.Add(new Border
            {
                Name = "PART_Row",
                Child = panel,
                BorderBrush = BorderBrush,
                // description_list.rs: a bordered row above another has a bottom border.
                BorderThickness = IsBordered && r < rows.Count - 1 ? new Thickness(0, 0, 0, 1) : default,
            });
        }
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BorderBrushProperty && _rows is not null)
        {
            foreach (var row in _rows.Children.OfType<Border>())
            {
                row.BorderBrush = BorderBrush;
            }
        }
    }
}

/// <summary>One label and value of a <see cref="DescriptionList"/>.</summary>
public class DescriptionItem : AvaloniaObject
{
    /// <summary>The label.</summary>
    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<DescriptionItem, object?>(nameof(Label));

    /// <summary>The value.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<DescriptionItem, object?>(nameof(Value));

    /// <summary>How many columns the item spans.</summary>
    public static readonly StyledProperty<int> SpanProperty =
        AvaloniaProperty.Register<DescriptionItem, int>(nameof(Span), 1);

    /// <inheritdoc cref="LabelProperty"/>
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <inheritdoc cref="ValueProperty"/>
    [Content]
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <inheritdoc cref="SpanProperty"/>
    public int Span { get => GetValue(SpanProperty); set => SetValue(SpanProperty, value); }
}

/// <summary>An 8px band across a whole row of a <see cref="DescriptionList"/>.</summary>
public class DescriptionSeparator : DescriptionItem
{
}

/// <summary>
/// The control a <see cref="DescriptionList"/> builds for an item, with the
/// :first (first column), :vertical, :bordered and :separator pseudo-classes.
/// </summary>
public class DescriptionCell : TemplatedControl
{
    /// <summary>The label.</summary>
    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<DescriptionCell, object?>(nameof(Label));

    /// <summary>The value.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<DescriptionCell, object?>(nameof(Value));

    /// <summary>The label's width beside its value.</summary>
    public static readonly StyledProperty<double> LabelWidthProperty =
        AvaloniaProperty.Register<DescriptionCell, double>(nameof(LabelWidth), 120);

    /// <summary>The padding of the label and the value.</summary>
    public static readonly StyledProperty<Thickness> CellPaddingProperty =
        AvaloniaProperty.Register<DescriptionCell, Thickness>(nameof(CellPadding));

    private bool _isFirstColumn;
    private bool _isSeparator;
    private bool _isBordered;
    private Orientation _orientation;

    /// <inheritdoc cref="LabelProperty"/>
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <inheritdoc cref="ValueProperty"/>
    public object? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <inheritdoc cref="LabelWidthProperty"/>
    public double LabelWidth { get => GetValue(LabelWidthProperty); set => SetValue(LabelWidthProperty, value); }

    /// <inheritdoc cref="CellPaddingProperty"/>
    public Thickness CellPadding { get => GetValue(CellPaddingProperty); set => SetValue(CellPaddingProperty, value); }

    /// <summary>How many columns the cell spans.</summary>
    public int Span { get; set; } = 1;

    /// <summary>Whether the cell starts its row.</summary>
    public bool IsFirstColumn
    {
        get => _isFirstColumn;
        set { _isFirstColumn = value; PseudoClasses.Set(":first", value); }
    }

    /// <summary>Whether the cell is a separator band.</summary>
    public bool IsSeparator
    {
        get => _isSeparator;
        set { _isSeparator = value; PseudoClasses.Set(":separator", value); }
    }

    /// <summary>Whether the list is bordered.</summary>
    public bool IsBordered
    {
        get => _isBordered;
        set { _isBordered = value; PseudoClasses.Set(":bordered", value); }
    }

    /// <summary>Whether the label is beside or above the value.</summary>
    public Orientation Orientation
    {
        get => _orientation;
        set { _orientation = value; PseudoClasses.Set(":vertical", value == Orientation.Vertical); }
    }
}

/// <summary>
/// A row of a <see cref="DescriptionList"/>: each cell starts at its span's
/// share of the width and the leftover is shared equally (flex-grow 1 over a
/// flex-basis of span / columns); cells stretch to the tallest.
/// </summary>
public class DescriptionRowPanel : Panel
{
    /// <summary>The list's number of columns.</summary>
    public int Columns { get; set; } = 3;

    private double[] Widths(double width)
    {
        var cells = Children.Select(c => c is DescriptionCell d ? d.Span : 1).ToArray();
        var basis = cells.Select(s => width * s / Columns).ToArray();
        var free = (width - basis.Sum()) / Math.Max(1, cells.Length);
        return basis.Select(b => b + free).ToArray();
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var widths = Widths(width);
        double height = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Measure(new Size(widths[i], double.PositiveInfinity));
            height = Math.Max(height, Children[i].DesiredSize.Height);
        }
        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var runs = LayoutSnap.Runs(this, 0, Widths(finalSize.Width));
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Arrange(new Rect(runs[i].Start, 0, runs[i].Length, finalSize.Height));
        }
        return finalSize;
    }
}
