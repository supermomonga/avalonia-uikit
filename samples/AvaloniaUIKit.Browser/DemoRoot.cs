using Avalonia;
using Avalonia.Controls;

namespace AvaloniaUIKit.Browser;

/// <summary>
/// A view's content: lays the demo out at the element's width with no limit on the height, as the
/// previews do, centers it, and reports the height it takes, so that the page sizes the element to
/// it. An element narrower than the preview then wraps the demo instead of cutting off its bottom.
/// The view reaches past the element by <c>inset</c> on every side (sites/app/style.css), which this
/// keeps clear as padding, and the demo (a <see cref="UserControl"/>, which clips like every templated
/// control) does not clip, so that focus rings and shadows outside the demo's bounds still show.
/// </summary>
internal sealed class DemoRoot : Decorator
{
    private readonly Action<double> _heightChanged;
    private double _height = -1;

    public DemoRoot(Control demo, double inset, Action<double> heightChanged)
    {
        demo.ClipToBounds = false;
        Child = demo;
        Padding = new Thickness(inset);
        _heightChanged = heightChanged;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize.WithHeight(double.PositiveInfinity));
        var height = Child?.DesiredSize.Height ?? 0;
        if (height != _height)
        {
            _height = height;
            _heightChanged(_height);
        }
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            var bounds = new Rect(finalSize).Deflate(Padding);
            var width = Math.Min(child.DesiredSize.Width, bounds.Width);
            child.Arrange(new Rect(bounds.X + (bounds.Width - width) / 2, bounds.Y, width, bounds.Height));
        }
        return finalSize;
    }
}
