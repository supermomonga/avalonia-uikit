using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Stepper (stepper): numbered (or icon) indicators joined by
/// lines. Steps up to <see cref="SelectedIndex"/> are checked and the lines
/// after passed steps are primary. A click on a step's indicator or label
/// selects it, unless the stepper is disabled. Size classes: xsmall (8px
/// dots), small (18px), the default (24px), large (32px).
/// </summary>
public class Stepper : ItemsControl
{
    /// <summary>The checked step.</summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<Stepper, int>(nameof(SelectedIndex), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Steps in a row (the default) or a column.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<Stepper, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>Whether steps share the width equally with their labels centered (GPUI's text_center).</summary>
    public static readonly StyledProperty<bool> CentersStepsProperty =
        AvaloniaProperty.Register<Stepper, bool>(nameof(CentersSteps));

    static Stepper()
    {
        SelectedIndexProperty.Changed.AddClassHandler<Stepper>((s, _) => s.Refresh());
        OrientationProperty.Changed.AddClassHandler<Stepper>((s, _) => s.Refresh());
        CentersStepsProperty.Changed.AddClassHandler<Stepper>((s, _) => s.Refresh());
    }

    private static readonly string[] SizeClasses = ["xsmall", "small", "large"];

    /// <summary>Creates a stepper.</summary>
    public Stepper()
    {
        Items.CollectionChanged += (_, _) => Refresh();
        Classes.CollectionChanged += (_, _) => Refresh();
    }

    /// <inheritdoc cref="SelectedIndexProperty"/>
    public int SelectedIndex { get => GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <inheritdoc cref="CentersStepsProperty"/>
    public bool CentersSteps { get => GetValue(CentersStepsProperty); set => SetValue(CentersStepsProperty, value); }

    /// <inheritdoc />
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new StepperItem();

    /// <inheritdoc />
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<StepperItem>(item, out recycleKey);

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is StepperItem step)
        {
            Update(step, index);
        }
    }

    internal void Select(StepperItem step)
    {
        var index = IndexFromContainer(step);
        if (index >= 0 && IsEffectivelyEnabled)
        {
            SetCurrentValue(SelectedIndexProperty, index);
        }
    }

    private void Refresh()
    {
        for (var i = 0; i < ItemCount; i++)
        {
            if (ContainerFromIndex(i) is StepperItem step)
            {
                Update(step, i);
            }
        }
    }

    private void Update(StepperItem step, int index)
    {
        // The stepper's size is every step's (stepper.rs with_size).
        foreach (var size in SizeClasses)
        {
            step.Classes.Set(size, Classes.Contains(size));
        }
        step.Number = index + 1;
        step.SetState(
            isChecked: index <= SelectedIndex,
            isPassed: index < SelectedIndex,
            isLast: index == ItemCount - 1,
            vertical: Orientation == Orientation.Vertical,
            centered: CentersSteps);
    }
}

/// <summary>
/// One step of a <see cref="Stepper"/>: the content is its label. Pseudo-classes
/// :checked, :passed (the line after it is primary), :last, :vertical and
/// :centered.
/// </summary>
[Avalonia.Controls.Metadata.TemplatePart("PART_Trigger", typeof(Control))]
public class StepperItem : ContentControl
{
    /// <summary>An icon in the indicator instead of the number.</summary>
    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<StepperItem, Geometry?>(nameof(Icon));

    /// <summary>The step's number, from 1.</summary>
    public static readonly DirectProperty<StepperItem, int> NumberProperty =
        AvaloniaProperty.RegisterDirect<StepperItem, int>(nameof(Number), s => s.Number);

    /// <summary>The indicator's size, set by the size classes.</summary>
    public static readonly StyledProperty<double> IndicatorSizeProperty =
        AvaloniaProperty.Register<StepperItem, double>(nameof(IndicatorSize), 24);

    /// <summary>Whether the stepper runs down (set by the stepper).</summary>
    public static readonly DirectProperty<StepperItem, bool> IsVerticalProperty =
        AvaloniaProperty.RegisterDirect<StepperItem, bool>(nameof(IsVertical), s => s.IsVertical);

    /// <summary>Whether steps are centered in equal shares (set by the stepper).</summary>
    public static readonly DirectProperty<StepperItem, bool> IsCenteredProperty =
        AvaloniaProperty.RegisterDirect<StepperItem, bool>(nameof(IsCentered), s => s.IsCentered);

    private int _number = 1;
    private bool _isVertical;
    private bool _isCentered;
    private Control? _trigger;
    private Control? _indicator;

    /// <inheritdoc cref="IconProperty"/>
    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <inheritdoc cref="NumberProperty"/>
    public int Number { get => _number; internal set => SetAndRaise(NumberProperty, ref _number, value); }

    /// <inheritdoc cref="IndicatorSizeProperty"/>
    public double IndicatorSize { get => GetValue(IndicatorSizeProperty); set => SetValue(IndicatorSizeProperty, value); }

