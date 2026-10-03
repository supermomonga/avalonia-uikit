using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>The brush properties a <see cref="ColorTransition"/> animates.</summary>
[Flags]
public enum BrushProperties
{
    /// <summary>None.</summary>
    None = 0,
    /// <summary>Background.</summary>
    Background = 1,
    /// <summary>BorderBrush.</summary>
    BorderBrush = 2,
    /// <summary>Foreground.</summary>
    Foreground = 4,
}

/// <summary>
/// A color transition as GPUI runs one (gpui_base::transition over Hsla): each
/// HSLA channel moves linearly along the eased progress. Avalonia's own brush
/// transitions blend in linear-light RGB, which takes a different path between
/// two colors. Set through <see cref="Motion.ColorTransitionProperty"/> on a
/// template part whose brushes are bound to its templated parent's: when the
/// parent's brush changes, the part shows the GPUI path to it.
/// </summary>
public sealed class ColorTransition
{
    /// <summary>How long the transition runs.</summary>
    public TimeSpan Duration { get; set; }

    /// <summary>The easing of the progress.</summary>
    public Easing Easing { get; set; } = new LinearEasing();

    /// <summary>The brushes that transition.</summary>
    public BrushProperties Properties { get; set; }

    // Dense keyframes: Avalonia blends between neighbours in linear light, which
    // over a 1/32 step stays within a fraction of an 8-bit step of the HSLA path.
    private const int Steps = 32;

    internal Animation Build(AvaloniaProperty property, Color from, Color to)
    {
        var animation = new Animation { Duration = Duration };
        var a = Hsla.From(from);
        var b = Hsla.From(to);
        for (var i = 0; i <= Steps; i++)
        {
            var cue = (double)i / Steps;
            var color = Hsla.Lerp(a, b, Easing.Ease(cue)).ToColor();
            animation.Children.Add(new KeyFrame
            {
                Cue = new Cue(cue),
                Setters = { new Setter(property, new ImmutableSolidColorBrush(color)) },
            });
        }
        return animation;
    }

    /// <summary>GPUI's Hsla, with its conversions (gpui color.rs).</summary>
    private readonly record struct Hsla(double H, double S, double L, double A)
    {
        public static Hsla From(Color color)
        {
            double r = color.R / 255.0, g = color.G / 255.0, b = color.B / 255.0;
            var max = Math.Max(r, Math.Max(g, b));
            var min = Math.Min(r, Math.Min(g, b));
            var delta = max - min;
            var l = (max + min) / 2;
            var s = l == 0 || l == 1 ? 0 : l < 0.5 ? delta / (2 * l) : delta / (2 - 2 * l);
            var h = delta == 0 ? 0
                : max == r ? ((((g - b) / delta) % 6 + 6) % 6) / 6
                : max == g ? ((b - r) / delta + 2) / 6
                : ((r - g) / delta + 4) / 6;
            return new Hsla(h, s, l, color.A / 255.0);
        }

        public static Hsla Lerp(Hsla x, Hsla y, double t) =>
            new(x.H + (y.H - x.H) * t, x.S + (y.S - x.S) * t, x.L + (y.L - x.L) * t, x.A + (y.A - x.A) * t);

        public Color ToColor()
        {
            var c = (1 - Math.Abs(2 * L - 1)) * S;
            var x = c * (1 - Math.Abs(H * 6 % 2 - 1));
            var m = L - c / 2;
            var (r, g, b) = (int)Math.Floor(H * 6) switch
            {
                0 or 6 => (c + m, x + m, m),
                1 => (x + m, c + m, m),
                2 => (m, c + m, x + m),
                3 => (m, x + m, c + m),
                4 => (x + m, m, c + m),
                _ => (c + m, m, x + m),
            };
            static byte To8(double v) => (byte)Math.Round(Math.Clamp(v, 0, 1) * 255);
            return Color.FromArgb(To8(A), To8(r), To8(g), To8(b));
        }
    }
}
