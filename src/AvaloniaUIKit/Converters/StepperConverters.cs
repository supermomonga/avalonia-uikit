using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>Where a stepper step's line goes (stepper/item.rs StepperSeparator).</summary>
public static class StepperConverters
{
    /// <summary>
    /// The line's margin from the step's bounds, whether the stepper is
    /// vertical, whether steps are centered, and the indicator's size: 4px
    /// after the indicator to 4px before the step's end (the next step), at
    /// the indicator's middle; centered steps run from the middle plus half
    /// the indicator to the next step's middle minus as much.
    /// </summary>
    public static readonly IMultiValueConverter LineMargin = new FuncMultiValueConverter<object?, Thickness>(values =>
    {
        var v = values.ToArray();
        var bounds = v.Length > 0 && v[0] is Rect r ? r : default;
        var vertical = v.Length > 1 && v[1] is true;
        var centered = v.Length > 2 && v[2] is true;
        var icon = v.Length > 3 && v[3] is double d && !double.IsNaN(d) ? d : 24;
        const double gap = 4;
        if (vertical)
        {
            return centered
                ? new Thickness(icon / 2, bounds.Height / 2 + icon / 2 + gap, 0, -bounds.Height / 2 + icon / 2 + gap)
                : new Thickness(icon / 2, icon + gap, 0, gap);
        }
        return centered
            ? new Thickness(bounds.Width / 2 + icon / 2 + gap, icon / 2, -bounds.Width / 2 + icon / 2 + gap, 0)
            : new Thickness(icon + gap, icon / 2, gap, 0);
    });
}
