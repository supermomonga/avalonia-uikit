using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Marker (marker.rs): a muted status row for a conversation or a
/// timeline: an <see cref="Icon"/> and the content. Classes: separator (lines
/// on both sides, centered by default), border (a bottom border); the
/// alignment is HorizontalContentAlignment (start by default). While
/// <see cref="IsLoading"/> a small spinner leads, or with the shimmer class a
/// text content shimmers (other content pulses).
/// </summary>
public class Marker : ContentControl
{
    /// <summary>The icon before the content, in a 16px box.</summary>
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<Marker, object?>(nameof(Icon));

    /// <summary>Whether the marker shows that something is in progress.</summary>
    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<Marker, bool>(nameof(IsLoading));

    static Marker()
    {
        IconProperty.Changed.AddClassHandler<Marker>((m, _) => m.Update());
        IsLoadingProperty.Changed.AddClassHandler<Marker>((m, _) => m.Update());
        ContentProperty.Changed.AddClassHandler<Marker>((m, _) => m.Update());
    }

    /// <summary>Creates a marker.</summary>
    public Marker() => Update();

    /// <inheritdoc cref="IconProperty"/>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <inheritdoc cref="IsLoadingProperty"/>
    public bool IsLoading { get => GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    private void Update()
    {
        PseudoClasses.Set(":icon", Icon is not null);
        PseudoClasses.Set(":loading", IsLoading);
        PseudoClasses.Set(":text", Content is string);
    }
}

/// <summary>
/// The row of a <see cref="Marker"/>: a leading line, the middle and a
/// trailing line (its three children). Visible lines share what the middle
/// leaves; without lines the middle sits at the alignment.
/// </summary>
public class MarkerPanel : Panel
{
    /// <summary>Where the middle sits without lines.</summary>
    public static readonly StyledProperty<HorizontalAlignment> AlignmentProperty =
        AvaloniaProperty.Register<MarkerPanel, HorizontalAlignment>(nameof(Alignment));

    /// <summary>The gap between the parts (gap_2).</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MarkerPanel, double>(nameof(Spacing), 8);

    static MarkerPanel()
    {
        AffectsArrange<MarkerPanel>(AlignmentProperty, SpacingProperty);
    }

    /// <inheritdoc cref="AlignmentProperty"/>
    public HorizontalAlignment Alignment { get => GetValue(AlignmentProperty); set => SetValue(AlignmentProperty, value); }

    /// <inheritdoc cref="SpacingProperty"/>
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        double height = 0, width = 0;
        foreach (var child in Children)
        {
            child.Measure(availableSize);
            height = Math.Max(height, child.DesiredSize.Height);
            width += child.DesiredSize.Width;
        }
        return new Size(double.IsInfinity(availableSize.Width) ? width : availableSize.Width, height);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 3)
        {
            return finalSize;
        }
        var (leading, middle, trailing) = (Children[0], Children[1], Children[2]);
        var lines = new[] { leading, trailing }.Where(l => l.IsVisible).ToArray();
        var middleWidth = Math.Min(middle.DesiredSize.Width, finalSize.Width);
        var gaps = Spacing * lines.Length;
        var share = lines.Length == 0 ? 0 : Math.Max(0, (finalSize.Width - middleWidth - gaps) / lines.Length);
        double x;
        if (lines.Length > 0)
        {
            x = 0;
            if (leading.IsVisible)
            {
                leading.Arrange(new Rect(0, 0, share, finalSize.Height));
                x = share + Spacing;
            }
            middle.Arrange(new Rect(x, 0, middleWidth, finalSize.Height));
            if (trailing.IsVisible)
            {
                trailing.Arrange(new Rect(x + middleWidth + Spacing, 0, share, finalSize.Height));
            }
            return finalSize;
        }
        x = Alignment switch
        {
            HorizontalAlignment.Center => (finalSize.Width - middleWidth) / 2,
            HorizontalAlignment.Right => finalSize.Width - middleWidth,
            _ => 0,
        };
        middle.Arrange(new Rect(x, 0, middleWidth, finalSize.Height));
        return finalSize;
    }
}
