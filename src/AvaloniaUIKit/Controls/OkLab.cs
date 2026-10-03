using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI Kit's Colorize::mix_oklab (theme/color.rs): premultiplied
/// interpolation in Oklab, <c>factor</c> of the first color.
/// </summary>
internal static class OkLab
{
    public static Color Mix(Color a, Color b, double factor)
    {
        factor = Math.Clamp(factor, 0, 1);
        var inv = 1 - factor;
        var (aa, ba) = (a.A / 255.0, b.A / 255.0);
        var alpha = aa * factor + ba * inv;
        if (alpha == 0)
        {
            return default;
        }
        var (l1, a1, b1) = ToOklab(a);
        var (l2, a2, b2) = ToOklab(b);
        var l = (l1 * aa * factor + l2 * ba * inv) / alpha;
        var x = (a1 * aa * factor + a2 * ba * inv) / alpha;
        var y = (b1 * aa * factor + b2 * ba * inv) / alpha;
        return FromOklab(l, x, y, alpha);
    }

    private static double ToLinear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double FromLinear(double c) => c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;

    private static (double L, double A, double B) ToOklab(Color c)
    {
        var (r, g, b) = (ToLinear(c.R / 255.0), ToLinear(c.G / 255.0), ToLinear(c.B / 255.0));
        var l = Math.Cbrt(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b);
        var m = Math.Cbrt(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b);
        var s = Math.Cbrt(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b);
        return (
            0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
            1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
            0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
    }

    private static Color FromOklab(double lightness, double a, double b, double alpha)
    {
        var l = Math.Pow(lightness + 0.3963377774 * a + 0.2158037573 * b, 3);
        var m = Math.Pow(lightness - 0.1055613458 * a - 0.0638541728 * b, 3);
        var s = Math.Pow(lightness - 0.0894841775 * a - 1.2914855480 * b, 3);
        static byte Channel(double v) => (byte)Math.Round(Math.Clamp(FromLinear(v), 0, 1) * 255);
        return Color.FromArgb(
            (byte)Math.Round(alpha * 255),
            Channel(4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s),
            Channel(-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s),
            Channel(-0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s));
    }
}
