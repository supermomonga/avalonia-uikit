using System.Globalization;
using Avalonia.Media;

namespace AvaloniaUIKit;

/// <summary>
/// GPUI's <c>Hsla</c> (gpui color.rs): hue, saturation, lightness and alpha,
/// each from 0 to 1 in single precision as GPUI keeps them, with GPUI's
/// conversions to and from RGB and GPUI Kit's <c>Colorize</c> shades
/// (theme/color.rs).
/// </summary>
internal readonly record struct GpuiHsla(float H, float S, float L, float A)
{
    /// <summary><c>hsl(h, s, l)</c> (theme/color.rs): degrees and percentages.</summary>
    public static GpuiHsla Hsl(float h, float s, float l) =>
        new(Math.Clamp(h / 360f, 0, 1), Math.Clamp(s / 100f, 0, 1), Math.Clamp(l / 100f, 0, 1), 1);

    /// <summary><c>From&lt;Hsla&gt; for Rgba</c>: red, green and blue from 0 to 1.</summary>
    public (float R, float G, float B) ToRgb()
    {
        var c = (1f - MathF.Abs(2f * L - 1f)) * S;
        var x = c * (1f - MathF.Abs(H * 6f % 2f - 1f));
        var m = L - c / 2f;
        var cm = c + m;
        var xm = x + m;
        var (r, g, b) = (int)MathF.Floor(H * 6f) switch
        {
            0 or 6 => (cm, xm, m),
            1 => (xm, cm, m),
            2 => (m, cm, xm),
            3 => (m, xm, cm),
            4 => (xm, m, cm),
            _ => (cm, m, xm),
        };
        return (Math.Clamp(r, 0f, 1f), Math.Clamp(g, 0f, 1f), Math.Clamp(b, 0f, 1f));
    }

    /// <summary>The color as the renderer writes it: each channel rounded to 8 bits.</summary>
    public Color ToColor()
    {
        var (r, g, b) = ToRgb();
        return Color.FromArgb(Byte(A), Byte(r), Byte(g), Byte(b));
    }

    /// <summary><c>From&lt;Rgba&gt; for Hsla</c>.</summary>
    public static GpuiHsla From(Color color)
    {
        float r = color.R / 255f, g = color.G / 255f, b = color.B / 255f;
        var max = MathF.Max(r, MathF.Max(g, b));
        var min = MathF.Min(r, MathF.Min(g, b));
        var delta = max - min;
        var l = (max + min) / 2f;
        var s = l is 0 or 1 ? 0 : l < 0.5f ? delta / (2f * l) : delta / (2f - 2f * l);
        var h = delta == 0 ? 0
            : max == r ? ((g - b) / delta % 6f + 6f) % 6f / 6f
            : max == g ? ((b - r) / delta + 2f) / 6f
            : ((r - g) / delta + 4f) / 6f;
        return new GpuiHsla(h, s, l, color.A / 255f);
    }

    /// <summary><c>Colorize::darken</c>: the lightness scaled down by <paramref name="factor"/>.</summary>
    public GpuiHsla Darken(float factor) => this with { L = L * (1f - Math.Clamp(factor, 0f, 1f)) };

    /// <summary><c>Colorize::lighten</c>: the lightness scaled up by <paramref name="factor"/>.</summary>
    public GpuiHsla Lighten(float factor) => this with { L = L * (1f + Math.Clamp(factor, 0f, 1f)) };

    /// <summary>
    /// <c>Colorize::to_hex</c> and base color_picker.rs <c>hex_string</c>: each
    /// channel times 255, truncated, in capitals; the alpha only while translucent.
    /// </summary>
    public string ToHex()
    {
        var (r, g, b) = ToRgb();
        static uint Channel(float v) => (uint)(v * 255f);
        return A < 1f
            ? string.Create(CultureInfo.InvariantCulture, $"#{Channel(r):X2}{Channel(g):X2}{Channel(b):X2}{Channel(A):X2}")
            : string.Create(CultureInfo.InvariantCulture, $"#{Channel(r):X2}{Channel(g):X2}{Channel(b):X2}");
    }

    private static byte Byte(float v) => (byte)MathF.Round(Math.Clamp(v, 0f, 1f) * 255f, MidpointRounding.AwayFromZero);
}

/// <summary>GPUI Kit's color texts and palettes, for the color select.</summary>
internal static class GpuiColor
{
    /// <summary>The color darkened (negative) or lightened by <paramref name="factor"/>, as GPUI Kit shades it.</summary>
    public static Color Shade(Color color, float factor) => factor switch
    {
        0 => color,
        < 0 => GpuiHsla.From(color).Darken(-factor).ToColor(),
        _ => GpuiHsla.From(color).Lighten(factor).ToColor(),
    };

