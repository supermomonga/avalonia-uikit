using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit.Browser;

/// <summary>
/// A view's content: lays the demo out at the host's width with no limit on the height, as the
/// previews do, centers it, and reports the height it takes, so that the page sizes the host to
/// it. A host narrower than the preview then wraps the demo instead of cutting off its bottom.
/// </summary>
internal sealed class DemoRoot : Decorator
{
    private readonly Action<double> _heightChanged;
    private double _height = -1;

    public DemoRoot(Control demo, Action<double> heightChanged)
    {
        Child = demo;
        _heightChanged = heightChanged;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize.WithHeight(double.PositiveInfinity));
        if (size.Height != _height)
        {
            _height = size.Height;
            _heightChanged(_height);
        }
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            var width = Math.Min(child.DesiredSize.Width, finalSize.Width);
            child.Arrange(new Rect((finalSize.Width - width) / 2, 0, width, finalSize.Height));
        }
        return finalSize;
    }
}
