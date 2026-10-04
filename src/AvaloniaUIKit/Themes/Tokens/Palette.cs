using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace AvaloniaUIKit;

/// <summary>
/// One theme's colors as the generator resolved them with GPUI Kit (Palettes.g.cs): an
/// ARGB value for every color key in <see cref="ColorKeys"/> and every token background
/// in <see cref="FillKeys"/>, and the gradients among them (Aurora).
/// </summary>
/// <remarks>
/// A color key is a brush (<c>UIKit.Primary</c>) and a <see cref="Color"/>
/// (<c>UIKit.Primary.Color</c>). A token background (<c>UIKit.Primary.Fill</c>) is the
/// brush GPUI Kit paints through <c>theme.tokens</c>, a gradient where the theme gives
/// one; the color key stays solid, the gradient's first stop, as GPUI Kit takes it.
/// </remarks>
internal sealed partial class Palette
{
    /// <summary>A two-stop linear gradient for the color or fill at <see cref="Slot"/>.</summary>
    /// <param name="Slot">A color's index, or <see cref="ColorKeys"/>' length plus a fill's index.</param>
    /// <param name="Angle">The CSS angle GPUI takes: 180 runs from the top down, 90 from the left.</param>
    /// <param name="From">The first stop's ARGB color.</param>
    /// <param name="FromOffset">Where the first stop is, from 0 to 1.</param>
    /// <param name="To">The second stop's ARGB color.</param>
    /// <param name="ToOffset">Where the second stop is, from 0 to 1.</param>
    internal readonly record struct Gradient(int Slot, float Angle, uint From, float FromOffset, uint To, float ToOffset);

    private readonly uint[] _colors;
    private readonly uint[] _fills;
    private readonly Dictionary<int, Gradient> _gradients;

    private Palette(string name, bool dark, uint[] colors, uint[] fills, Gradient[] gradients)
    {
        Name = name;
        IsDark = dark;
        _colors = colors;
        _fills = fills;
        _gradients = gradients.ToDictionary(g => g.Slot);
    }

    /// <summary>The theme's name in GPUI Kit (<c>Ayu Dark</c>).</summary>
    public string Name { get; }

    /// <summary>Whether GPUI Kit's theme is a dark one.</summary>
    public bool IsDark { get; }

    /// <summary>How many values a palette holds: a brush and a color per color key, and the fills.</summary>
    internal static int Count => ColorKeys.Length * 2 + FillKeys.Length;

    /// <summary>Where a resource key's value is, or -1 when the palette has no such key.</summary>
    internal static int Slot(object key) =>
        key is string name && Index.Slots.TryGetValue(name, out var slot) ? slot : -1;

    /// <summary>Creates the value of a slot: a brush, or a color.</summary>
    internal object Create(int slot)
    {
        var colors = ColorKeys.Length;
        if (slot < colors)
        {
            return Brush(slot, _colors[slot]);
        }
        if (slot < colors * 2)
        {
            return Color.FromUInt32(_colors[slot - colors]);
        }
        var fill = slot - colors * 2;
        return Brush(colors + fill, _fills[fill]);
    }

    private IBrush Brush(int index, uint color) =>
        _gradients.TryGetValue(index, out var gradient)
            ? LinearGradient(gradient)
            : new ImmutableSolidColorBrush(Color.FromUInt32(color));

    // GPUI's quad shader (shaders.metal fill_color) runs a gradient across the quad's bounds
    // from its start to its end along the angle, interpolating in sRGB and holding the stop
    // colors beyond them. An angle along an axis is the same brush in Avalonia; GPUI scales
    // a diagonal by the quad's aspect, which relative points only approach.
    private static ImmutableLinearGradientBrush LinearGradient(Gradient gradient)
    {
        var radians = (gradient.Angle - 90) * Math.PI / 180;
        var dx = Math.Round(Math.Cos(radians), 6) / 2;
        var dy = Math.Round(Math.Sin(radians), 6) / 2;
        return new ImmutableLinearGradientBrush(
            [
                new ImmutableGradientStop(gradient.FromOffset, Color.FromUInt32(gradient.From)),
                new ImmutableGradientStop(gradient.ToOffset, Color.FromUInt32(gradient.To)),
            ],
            startPoint: new RelativePoint(0.5 - dx, 0.5 - dy, RelativeUnit.Relative),
            endPoint: new RelativePoint(0.5 + dx, 0.5 + dy, RelativeUnit.Relative));
    }

    // Brushes, then colors, then fills: where a key's value is in a palette's values. Built on
    // first use, after the generated keys (another file, whose initializers may run later).
    private static class Index
    {
        internal static readonly Dictionary<string, int> Slots = BuildSlots();
    }

    private static Dictionary<string, int> BuildSlots()
    {
        var slots = new Dictionary<string, int>(Count, StringComparer.Ordinal);
        for (var i = 0; i < ColorKeys.Length; i++)
        {
            slots.Add(ColorKeys[i], i);
            slots.Add(ColorKeys[i] + ".Color", ColorKeys.Length + i);
        }
        for (var i = 0; i < FillKeys.Length; i++)
        {
            slots.Add(FillKeys[i], ColorKeys.Length * 2 + i);
        }
        return slots;
    }
}
