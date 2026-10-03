using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Form (form/form.rs): <see cref="FormField"/>s in a grid of
/// <see cref="Columns"/>, labels above their inputs (Vertical, the default) or
/// beside them, and an optional right-aligned <see cref="Footer"/> spanning
/// all columns. Size classes (small, large) set the gaps. Validation and
/// values stay with the inputs (DataValidationErrors).
/// </summary>
public class Form : ItemsControl
{
    /// <summary>Where a field's label goes: above (Vertical) or beside its input.</summary>
    public static readonly AttachedProperty<Orientation> LabelOrientationProperty =
        AvaloniaProperty.RegisterAttached<Form, Control, Orientation>("LabelOrientation", Orientation.Vertical, inherits: true);

    /// <summary>The width of a label beside its input (GPUI's default is 140).</summary>
    public static readonly AttachedProperty<double> LabelWidthProperty =
        AvaloniaProperty.RegisterAttached<Form, Control, double>("LabelWidth", 140, inherits: true);

    /// <summary>A field's gap: 4, or 8 at large (form/field.rs); set by the size classes.</summary>
    public static readonly AttachedProperty<double> FieldGapProperty =
        AvaloniaProperty.RegisterAttached<Form, Control, double>("FieldGap", 4, inherits: true);

    /// <summary>The number of columns.</summary>
    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<Form, int>(nameof(Columns), 1, coerce: (_, v) => Math.Max(1, v));

    /// <summary>The gap between rows: 6, 8 or 12 by size; columns are three times as far apart.</summary>
    public static readonly StyledProperty<double> GapProperty =
        AvaloniaProperty.Register<Form, double>(nameof(Gap), 8);

    /// <summary>Content under the fields, at the end of the row (a submit button).</summary>
    public static readonly StyledProperty<object?> FooterProperty =
        AvaloniaProperty.Register<Form, object?>(nameof(Footer));

    /// <inheritdoc cref="LabelOrientationProperty"/>
    public Orientation LabelOrientation { get => GetValue(LabelOrientationProperty); set => SetValue(LabelOrientationProperty, value); }

    /// <inheritdoc cref="LabelWidthProperty"/>
    public double LabelWidth { get => GetValue(LabelWidthProperty); set => SetValue(LabelWidthProperty, value); }

    /// <inheritdoc cref="ColumnsProperty"/>
    public int Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <inheritdoc cref="GapProperty"/>
    public double Gap { get => GetValue(GapProperty); set => SetValue(GapProperty, value); }

    /// <inheritdoc cref="FooterProperty"/>
    public object? Footer { get => GetValue(FooterProperty); set => SetValue(FooterProperty, value); }

    /// <summary>Gets where a field's label goes.</summary>
    public static Orientation GetLabelOrientation(Control c) => c.GetValue(LabelOrientationProperty);

    /// <summary>Sets where a field's label goes.</summary>
    public static void SetLabelOrientation(Control c, Orientation value) => c.SetValue(LabelOrientationProperty, value);

    /// <summary>Gets the width of a label beside its input.</summary>
    public static double GetLabelWidth(Control c) => c.GetValue(LabelWidthProperty);

    /// <summary>Sets the width of a label beside its input.</summary>
    public static void SetLabelWidth(Control c, double value) => c.SetValue(LabelWidthProperty, value);

    /// <summary>Gets a field's gap.</summary>
    public static double GetFieldGap(Control c) => c.GetValue(FieldGapProperty);

    /// <summary>Sets a field's gap.</summary>
    public static void SetFieldGap(Control c, double value) => c.SetValue(FieldGapProperty, value);

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new FormField();

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<FormField>(item, out recycleKey);
}

/// <summary>
/// One field of a <see cref="Form"/> (form/field.rs): a medium-weight label
/// (with a red "*" when <see cref="IsRequired"/>), the input (the content), and
/// a muted description, laid out by the form's inherited values.
/// </summary>
public class FormField : ContentControl
{
    /// <summary>The label.</summary>
    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<FormField, object?>(nameof(Label));

    /// <summary>The help text under the input.</summary>
    public static readonly StyledProperty<object?> DescriptionProperty =
        AvaloniaProperty.Register<FormField, object?>(nameof(Description));

    /// <summary>Whether the label shows the required mark.</summary>
    public static readonly StyledProperty<bool> IsRequiredProperty =
        AvaloniaProperty.Register<FormField, bool>(nameof(IsRequired));

    /// <summary>How many of the form's columns the field spans.</summary>
    public static readonly StyledProperty<int> ColumnSpanProperty =
        AvaloniaProperty.Register<FormField, int>(nameof(ColumnSpan), 1);