    /// <inheritdoc cref="IsVerticalProperty"/>
    public bool IsVertical { get => _isVertical; private set => SetAndRaise(IsVerticalProperty, ref _isVertical, value); }

    /// <inheritdoc cref="IsCenteredProperty"/>
    public bool IsCentered { get => _isCentered; private set => SetAndRaise(IsCenteredProperty, ref _isCentered, value); }

    internal void SetState(bool isChecked, bool isPassed, bool isLast, bool vertical, bool centered)
    {
        PseudoClasses.Set(":checked", isChecked);
        PseudoClasses.Set(":passed", isPassed);
        PseudoClasses.Set(":last", isLast);
        PseudoClasses.Set(":vertical", vertical);
        PseudoClasses.Set(":centered", centered);
        IsVertical = vertical;
        IsCentered = centered;
        PseudoClasses.Set(":icon", Icon is not null);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty)
        {
            PseudoClasses.Set(":icon", Icon is not null);
        }
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_trigger is not null)
        {
            _trigger.PointerReleased -= OnTriggerReleased;
        }
        if (_indicator is not null)
        {
            _indicator.PointerPressed -= OnIndicatorPressed;
        }
        _trigger = e.NameScope.Find<Control>("PART_Trigger");
        _indicator = e.NameScope.Find<Control>("PART_Indicator");
        if (_trigger is not null)
        {
            _trigger.PointerReleased += OnTriggerReleased;
        }
        if (_indicator is not null)
        {
            _indicator.PointerPressed += OnIndicatorPressed;
        }
    }

    // GPUI's active(): the indicator under a held button.
    private void OnIndicatorPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            PseudoClasses.Set(":indicator-pressed", true);
        }
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        PseudoClasses.Set(":indicator-pressed", false);
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        PseudoClasses.Set(":indicator-pressed", false);
    }

    private void OnTriggerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton == MouseButton.Left && _trigger is { } trigger && trigger.Bounds.WithX(0).WithY(0).Contains(e.GetPosition(trigger)))
        {
            (ItemsControl.ItemsControlFromItemContainer(this) as Stepper)?.Select(this);
        }
    }
}

/// <summary>
/// The panel of a <see cref="Stepper"/>: every step but the last shares what
/// the last leaves (all share equally when centered), across or down.
/// </summary>
public class StepperPanel : Panel
{
    /// <summary>Steps across (the default) or down.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<StepperPanel, Orientation>(nameof(Orientation), Orientation.Horizontal);

    /// <summary>Whether the last step shares the room too.</summary>
    public static readonly StyledProperty<bool> SharesEquallyProperty =
        AvaloniaProperty.Register<StepperPanel, bool>(nameof(SharesEqually));

    static StepperPanel()
    {
        AffectsMeasure<StepperPanel>(OrientationProperty, SharesEquallyProperty);
    }

    /// <inheritdoc cref="OrientationProperty"/>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <inheritdoc cref="SharesEquallyProperty"/>
    public bool SharesEqually { get => GetValue(SharesEquallyProperty); set => SetValue(SharesEquallyProperty, value); }

    private bool Across => Orientation == Orientation.Horizontal;

    private double[] Lengths(Size size)
    {
        var n = Children.Count;
        var total = Across ? size.Width : size.Height;
        var lengths = new double[n];
        if (n == 0 || double.IsInfinity(total))
        {
            for (var i = 0; i < n; i++)
            {
                lengths[i] = Across ? Children[i].DesiredSize.Width : Children[i].DesiredSize.Height;
            }
            return lengths;
        }
        if (SharesEqually)
        {
            Array.Fill(lengths, total / n);
            return lengths;
        }
        var last = Across ? Children[n - 1].DesiredSize.Width : Children[n - 1].DesiredSize.Height;
        lengths[n - 1] = last;
        for (var i = 0; i < n - 1; i++)
        {
            lengths[i] = Math.Max(0, (total - last) / (n - 1));
        }
        return lengths;
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        double across = 0, cross = 0;
        foreach (var child in Children)
        {
            child.Measure(Across ? new Size(double.PositiveInfinity, availableSize.Height) : new Size(availableSize.Width, double.PositiveInfinity));
            across += Across ? child.DesiredSize.Width : child.DesiredSize.Height;
            cross = Math.Max(cross, Across ? child.DesiredSize.Height : child.DesiredSize.Width);
        }
        return Across
            ? new Size(double.IsInfinity(availableSize.Width) ? across : availableSize.Width, cross)
            : new Size(cross, double.IsInfinity(availableSize.Height) ? across : availableSize.Height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var runs = LayoutSnap.Runs(this, 0, Lengths(finalSize));
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].Arrange(Across
                ? new Rect(runs[i].Start, 0, runs[i].Length, finalSize.Height)
                : new Rect(0, runs[i].Start, finalSize.Width, runs[i].Length));
        }
        return finalSize;
    }
}
