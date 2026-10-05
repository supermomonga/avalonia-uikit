using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's ResizablePanel (crates/base/src/resizable/panel.rs): a panel of
/// a <see cref="ResizablePanelGroup"/>. <see cref="Size"/> is its initial
/// length along the group's orientation (none: it shares the length the sized
/// panels leave), and <see cref="MinSize"/> and <see cref="MaxSize"/> bound it
/// while it is laid out and resized (GPUI's <c>size_range</c>, 100px to no
/// limit by default). A hidden panel (<c>IsVisible="False"</c>, GPUI's
/// <c>visible(false)</c>) takes no room and keeps its length for when it is
/// shown again. Any other control in a group is a panel without a size.
/// </summary>
public class ResizablePanel : ContentControl
{
    /// <summary>The initial length along the group (GPUI's <c>size</c>; NaN, the default, for none).</summary>
    public static readonly StyledProperty<double> SizeProperty =
        AvaloniaProperty.Register<ResizablePanel, double>(nameof(Size), double.NaN);

    /// <summary>The smallest length (the start of GPUI's <c>size_range</c>; PANEL_MIN_SIZE, 100, by default).</summary>
    public static readonly StyledProperty<double> MinSizeProperty =
        AvaloniaProperty.Register<ResizablePanel, double>(nameof(MinSize), ResizablePanelGroup.PanelMinSize);

    /// <summary>The largest length (the end of GPUI's <c>size_range</c>; no limit by default).</summary>
    public static readonly StyledProperty<double> MaxSizeProperty =
        AvaloniaProperty.Register<ResizablePanel, double>(nameof(MaxSize), double.PositiveInfinity);

    static ResizablePanel()
    {
        SizeProperty.Changed.AddClassHandler<ResizablePanel>((panel, _) => panel.InvalidateGroup());
        MinSizeProperty.Changed.AddClassHandler<ResizablePanel>((panel, _) => panel.InvalidateGroup());
        MaxSizeProperty.Changed.AddClassHandler<ResizablePanel>((panel, _) => panel.InvalidateGroup());
    }

    /// <inheritdoc cref="SizeProperty"/>
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc cref="MinSizeProperty"/>
    public double MinSize
    {
        get => GetValue(MinSizeProperty);
        set => SetValue(MinSizeProperty, value);
    }

    /// <inheritdoc cref="MaxSizeProperty"/>
    public double MaxSize
    {
        get => GetValue(MaxSizeProperty);
        set => SetValue(MaxSizeProperty, value);
    }

    private void InvalidateGroup() => (Parent as ResizablePanelGroup)?.InvalidateMeasure();
}
