using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// Keeps an icon's one-pixel strokes sharp. A Lucide icon at 12px (an xsmall
/// button's) draws its 2px strokes 1px wide, and a stroke on the icon's center
/// line, centered in an even-sized button, falls between two device pixels at
/// an odd scaling (100%): both are painted half dark, and the icon looks
/// blurred. At an even scaling (200%) the stroke fills whole pixels.
/// </summary>
public static class IconStrokes
{
    /// <summary>
    /// Shifts the icon right and down by half a device pixel at an odd whole
    /// scaling, so the strokes on its center lines fill whole pixels. Layout is
    /// unchanged; other scalings draw it where it is.
    /// </summary>
    public static readonly AttachedProperty<bool> SnapsToPixelsProperty =
        AvaloniaProperty.RegisterAttached<Visual, bool>("SnapsToPixels", typeof(IconStrokes));

    private static readonly AttachedProperty<Snapper?> SnapperProperty =
        AvaloniaProperty.RegisterAttached<Visual, Snapper?>("Snapper", typeof(IconStrokes));

    static IconStrokes()
    {
        SnapsToPixelsProperty.Changed.AddClassHandler<Visual>((icon, e) =>
        {
            if (icon.GetValue(SnapperProperty) is { } snapper)
            {
                snapper.Apply();
            }
            else if (e.GetNewValue<bool>())
            {
                icon.SetValue(SnapperProperty, new Snapper(icon));
            }
        });
    }

    /// <summary>Gets whether the icon's strokes are snapped to device pixels.</summary>
    public static bool GetSnapsToPixels(Visual element) => element.GetValue(SnapsToPixelsProperty);

    /// <summary>Sets whether the icon's strokes are snapped to device pixels.</summary>
    public static void SetSnapsToPixels(Visual element, bool value) => element.SetValue(SnapsToPixelsProperty, value);

    /// <summary>Follows the scaling of the icon's top level.</summary>
    private sealed class Snapper
    {
        private readonly Visual _icon;
        private TopLevel? _top;

        public Snapper(Visual icon)
        {
            _icon = icon;
            icon.AttachedToVisualTree += (_, _) => Attach();
            icon.DetachedFromVisualTree += (_, _) => Detach();
            Attach();
        }

        public void Apply()
        {
            var scale = _top?.RenderScaling ?? 1;
            var whole = Math.Round(scale);
            var offset = GetSnapsToPixels(_icon) && Math.Abs(scale - whole) < 0.001 && whole % 2 == 1 ? 0.5 / scale : 0;
            if (offset == 0)
            {
                _icon.ClearValue(Visual.RenderTransformProperty);
            }
            else
            {
                _icon.RenderTransform = new TranslateTransform(offset, offset);
            }
        }

        private void Attach()
        {
            Detach();
            _top = TopLevel.GetTopLevel(_icon);
            if (_top is not null)
            {
                _top.ScalingChanged += OnScalingChanged;
            }
            Apply();
        }

        private void Detach()
        {
            if (_top is not null)
            {
                _top.ScalingChanged -= OnScalingChanged;
            }
            _top = null;
        }

        private void OnScalingChanged(object? sender, EventArgs e) => Apply();
    }
}
