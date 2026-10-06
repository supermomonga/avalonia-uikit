using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace AvaloniaUIKit.Demo.ControlCatalog.Xaml;

/// <summary>
/// Values as XAML writes them, and back: the types the property grid edits. Every
/// conversion is written out per type; nothing is looked up by reflection (ADR 11).
/// </summary>
public static class XamlValues
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    /// <summary>Whether the property grid can edit values of <paramref name="type"/> as text or a choice.</summary>
    public static bool IsEditable(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsEnum
            || type == typeof(string)
            || type == typeof(bool)
            || IsNumber(type)
            || type == typeof(Thickness)
            || type == typeof(CornerRadius)
            || type == typeof(Color)
            || type == typeof(IBrush)
            || type == typeof(FontFamily)
            || type == typeof(GridLength)
            || type == typeof(Size)
            || type == typeof(Point)
            || type == typeof(TimeSpan)
            || type == typeof(DateTime)
            || type == typeof(KeyGesture)
            || type == typeof(Geometry)
            || type == typeof(MaskPattern);
    }

    /// <summary>Whether <paramref name="type"/> is a number type.</summary>
    public static bool IsNumber(Type type) =>
        type == typeof(double) || type == typeof(float) || type == typeof(decimal) || IsInteger(type);

    /// <summary>Whether <paramref name="type"/> is an integer type.</summary>
    public static bool IsInteger(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
        || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte);

    /// <summary>
    /// <paramref name="value"/> as a XAML attribute writes it, or null when it has no
    /// attribute form (a control, a template, a geometry that is no bundled icon).
    /// </summary>
    public static string? Format(object? value) => value switch
    {
        null => "{x:Null}",
        string s => s,
        bool b => b ? "True" : "False",
        double d => FormatDouble(d),
        float f => FormatDouble(f),
        decimal m => m.ToString(Invariant),
        int or long or short or byte or uint or ulong or ushort or sbyte => Convert.ToString(value, Invariant),
        Enum e => e.ToString(),
        Thickness t => FormatThickness(t),
        CornerRadius r => r.TopLeft == r.TopRight && r.TopLeft == r.BottomRight && r.TopLeft == r.BottomLeft
            ? FormatDouble(r.TopLeft)
            : string.Join(',', FormatDouble(r.TopLeft), FormatDouble(r.TopRight), FormatDouble(r.BottomRight), FormatDouble(r.BottomLeft)),
        Color c => FormatColor(c),
        ISolidColorBrush { Opacity: 1 } brush => FormatColor(brush.Color),
        FontFamily family => family.ToString(),
        GridLength length => length.ToString(),
        Size size => $"{FormatDouble(size.Width)},{FormatDouble(size.Height)}",
        Point point => $"{FormatDouble(point.X)},{FormatDouble(point.Y)}",
        TimeSpan span => span.ToString("c", Invariant),
        DateTime date => date.TimeOfDay == TimeSpan.Zero ? date.ToString("yyyy-MM-dd", Invariant) : date.ToString("s", Invariant),
        KeyGesture gesture => gesture.ToString(),
        PatternMask mask => mask.Pattern,
        Geometry geometry => Icons.KeyOf(geometry) is { } key ? $"{{StaticResource {key}}}" : null,
        _ => null,
    };

    /// <summary>
    /// <paramref name="value"/> as the property grid shows it in a text box: as XAML writes it,
    /// with no braces for null.
    /// </summary>
    public static string Display(object? value) => value switch
    {
        null => "",
        Geometry geometry => Icons.KeyOf(geometry) is { } key ? Icons.NameOf(key) : "(geometry)",
        _ => Format(value) ?? value.ToString() ?? "",
    };

    /// <summary>Reads <paramref name="text"/> as a value of <paramref name="type"/>.</summary>
    /// <returns>Whether the text is such a value; an empty text is null for a type that allows it.</returns>
    public static bool TryParse(string text, Type type, out object? value)
    {
        value = null;
        var underlying = Nullable.GetUnderlyingType(type);
        var nullable = underlying is not null || !type.IsValueType;
        type = underlying ?? type;
        text = text.Trim();
        if (text.Length == 0 || text == "{x:Null}")
        {
            if (type == typeof(string) && text.Length == 0)
            {
                value = "";
                return true;
            }
            return nullable;
        }
        try
        {
            value = Parse(text, type);
            return value is not null;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static object? Parse(string text, Type type)
    {
        if (type == typeof(string))
        {
            return text;
        }
        if (type.IsEnum)
        {
            return Enum.Parse(type, text, ignoreCase: true);
        }
        if (type == typeof(bool))
        {
            return bool.Parse(text);
        }
        if (type == typeof(double))
        {
            return ParseDouble(text);
        }
        if (type == typeof(float))
        {
            return (float)ParseDouble(text);
        }
        if (type == typeof(decimal))
        {
            return decimal.Parse(text, NumberStyles.Float, Invariant);
        }
        if (type == typeof(int))
        {
            return int.Parse(text, NumberStyles.Integer, Invariant);
        }
        if (type == typeof(long))
        {
            return long.Parse(text, NumberStyles.Integer, Invariant);
        }
        if (type == typeof(short))
        {
            return short.Parse(text, NumberStyles.Integer, Invariant);
        }
        if (type == typeof(byte))
        {
            return byte.Parse(text, NumberStyles.Integer, Invariant);
        }
        if (type == typeof(uint))
        {
            return uint.Parse(text, NumberStyles.Integer, Invariant);
        }
        if (type == typeof(ulong))
        {
            return ulong.Parse(text, NumberStyles.Integer, Invariant);
        }
        if (type == typeof(ushort))
        {
            return ushort.Parse(text, NumberStyles.Integer, Invariant);
        }
        if (type == typeof(sbyte))
        {
            return sbyte.Parse(text, NumberStyles.Integer, Invariant);
        }
        if (type == typeof(Thickness))
        {
            return Thickness.Parse(text);
        }
        if (type == typeof(CornerRadius))
        {
            return CornerRadius.Parse(text);
        }
        if (type == typeof(Color))
        {
            return Color.Parse(text);
        }
        if (type == typeof(IBrush))
        {
            return new SolidColorBrush(Color.Parse(text)).ToImmutable();
        }
        if (type == typeof(FontFamily))
        {
            return FontFamily.Parse(text);
        }
        if (type == typeof(GridLength))
        {
            return GridLength.Parse(text);
        }
        if (type == typeof(Size))
        {
            return Size.Parse(text);
        }
        if (type == typeof(Point))
        {
            return Point.Parse(text);
        }
        if (type == typeof(TimeSpan))
        {
            return TimeSpan.Parse(text, Invariant);
        }
        if (type == typeof(DateTime))
        {
            return DateTime.Parse(text, Invariant);
        }
        if (type == typeof(KeyGesture))
        {
            return KeyGesture.Parse(text);
        }
        if (type == typeof(MaskPattern))
        {
            return MaskPattern.Parse(text);
        }
        if (type == typeof(Geometry))
        {
            return Icons.Find(text);
        }
        return null;
    }

    /// <summary>Whether the live <paramref name="value"/> is what the XAML <paramref name="text"/> says.</summary>
    public static bool Matches(object? value, string text)
    {
        if (value is null)
        {
            return text is "{x:Null}" or "";
        }
        if (Format(value) is { } formatted && string.Equals(formatted, text.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        var type = value.GetType();
        return TryParse(text, type, out var parsed) && Equals(parsed, value);
    }

    private static string FormatDouble(double value) =>
        double.IsNaN(value) ? "NaN"
        : double.IsPositiveInfinity(value) ? "Infinity"
        : double.IsNegativeInfinity(value) ? "-Infinity"
        : Math.Round(value, 4).ToString("0.####", Invariant);

    private static double ParseDouble(string text) => text.ToLowerInvariant() switch
    {
        "auto" or "nan" => double.NaN,
        "infinity" or "∞" => double.PositiveInfinity,
        "-infinity" => double.NegativeInfinity,
        _ => double.Parse(text, NumberStyles.Float, Invariant),
    };

    private static string FormatThickness(Thickness t)
    {
        if (t.IsUniform)
        {
            return FormatDouble(t.Left);
        }
        if (t.Left == t.Right && t.Top == t.Bottom)
        {
            return $"{FormatDouble(t.Left)},{FormatDouble(t.Top)}";
        }
        return string.Join(',', FormatDouble(t.Left), FormatDouble(t.Top), FormatDouble(t.Right), FormatDouble(t.Bottom));
    }

    private static string FormatColor(Color c) =>
        c.A == 255 ? $"#{c.R:X2}{c.G:X2}{c.B:X2}" : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
}
