using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaloniaUIKit;

/// <summary>The side of an <see cref="InputGroupAddon"/> relative to the input (GPUI's InputGroupAddonAlignment).</summary>
public enum InputGroupAddonAlignment
{
    /// <summary>Before the input, on its line (the default).</summary>
    InlineStart,

    /// <summary>After the input, on its line.</summary>
    InlineEnd,

    /// <summary>Above the input's line, across the group.</summary>
    BlockStart,

    /// <summary>Below the input's line, across the group.</summary>
    BlockEnd,
}

/// <summary>
/// GPUI Kit's InputGroup (crates/component/src/input/group.rs): one frame around
/// a TextBox (single-line or multi-line) and its <see cref="InputGroupAddon"/>s,
/// on the input's line or in rows above and below it.
/// </summary>
/// <remarks>
/// <para>
/// The frame is the input frame (input_h tall while the input is single-line
/// and no addon is above or below it), transparent in light themes and tinted
/// in dark ones; its border turns to the ring color, with the ring band, while
/// the input has focus, over 120ms. The input inside draws no frame of its own.
/// Size classes (xsmall, small, large) size the frame, the input and the
/// addons. Buttons in the addons take GPUI's InputGroupButton look.
/// </para>
/// <para>
/// <see cref="IsInvalid"/>, or a validation error on the input or the group,
/// shows the danger border and ring, even disabled. A disabled group, or a
/// disabled input, disables the input and the addons and fades the group to
/// half. <see cref="IsReadOnly"/> makes the input read-only. A press on an
/// addon (not a button in it) or the frame's padding focuses the input.
/// </para>
/// </remarks>
[PseudoClasses(":input-focused", ":input-disabled", ":invalid", ":multiline", ":block", ":inline-start", ":inline-end")]
public class InputGroup : ItemsControl
{
    /// <summary>Whether the input is read-only while the addons stay usable (GPUI's readonly()).</summary>
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<InputGroup, bool>(nameof(IsReadOnly));

    /// <summary>Whether the group shows the app's validation failure (GPUI's invalid()); it does not reject edits.</summary>
    public static readonly StyledProperty<bool> IsInvalidProperty =
        AvaloniaProperty.Register<InputGroup, bool>(nameof(IsInvalid));

    private Control? _input;

    static InputGroup()
    {
        IsInvalidProperty.Changed.AddClassHandler<InputGroup>((group, _) => group.UpdateState());
        DataValidationErrors.HasErrorsProperty.Changed.AddClassHandler<InputGroup>((group, _) => group.UpdateState());
    }

    /// <summary>Creates an empty group.</summary>
    public InputGroup()
    {
        Items.CollectionChanged += OnItemsChanged;
    }

    /// <inheritdoc cref="IsReadOnlyProperty"/>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <inheritdoc cref="IsInvalidProperty"/>
    public bool IsInvalid
    {
        get => GetValue(IsInvalidProperty);
        set => SetValue(IsInvalidProperty, value);
    }

    /// <summary>The group's input: its first item that is not an addon.</summary>
    public Control? Input => _input;

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // group.rs: a press on an addon or the inset focuses the input; buttons keep theirs.
        if (!e.Handled && _input is { IsEffectivelyEnabled: true } input && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            (input as TextBox ?? input.GetVisualDescendants().OfType<TextBox>().FirstOrDefault() as InputElement ?? input).Focus(NavigationMethod.Pointer);
            e.Handled = true;
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var input = Items.OfType<Control>().FirstOrDefault(c => c is not InputGroupAddon);
        if (!ReferenceEquals(input, _input))
        {
            if (_input is not null)
            {
                _input.PropertyChanged -= OnInputPropertyChanged;
            }
            _input = input;
            if (_input is not null)
            {
                _input.PropertyChanged += OnInputPropertyChanged;
            }
        }
        foreach (var addon in Items.OfType<InputGroupAddon>())
        {
            addon.PropertyChanged -= OnAddonPropertyChanged;
            addon.PropertyChanged += OnAddonPropertyChanged;
        }
        UpdateState();
    }

    private void OnInputPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsKeyboardFocusWithinProperty || e.Property == IsEffectivelyEnabledProperty ||
            e.Property == TextBox.AcceptsReturnProperty || e.Property == DataValidationErrors.HasErrorsProperty)
        {
            UpdateState();
        }
    }

    private void OnAddonPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputGroupAddon.AlignmentProperty)
        {
            UpdateState();
        }
    }

    private void UpdateState()
    {
        var input = _input;
        PseudoClasses.Set(":input-focused", input is { IsKeyboardFocusWithin: true, IsEffectivelyEnabled: true });
        PseudoClasses.Set(":input-disabled", input is { IsEnabled: false });
        PseudoClasses.Set(":invalid", IsInvalid || DataValidationErrors.GetHasErrors(this) || (input is not null && DataValidationErrors.GetHasErrors(input)));
        PseudoClasses.Set(":multiline", input is TextBox { AcceptsReturn: true });
        var addons = Items.OfType<InputGroupAddon>().ToList();
        PseudoClasses.Set(":block", addons.Any(a => a.Alignment is InputGroupAddonAlignment.BlockStart or InputGroupAddonAlignment.BlockEnd));
        PseudoClasses.Set(":inline-start", addons.Any(a => a.Alignment == InputGroupAddonAlignment.InlineStart));
        PseudoClasses.Set(":inline-end", addons.Any(a => a.Alignment == InputGroupAddonAlignment.InlineEnd));
    }
}