    /// <summary>
    /// <c>Colorize::to_hex</c> and base color_picker.rs <c>hex_string</c>:
    /// <c>#RRGGBB</c> in capitals, <c>#RRGGBBAA</c> while the color is translucent.
    /// </summary>
    public static string Hex(Color color) => color.A < 255
        ? string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}")
        : string.Create(CultureInfo.InvariantCulture, $"#{color.R:X2}{color.G:X2}{color.B:X2}");

    /// <summary>
    /// base color_picker.rs <c>parse_hex</c>: <c>#rgb</c>, <c>#rgba</c>,
    /// <c>#rrggbb</c> or <c>#rrggbbaa</c>, the <c>#</c> optional; a single
    /// digit repeats itself (<c>#fff</c> is white).
    /// </summary>
    public static Color? ParseHex(string? text)
    {
        if (text is null)
        {
            return null;
        }
        var value = text.StartsWith('#') ? text[1..] : text;
        if (!value.All(char.IsAsciiHexDigit))
        {
            return null;
        }
        var (width, alpha) = value.Length switch
        {
            3 => (1, false),
            4 => (1, true),
            6 => (2, false),
            8 => (2, true),
            _ => (0, false),
        };
        if (width == 0)
        {
            return null;
        }
        byte Part(int index)
        {
            var raw = byte.Parse(value.AsSpan(index * width, width), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return width == 1 ? (byte)(raw * 0x11) : raw;
        }
        return Color.FromArgb(alpha ? Part(3) : (byte)255, Part(0), Part(1), Part(2));
    }

    /// <summary>
    /// Whether the hex field accepts <paramref name="text"/> while it is typed
    /// (base color_picker.rs HEX_PATTERN, <c>^#[0-9a-fA-F]{0,8}$</c>).
    /// </summary>
    public static bool IsHexInput(string? text) =>
        text is { Length: >= 1 and <= 9 } && text[0] == '#' && text.AsSpan(1).IndexOfAnyExcept("0123456789abcdefABCDEF") < 0;

    /// <summary>
    /// color_picker.rs color_palettes: stone, red, orange, yellow, green, cyan,
    /// blue, purple and pink from GPUI Kit's default colors (theme/default-colors.json,
    /// read from their hslChannel), each from 950 down to 50.
    /// </summary>
    public static readonly IReadOnlyList<IReadOnlyList<GpuiHsla>> Palettes = Rows(
    [
        // stone
        [(20, 14.3, 4.1), (24, 9.8, 10), (12, 6.5, 15.1), (30, 6.3, 25.1), (33.3, 5.5, 32.4), (25, 5.3, 44.7), (24, 5.4, 63.9), (24, 5.7, 82.9), (20, 5.9, 90), (60, 4.8, 95.9), (60, 9.1, 97.8)],
        // red
        [(0, 74.7, 15.5), (0, 62.8, 30.6), (0, 70, 35.3), (0, 73.7, 41.8), (0, 72.2, 50.6), (0, 84.2, 60.2), (0, 90.6, 70.8), (0, 93.5, 81.8), (0, 96.3, 89.4), (0, 93.3, 94.1), (0, 85.7, 97.3)],
        // orange
        [(13, 81.1, 14.5), (15.3, 74.6, 27.8), (15, 79.1, 33.7), (17.5, 88.3, 40.4), (20.5, 90.2, 48.2), (24.6, 95, 53.1), (27, 96, 61), (30.7, 97.2, 72.4), (32.1, 97.7, 83.1), (34.3, 100, 91.8), (33.3, 100, 96.5)],
        // yellow
        [(26, 83.3, 14.1), (28.4, 72.5, 25.7), (31.8, 81, 28.8), (35.5, 91.7, 32.9), (40.6, 96.1, 40.4), (45.4, 93.4, 47.5), (47.9, 95.8, 53.1), (50.4, 97.8, 63.5), (52.8, 98.3, 76.9), (54.9, 96.7, 88), (54.5, 91.7, 95.3)],
        // green
        [(144.9, 80.4, 10), (143.8, 61.2, 20.2), (142.8, 64.2, 24.1), (142.4, 71.8, 29.2), (142.1, 76.2, 36.3), (142.1, 70.6, 45.3), (141.9, 69.2, 58), (141.7, 76.6, 73.1), (141, 78.9, 85.1), (140.6, 84.2, 92.5), (138.5, 76.5, 96.7)],
        // cyan
        [(197, 78.9, 14.9), (196.4, 63.6, 23.7), (194.4, 69.6, 27.1), (192.9, 82.3, 31), (191.6, 91.4, 36.5), (188.7, 94.5, 42.7), (187.9, 85.7, 53.3), (187, 92.4, 69), (186.2, 93.5, 81.8), (185.1, 95.9, 90.4), (183.2, 100, 96.3)],
        // blue
        [(226.2, 57, 21), (224.4, 64.3, 32.9), (225.9, 70.7, 40.2), (224.3, 76.3, 48), (221.2, 83.2, 53.3), (217.2, 91.2, 59.8), (213.1, 93.9, 67.8), (211.7, 96.4, 78.4), (213.3, 96.9, 87.3), (214.3, 94.6, 92.7), (213.8, 100, 96.9)],
        // purple
        [(273.5, 86.9, 21), (273.6, 65.6, 32), (272.9, 67.2, 39.4), (272.1, 71.7, 47.1), (271.5, 81.3, 55.9), (270.7, 91, 65.1), (270, 95.2, 75.3), (269.2, 97.4, 85.1), (268.6, 100, 91.8), (268.7, 100, 95.5), (270, 100, 98)],
        // pink
        [(336.2, 83.9, 17.1), (335.9, 69, 30.4), (335.8, 74.4, 35.3), (335.1, 77.6, 42), (333.3, 71.4, 50.6), (330.4, 81.2, 60.4), (328.6, 85.5, 70.2), (327.4, 87.1, 81.8), (325.9, 84.6, 89.8), (325.7, 77.8, 94.7), (327.3, 73.3, 97.1)],
    ]);

    // theme/color.rs from_hsl_channel: "h s% l%".
    private static IReadOnlyList<IReadOnlyList<GpuiHsla>> Rows((double H, double S, double L)[][] rows) =>
        rows.Select(row => (IReadOnlyList<GpuiHsla>)row.Select(c => GpuiHsla.Hsl((float)c.H, (float)c.S, (float)c.L)).ToArray()).ToArray();
}
