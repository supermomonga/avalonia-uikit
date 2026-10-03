using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Layout;
using Avalonia.LogicalTree;

namespace AvaloniaUIKit.Tests.Rendering;

/// <summary>
/// Hosts the theme's WindowDrawnDecorations the way TopLevelHost does: the
/// headless window never asks for drawn decorations, so the test builds the
/// template itself and lays the underlay and overlay over its own bounds.
/// Template application, the enabled parts and the render scaling are
/// internal to Avalonia; the test reaches them as it reaches the clock (R7).
/// </summary>
public sealed class DecorationsHost : Control
{
    // DrawnWindowDecorationParts: Shadow = 1, Border = 2, TitleBar = 4.
    public const int TitleBar = 4;

    private readonly int _parts;

    public DecorationsHost(string? title, int parts = TitleBar)
    {
        _parts = parts;
        Decorations = new WindowDrawnDecorations { Title = title };
        LogicalChildren.Add(Decorations);
    }

    public WindowDrawnDecorations Decorations { get; }

    /// <summary>Hides the caption buttons, as GPUI on macOS draws none (the traffic lights are native).</summary>
    public bool HidesCaptionButtons { get; init; }

    // After the logical attach, which styles the decorations (and so sets their template).
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        var parts = AvaloniaPropertyRegistry.Instance.FindRegistered(Decorations, "EnabledParts")
            ?? throw new InvalidOperationException("WindowDrawnDecorations.EnabledParts is gone: Avalonia changed its decorations");
        Decorations.SetValue(parts, Enum.ToObject(parts.PropertyType, _parts));
        SetRenderScaling(Decorations, CaseHost.Scale);
        ApplyTemplate(Decorations);
        if (Decorations.Content is { } content)
        {
            foreach (var layer in new[] { content.Underlay, content.Overlay })
            {
                if (layer is not null && !VisualChildren.Contains(layer))
                {
                    VisualChildren.Add(layer);
                }
            }
            if (HidesCaptionButtons && content.Overlay?.GetLogicalDescendants().OfType<StackPanel>().FirstOrDefault(p => p.Name == "PART_OverlayPanel") is { } buttons)
            {
                buttons.IsVisible = false;
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in VisualChildren.OfType<Layoutable>())
        {
            child.Measure(availableSize);
        }
        return default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in VisualChildren.OfType<Layoutable>())
        {
            child.Arrange(new Rect(finalSize));
        }
        return finalSize;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ApplyTemplate")]
    private static extern void ApplyTemplate(WindowDrawnDecorations decorations);

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_RenderScaling")]
    private static extern void SetRenderScaling(WindowDrawnDecorations decorations, double value);
}