    static FormField()
    {
        Form.LabelOrientationProperty.Changed.AddClassHandler<FormField>((f, _) => f.Update());
    }

    /// <summary>Creates a field.</summary>
    public FormField() => Update();

    /// <inheritdoc cref="LabelProperty"/>
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <inheritdoc cref="DescriptionProperty"/>
    public object? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <inheritdoc cref="IsRequiredProperty"/>
    public bool IsRequired { get => GetValue(IsRequiredProperty); set => SetValue(IsRequiredProperty, value); }

    /// <inheritdoc cref="ColumnSpanProperty"/>
    public int ColumnSpan { get => GetValue(ColumnSpanProperty); set => SetValue(ColumnSpanProperty, value); }

    /// <inheritdoc />
    protected override void OnAttachedToLogicalTree(Avalonia.LogicalTree.LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        Update();
    }

    private void Update() =>
        PseudoClasses.Set(":horizontal", GetValue(Form.LabelOrientationProperty) == Orientation.Horizontal);
}

/// <summary>
/// The grid of a <see cref="Form"/>: equal columns <see cref="ColumnSpacing"/>
/// apart, fields placed in order and spanning their <see cref="FormField.ColumnSpan"/>,
/// rows as tall as their tallest field, <see cref="RowSpacing"/> apart.
/// </summary>
public class FormPanel : Panel
{
    /// <summary>The number of columns.</summary>
    public static readonly StyledProperty<int> ColumnsProperty =
        AvaloniaProperty.Register<FormPanel, int>(nameof(Columns), 1);

    /// <summary>The gap between columns.</summary>
    public static readonly StyledProperty<double> ColumnSpacingProperty =
        AvaloniaProperty.Register<FormPanel, double>(nameof(ColumnSpacing));

    /// <summary>The gap between rows.</summary>
    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<FormPanel, double>(nameof(RowSpacing));

    static FormPanel()
    {
        AffectsMeasure<FormPanel>(ColumnsProperty, ColumnSpacingProperty, RowSpacingProperty);
    }

    /// <inheritdoc cref="ColumnsProperty"/>
    public int Columns { get => GetValue(ColumnsProperty); set => SetValue(ColumnsProperty, value); }

    /// <inheritdoc cref="ColumnSpacingProperty"/>
    public double ColumnSpacing { get => GetValue(ColumnSpacingProperty); set => SetValue(ColumnSpacingProperty, value); }

    /// <inheritdoc cref="RowSpacingProperty"/>
    public double RowSpacing { get => GetValue(RowSpacingProperty); set => SetValue(RowSpacingProperty, value); }

    // CSS grid auto-placement: a field that does not fit the rest of a row starts the next.
    private List<(Control Child, int Row, int Column, int Span)> Place()
    {
        var columns = Math.Max(1, Columns);
        var placed = new List<(Control, int, int, int)>();
        int row = 0, column = 0;
        foreach (var child in Children.Where(c => c.IsVisible))
        {
            var span = Math.Clamp(child is FormField f ? f.ColumnSpan : 1, 1, columns);
            if (column + span > columns)
            {
                row++;
                column = 0;
            }
            placed.Add((child, row, column, span));
            column += span;
        }
        return placed;
    }

    private double ColumnWidth(double width) => (width - ColumnSpacing * (Math.Max(1, Columns) - 1)) / Math.Max(1, Columns);

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width;
        var column = ColumnWidth(width);
        var heights = new Dictionary<int, double>();
        foreach (var (child, row, _, span) in Place())
        {
            child.Measure(new Size(column * span + ColumnSpacing * (span - 1), double.PositiveInfinity));
            heights[row] = Math.Max(heights.GetValueOrDefault(row), child.DesiredSize.Height);
        }
        var height = heights.Values.Sum() + RowSpacing * Math.Max(0, heights.Count - 1);
        return new Size(width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var column = ColumnWidth(finalSize.Width);
        var placed = Place();
        var heights = placed.GroupBy(p => p.Row).ToDictionary(g => g.Key, g => g.Max(p => p.Child.DesiredSize.Height));
        foreach (var (child, row, col, span) in placed)
        {
            var y = Enumerable.Range(0, row).Sum(r => heights[r] + RowSpacing);
            var x = LayoutSnap.Edge(this, col * (column + ColumnSpacing));
            var right = LayoutSnap.Edge(this, col * (column + ColumnSpacing) + column * span + ColumnSpacing * (span - 1));
            child.Arrange(new Rect(x, y, right - x, heights[row]));
        }
        return finalSize;
    }
}
