using Avalonia;
using Avalonia.Data.Converters;

namespace AvaloniaUIKit.Converters;

/// <summary>The gaps of a form field (form/field.rs) from the field's gap.</summary>
public static class FormConverters
{
    /// <summary>The label above the input: half the gap under it.</summary>
    public static readonly IValueConverter LabelGap = new FuncValueConverter<double, Thickness>(g => new Thickness(0, 0, 0, g / 2));

    /// <summary>The label beside the input: the gap after it.</summary>
    public static readonly IValueConverter LabelBeside = new FuncValueConverter<double, Thickness>(g => new Thickness(0, 0, g, 0));

    /// <summary>The description row: half the gap above it, present even when empty.</summary>
    public static readonly IValueConverter HalfTop = new FuncValueConverter<double, Thickness>(g => new Thickness(0, g / 2, 0, 0));

    /// <summary>The footer: the form's row gap above it.</summary>
    public static readonly IValueConverter TopGap = new FuncValueConverter<double, Thickness>(g => new Thickness(0, g, 0, 0));
}