/// <summary>
/// Text, icons, buttons or other content on one side of an <see cref="InputGroup"/>'s
/// input (GPUI's InputGroupAddon): a row with 8px between its items, in muted
/// medium-weight text. An item aligned right goes to the row's end (GPUI's ml_auto).
/// </summary>
public class InputGroupAddon : ItemsControl
{
    /// <summary>Where the addon goes (GPUI's align()); before the input by default.</summary>
    public static readonly StyledProperty<InputGroupAddonAlignment> AlignmentProperty =
        AvaloniaProperty.Register<InputGroupAddon, InputGroupAddonAlignment>(nameof(Alignment));

    /// <inheritdoc cref="AlignmentProperty"/>
    public InputGroupAddonAlignment Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }
}

/// <summary>
/// The layout of an <see cref="InputGroup"/>: the BlockStart addons, then the
/// input's line (InlineStart addons, the input taking the rest, InlineEnd
/// addons; each centered on the line), then the BlockEnd addons. The line fills
/// the height the block rows leave.
/// </summary>
public class InputGroupPanel : Panel
{
    private static InputGroupAddonAlignment? AlignmentOf(Control child) => (child as InputGroupAddon)?.Alignment;

    private static bool IsBlock(Control child) => AlignmentOf(child) is InputGroupAddonAlignment.BlockStart or InputGroupAddonAlignment.BlockEnd;

    private static bool IsInline(Control child) => AlignmentOf(child) is InputGroupAddonAlignment.InlineStart or InputGroupAddonAlignment.InlineEnd;

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        double inlineWidth = 0, line = 0, blocks = 0, blockWidth = 0;
        var visible = Children.Where(c => c.IsVisible).ToList();
        // The line's items keep their own heights: a fixed-height group centers them, overflowing.
        foreach (var child in visible.Where(IsInline))
        {
            child.Measure(Size.Infinity);
            inlineWidth += child.DesiredSize.Width;
            line = Math.Max(line, child.DesiredSize.Height);
        }
        var inputs = visible.Where(c => AlignmentOf(c) is null).ToList();
        double inputWidth = 0;
        foreach (var child in inputs)
        {
            child.Measure(new Size(Math.Max(0, (availableSize.Width - inlineWidth) / inputs.Count), double.PositiveInfinity));
            inputWidth += child.DesiredSize.Width;
            line = Math.Max(line, child.DesiredSize.Height);
        }
        foreach (var child in visible.Where(IsBlock))
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            blocks += child.DesiredSize.Height;
            blockWidth = Math.Max(blockWidth, child.DesiredSize.Width);
        }
        return new Size(Math.Max(inlineWidth + inputWidth, blockWidth), blocks + line);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        var y = 0.0;
        foreach (var child in visible.Where(c => AlignmentOf(c) == InputGroupAddonAlignment.BlockStart))
        {
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        var below = visible.Where(c => AlignmentOf(c) == InputGroupAddonAlignment.BlockEnd).Sum(c => c.DesiredSize.Height);
        var line = Math.Max(0, finalSize.Height - y - below);
        var starts = visible.Where(c => AlignmentOf(c) == InputGroupAddonAlignment.InlineStart).ToList();
        var ends = visible.Where(c => AlignmentOf(c) == InputGroupAddonAlignment.InlineEnd).ToList();
        var inputs = visible.Where(c => AlignmentOf(c) is null).ToList();
        var rest = Math.Max(0, finalSize.Width - starts.Sum(c => c.DesiredSize.Width) - ends.Sum(c => c.DesiredSize.Width));
        // An addon keeps its height on a shorter line, centered across it (h_flex items_center).
        Rect Centered(Control child, double x) =>
            new(x, y + (line - child.DesiredSize.Height) / 2, child.DesiredSize.Width, child.DesiredSize.Height);
        var x = 0.0;
        foreach (var child in starts)
        {
            child.Arrange(Centered(child, x));
            x += child.DesiredSize.Width;
        }
        foreach (var child in inputs)
        {
            var width = rest / inputs.Count;
            child.Arrange(new Rect(x, y, width, line));
            x += width;
        }
        foreach (var child in ends)
        {
            child.Arrange(Centered(child, x));
            x += child.DesiredSize.Width;
        }
        y += line;
        foreach (var child in visible.Where(c => AlignmentOf(c) == InputGroupAddonAlignment.BlockEnd))
        {
            child.Arrange(new Rect(0, y, finalSize.Width, child.DesiredSize.Height));
            y += child.DesiredSize.Height;
        }
        return finalSize;
    }
}

/// <summary>
/// The row of an <see cref="InputGroupAddon"/>: its items from the start with a
/// gap between them, those aligned right from the end; each centered vertically.
/// </summary>
public class InputGroupAddonPanel : Panel
{
    /// <summary>The space between items: 8 (gap_2).</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<InputGroupAddonPanel, double>(nameof(Spacing), 8);

    static InputGroupAddonPanel()
    {
        AffectsMeasure<InputGroupAddonPanel>(SpacingProperty);
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
        double width = 0, height = 0;
        var count = 0;
        foreach (var child in Children.Where(c => c.IsVisible))
        {
            child.Measure(Size.Infinity);
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            count++;
        }
        return new Size(width + Math.Max(0, count - 1) * Spacing, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        Rect Centered(Control child, double x) =>
            new(x, (finalSize.Height - child.DesiredSize.Height) / 2, child.DesiredSize.Width, child.DesiredSize.Height);
        var x = 0.0;
        foreach (var child in visible.Where(c => c.HorizontalAlignment != HorizontalAlignment.Right))
        {
            child.Arrange(Centered(child, x));
            x += child.DesiredSize.Width + Spacing;
        }
        var end = finalSize.Width;
        foreach (var child in Enumerable.Reverse(visible.Where(c => c.HorizontalAlignment == HorizontalAlignment.Right).ToList()))
        {
            end -= child.DesiredSize.Width;
            child.Arrange(Centered(child, end));
            end -= Spacing;
        }
        return finalSize;
    }
}
