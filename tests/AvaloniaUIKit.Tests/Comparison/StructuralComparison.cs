using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;

namespace AvaloniaUIKit.Tests.Comparison;

public enum PrimitiveKind
{
    /// <summary>A filled rounded rectangle.</summary>
    Fill,
    /// <summary>A border band: the area between a rounded rectangle and its inset by the border widths.</summary>
    Band,
    /// <summary>A gaussian shadow.</summary>
    Shadow,
    /// <summary>A raster image where it is drawn (its color is not compared).</summary>
    Image,
    /// <summary>A filled rounded rectangle with a two-stop linear gradient: Color to EndColor at Angle.</summary>
    Gradient,
}

/// <summary>
/// A painted shape reduced to what both renderers agree on: geometry in
/// logical pixels with the outer corner radii, and a straight-alpha color.
/// </summary>
public sealed record Primitive(PrimitiveKind Kind, Rect Bounds, CornerRadius Radii, Thickness Widths, double Sigma, Rgba Color, string Source)
{
    /// <summary>Cut by a clipping ancestor: its outline there is the clip's, not its own (R19).</summary>
    public bool Clipped { get; init; }

    /// <summary>A gradient's color at its end (Color is its start).</summary>
    public Rgba EndColor { get; init; }

    /// <summary>A gradient's direction, in CSS degrees (0 runs bottom to top, 180 top to bottom).</summary>
    public double Angle { get; init; }

    public override string ToString()
    {
        var text = FormattableString.Invariant($"{Kind} {VisualAssert.Fmt(Bounds)} r=({Radii.TopLeft:0.##},{Radii.TopRight:0.##},{Radii.BottomRight:0.##},{Radii.BottomLeft:0.##})");
        if (Kind == PrimitiveKind.Band)
        {
            text += FormattableString.Invariant($" w=({Widths.Top:0.##},{Widths.Right:0.##},{Widths.Bottom:0.##},{Widths.Left:0.##})");
        }
        if (Kind == PrimitiveKind.Shadow)
        {
            text += FormattableString.Invariant($" sigma={Sigma:0.###}");
        }
        if (Kind == PrimitiveKind.Gradient)
        {
            text += FormattableString.Invariant($" {Angle:0.#}deg to {EndColor}");
        }
        return text + $" {Color} [{Source}]";
    }
}

/// <summary>
/// Compares what GPUI painted (its scene) with what the Avalonia visual tree
/// will paint, primitive by primitive. Unlike pixels this is exact: colors to
/// one 8-bit step (R11) and geometry to half a logical pixel at rounding ties (R9).
/// </summary>
public static class StructuralComparison
{
    public const double ColorTolerance = 1.01; // 1/255 steps
    public const double GeometryTolerance = 0.26;

    public static IReadOnlyList<Primitive> FromScene(GoldenScene scene)
    {
        var list = new List<Primitive>();
        foreach (var s in scene.Shadows.Where(s => !s.Color.IsTransparent && !s.Inset))
        {
            // An unblurred shadow is a crisp filled rounded rectangle (the popover ring).
            list.Add(s.Sigma <= 0
                ? new Primitive(PrimitiveKind.Fill, s.Bounds, Clamp(s.Radii, s.Bounds), default, 0, s.Color, $"shadow#{s.Order}")
                : new Primitive(PrimitiveKind.Shadow, s.Bounds, s.Radii, default, s.Sigma, s.Color, $"shadow#{s.Order}"));
        }
        foreach (var q in scene.Quads)
        {
            if (q.SolidBackground && !q.Background.IsTransparent)
            {
                list.Add(new Primitive(PrimitiveKind.Fill, q.Bounds, Clamp(q.Radii, q.Bounds), default, 0, q.Background, $"quad#{q.Order}"));
            }
            if (q.Gradient is { } g && !(g.Start.IsTransparent && g.End.IsTransparent))
            {
                list.Add(new Primitive(PrimitiveKind.Gradient, q.Bounds, Clamp(q.Radii, q.Bounds), default, 0, g.Start, $"quad#{q.Order}")
                {
                    EndColor = g.End,
                    Angle = g.Angle,
                });
            }
            if (q.BorderWidths != default && !q.BorderColor.IsTransparent && !FadedHairline(q))
            {
                list.Add(Band(q.Bounds, Clamp(q.Radii, q.Bounds), q.BorderWidths, q.BorderColor, $"quad#{q.Order}"));
            }
        }
        foreach (var image in scene.Images.Where(i => i.Opacity > 0))
        {
            var drawn = image.Bounds.Intersect(image.Clip);
            if (drawn.Width > 0 && drawn.Height > 0)
            {
                list.Add(new Primitive(PrimitiveKind.Image, drawn, Clamp(image.Radii, drawn), default, 0, default, $"image#{image.Order}"));
            }
        }
        return Dedupe(list);
    }

    // R30: GPUI widens any non-zero stroke to one device pixel (snap_stroke); Avalonia's
    // layout rounding drops a stroke under half a device pixel. A ring fading in or out
    // (the slider's) is then under 5% alpha, so only the pixels compare it.
    private static bool FadedHairline(SceneQuad q)
    {
        var w = q.BorderWidths;
        var hairline = 1 / CaseHost.Scale;
        return q.BorderColor.A < 0.05 && new[] { w.Left, w.Top, w.Right, w.Bottom }.All(e => e == 0 || Math.Abs(e - hairline) < 1e-6);
    }

    // Where an Image draws: its source scaled as Image.Render scales it, centered, within its bounds.
    private static Rect DrawnRect(Image image)
    {
        if (image.Source is not { } source)
        {
            return default;
        }
        var viewport = new Rect(image.Bounds.Size);
        var scale = image.Stretch.CalculateScaling(image.Bounds.Size, source.Size, image.StretchDirection);
        return viewport.CenterRect(new Rect(source.Size * scale)).Intersect(viewport);
    }

    public static IReadOnlyList<Primitive> FromVisuals(Visual root)
    {
        var list = new List<Primitive>();
        foreach (var visual in root.GetSelfAndVisualDescendants())
        {
            if (!visual.IsEffectivelyVisible || visual is not Control control)
            {
                continue;
            }
            var opacity = EffectiveOpacity(visual, root);
            if (opacity <= 0)
            {
                continue;
            }
            var origin = visual.TranslatePoint(default, root);
            if (origin is null)
            {
                continue;
            }
            var rect = new Rect(origin.Value, visual.Bounds.Size);
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                continue;
            }
            var clip = ClipOf(visual, root);
            var name = string.IsNullOrEmpty(control.Name) ? control.GetType().Name : $"{control.GetType().Name}#{control.Name}";
            var before = list.Count;
            switch (visual)
            {
                case Border b:
                    AddBox(list, rect, b.Background, b.BorderBrush, Painted(b, b.BorderThickness), b.CornerRadius, b.BackgroundSizing, b.BoxShadow, opacity, name);
                    break;
                case ContentPresenter p:
                    AddBox(list, rect, p.Background, p.BorderBrush, Painted(p, p.BorderThickness), p.CornerRadius, p.BackgroundSizing, p.BoxShadow, opacity, name);
                    break;
                case Panel panel when panel.Background is not null:
                    AddBox(list, rect, panel.Background, null, default, default, BackgroundSizing.CenterBorder, default, opacity, name);
                    break;
                case Image image when DrawnRect(image) is { Width: > 0, Height: > 0 } drawn:
                    var radii = image.Clip is RectangleGeometry { RadiusX: var rx } && rx > 0 ? new CornerRadius(rx) : default;
                    var drawnRect = new Rect(rect.Position + drawn.Position, drawn.Size);
                    list.Add(new Primitive(PrimitiveKind.Image, drawnRect, Clamp(radii, drawnRect), default, 0, default, name));
                    break;
                case Rectangle r when r.Fill is not null:
                    AddBox(list, rect, r.Fill, null, default, new CornerRadius(r.RadiusX), BackgroundSizing.CenterBorder, default, opacity, name);
                    break;
            }
            if (clip is { } c)
            {
                for (var i = before; i < list.Count; i++)
                {
                    var p = list[i];
                    var cut = p.Bounds.Intersect(c);
                    if (cut != p.Bounds)
                    {
                        list[i] = p with { Bounds = cut, Radii = Clamp(p.Radii, cut), Clipped = true };
                    }
                }
                list.RemoveAll(p => p.Bounds.Width <= 0 || p.Bounds.Height <= 0);
            }
            // Wholly clipped away (a closed reveal), it paints nothing: GPUI culls it.
            if (ClipOf(visual, root, borders: false) is { } all)
            {
                for (var i = list.Count - 1; i >= before; i--)
                {
                    if (!list[i].Bounds.Intersects(all))
                    {
                        list.RemoveAt(i);
                    }
                }
            }
        }
        return Dedupe(list);
    }

    // Borders paint their thickness snapped to device pixels when they round layout.
    private static Thickness Painted(Avalonia.Layout.Layoutable element, Thickness thickness) =>
        element.UseLayoutRounding ? Avalonia.Layout.LayoutHelper.RoundLayoutThickness(thickness, CaseHost.Scale) : thickness;

    private static void AddBox(List<Primitive> list, Rect rect, IBrush? background, IBrush? borderBrush, Thickness t,
        CornerRadius cr, BackgroundSizing sizing, BoxShadows shadows, double opacity, string name)
    {
        var uniform = t.Left == t.Top && t.Top == t.Right && t.Right == t.Bottom;
        var simple = uniform && sizing == BackgroundSizing.CenterBorder;
        // Corner radii in Avalonia sit on the middle of the border (WinUI).
        var outer = new CornerRadius(
            cr.TopLeft == 0 ? 0 : cr.TopLeft + Math.Max(t.Left, t.Top) / 2,
            cr.TopRight == 0 ? 0 : cr.TopRight + Math.Max(t.Right, t.Top) / 2,
            cr.BottomRight == 0 ? 0 : cr.BottomRight + Math.Max(t.Right, t.Bottom) / 2,
            cr.BottomLeft == 0 ? 0 : cr.BottomLeft + Math.Max(t.Left, t.Bottom) / 2);
        if (simple && t.Top > 0)
        {
            // DrawRectangle with a pen: the stroke is centered on the rect deflated by t/2.
            outer = new CornerRadius(cr.TopLeft + t.Top / 2, cr.TopRight + t.Top / 2, cr.BottomRight + t.Top / 2, cr.BottomLeft + t.Top / 2);
            if (cr.TopLeft == 0 && cr.TopRight == 0 && cr.BottomRight == 0 && cr.BottomLeft == 0)
            {
                outer = default;
            }
        }
        if (simple && t.Top == 0)
        {
            outer = cr;
        }

        var gradient = Gradient(background, opacity);
        if ((gradient?.Start ?? Solid(background, opacity)) is { } fill && !(fill.IsTransparent && (gradient?.End.IsTransparent ?? true)))
        {
            Rect fillRect = rect;
            CornerRadius fillRadii = outer;
            if (simple && t.Top > 0)
            {
                // The simple path fills up to the middle of the stroke.
                fillRect = rect.Deflate(t.Top / 2);
                fillRadii = cr;
            }
            else if (sizing == BackgroundSizing.InnerBorderEdge)
            {
                fillRect = rect.Deflate(t);
                fillRadii = new CornerRadius(
                    Math.Max(0, cr.TopLeft - Math.Max(t.Left, t.Top) / 2),
                    Math.Max(0, cr.TopRight - Math.Max(t.Right, t.Top) / 2),
                    Math.Max(0, cr.BottomRight - Math.Max(t.Right, t.Bottom) / 2),
                    Math.Max(0, cr.BottomLeft - Math.Max(t.Left, t.Bottom) / 2));
            }
            list.Add(gradient is { } g
                ? new Primitive(PrimitiveKind.Gradient, fillRect, Clamp(fillRadii, fillRect), default, 0, g.Start, name) { EndColor = g.End, Angle = g.Angle }
                : new Primitive(PrimitiveKind.Fill, fillRect, Clamp(fillRadii, fillRect), default, 0, fill, name));
        }
        if (t != default && Solid(borderBrush, opacity) is { } band && !band.IsTransparent)
        {
            list.Add(Band(rect, Clamp(outer, rect), t, band, name));
        }
        foreach (var s in shadows)
        {
            if (s.IsInset || s.Color.A == 0)
            {
                continue;
            }
            // Skia: sigma = 0.57735 * radius + 0.5 with radius = blur / 2 (0 stays 0).
            var sigma = s.Blur > 0 ? 0.288675 * s.Blur + 0.5 : 0;
            var shadowRect = (simple && t.Top > 0 ? rect.Deflate(t.Top / 2) : rect).Translate(new Vector(s.OffsetX, s.OffsetY)).Inflate(s.Spread);
            var baseRadii = simple && t.Top > 0 ? cr : outer;
            var radii = new CornerRadius(
                Math.Max(0, baseRadii.TopLeft + s.Spread), Math.Max(0, baseRadii.TopRight + s.Spread),
                Math.Max(0, baseRadii.BottomRight + s.Spread), Math.Max(0, baseRadii.BottomLeft + s.Spread));
            // A shadow faded below half an 8-bit step paints nothing (GPUI skips it).
            if (Rgba.From(s.Color, opacity) is { IsTransparent: false } color)
            {
                list.Add(new Primitive(PrimitiveKind.Shadow, shadowRect, Clamp(radii, shadowRect), default, sigma, color, name));
            }
        }
    }

    /// <summary>A border band, or a fill when the border leaves no inside (a 2px line drawn as a 2px border).</summary>
    private static Primitive Band(Rect bounds, CornerRadius radii, Thickness widths, Rgba color, string source)
    {
        var inner = bounds.Deflate(widths);
        return inner.Width <= 0.001 || inner.Height <= 0.001
            ? new Primitive(PrimitiveKind.Fill, bounds, radii, default, 0, color, source)
            : new Primitive(PrimitiveKind.Band, bounds, radii, widths, 0, color, source);
    }

    /// <summary>
    /// The intersection of the clipping ancestors' bounds (within the root), if
    /// any: the Borders' only (whose rounded clips cut outlines, R19) or all.
    /// </summary>
    private static Rect? ClipOf(Visual visual, Visual root, bool borders = true)
    {
        Rect? clip = null;
        for (var v = visual.GetVisualParent(); v is not null && v != root; v = v.GetVisualParent())
        {
            if (v.ClipToBounds && (v is Border || !borders) && v.TranslatePoint(default, root) is { } origin)
            {
                var r = new Rect(origin, v.Bounds.Size);
                clip = clip is { } c ? c.Intersect(r) : r;
            }
        }
        return clip;
    }

    private static Rgba? Solid(IBrush? brush, double opacity) =>
        brush is ISolidColorBrush s ? Rgba.From(s.Color, s.Opacity * opacity) : null;

    // A two-stop linear gradient across the box (relative points), as GPUI's linear_gradient.
    private static SceneGradient? Gradient(IBrush? brush, double opacity)
    {
        if (brush is not ILinearGradientBrush { GradientStops: [{ Offset: 0 } first, { Offset: 1 } last] } linear ||
            linear.StartPoint.Unit != RelativeUnit.Relative || linear.EndPoint.Unit != RelativeUnit.Relative)
        {
            return null;
        }
        var dx = linear.EndPoint.Point.X - linear.StartPoint.Point.X;
        var dy = linear.EndPoint.Point.Y - linear.StartPoint.Point.Y;
        var angle = (Math.Atan2(dx, -dy) * 180 / Math.PI + 360) % 360;
        return new SceneGradient(angle, Rgba.From(first.Color, linear.Opacity * opacity), Rgba.From(last.Color, linear.Opacity * opacity));
    }

    private static double EffectiveOpacity(Visual visual, Visual root)
    {
        var opacity = 1.0;
        for (Visual? v = visual; v is not null; v = v.GetVisualParent())
        {
            opacity *= v.Opacity;
            if (v == root)
            {
                break;
            }
        }
        return opacity;
    }

    /// <summary>Radii larger than half the shorter side render as half the shorter side in both renderers.</summary>
    private static CornerRadius Clamp(CornerRadius r, Rect rect)
    {
        var max = Math.Min(rect.Width, rect.Height) / 2;
        return new CornerRadius(Math.Min(r.TopLeft, max), Math.Min(r.TopRight, max), Math.Min(r.BottomRight, max), Math.Min(r.BottomLeft, max));
    }

    private static IReadOnlyList<Primitive> Dedupe(List<Primitive> list)
    {
        var result = new List<Primitive>();
        foreach (var p in list.Select(Strip))
        {
            if (!result.Any(r => Same(r, p, 0.001, 0.01)))
            {
                result.Add(p);
            }
        }
        return result;
    }

    /// <summary>
    /// A square box's border on one side only paints a strip: GPUI's row rule (a
    /// quad's bottom border) and DataGrid's grid line (a 1px rectangle) alike.
    /// </summary>
    private static Primitive Strip(Primitive p)
    {
        var w = p.Widths;
        if (p.Kind != PrimitiveKind.Band || p.Radii != default || new[] { w.Left, w.Top, w.Right, w.Bottom }.Count(e => e > 0) != 1)
        {
            return p;
        }
        var b = p.Bounds;
        var strip = w.Left > 0 ? new Rect(b.X, b.Y, w.Left, b.Height)
            : w.Top > 0 ? new Rect(b.X, b.Y, b.Width, w.Top)
            : w.Right > 0 ? new Rect(b.Right - w.Right, b.Y, w.Right, b.Height)
            : new Rect(b.X, b.Bottom - w.Bottom, b.Width, w.Bottom);
        return p with { Kind = PrimitiveKind.Fill, Bounds = strip, Widths = default };
    }

    public static bool Same(Primitive a, Primitive b, double geometry, double color) =>
        a.Kind == b.Kind &&
        Near(a.Bounds.X, b.Bounds.X, geometry) && Near(a.Bounds.Y, b.Bounds.Y, geometry) &&
        Near(a.Bounds.Width, b.Bounds.Width, geometry) && Near(a.Bounds.Height, b.Bounds.Height, geometry) &&
        SameShape(a, b, geometry, color);

    /// <summary>
    /// Like <see cref="Same"/>, but lets a box be narrower in Avalonia by less than
    /// one logical pixel, and shifted horizontally by less than one: GPUI rounds
    /// text widths up to whole logical pixels, Avalonia to device pixels (R9).
    /// </summary>
    public static bool SameWithTextRounding(Primitive gpui, Primitive avalonia, double geometry, double color)
    {
        var dw = gpui.Bounds.Width - avalonia.Bounds.Width;
        var dx = gpui.Bounds.X - avalonia.Bounds.X;
        return gpui.Kind == avalonia.Kind &&
            dw >= -geometry && dw < 1 &&
            // Boxes after (or around) text shift by up to the text's rounding difference.
            dx > -0.5 - geometry && dx < 1 &&
            Near(gpui.Bounds.Y, avalonia.Bounds.Y, geometry) &&
            Near(gpui.Bounds.Height, avalonia.Bounds.Height, geometry) &&
            SameShape(gpui, avalonia, geometry, color);
    }

    private static bool SameShape(Primitive a, Primitive b, double geometry, double color) =>
        // A spread shadow keeps the element's radius in GPUI and grows it by the
        // spread in Skia (R3); a popover's spreads are at most 2px. A clipped
        // shape's corners are the clip's (R19).
        SameRadii(a, b, a.Kind == PrimitiveKind.Shadow ? 2.01 : (a.Clipped || b.Clipped) ? Math.Max(a.Radii.TopLeft, b.Radii.TopLeft) + 0.01 : geometry) &&
        Near(a.Widths.Left, b.Widths.Left, 0.001) && Near(a.Widths.Top, b.Widths.Top, 0.001) &&
        Near(a.Widths.Right, b.Widths.Right, 0.001) && Near(a.Widths.Bottom, b.Widths.Bottom, 0.001) &&
        Near(a.Sigma, b.Sigma, 0.02) &&
        a.Color.Distance(b.Color) <= color &&
        (a.Kind != PrimitiveKind.Gradient || (a.EndColor.Distance(b.EndColor) <= color && Near(a.Angle, b.Angle, 0.5)));

    private static bool SameRadii(Primitive a, Primitive b, double tolerance) =>
        Near(a.Radii.TopLeft, b.Radii.TopLeft, tolerance) && Near(a.Radii.TopRight, b.Radii.TopRight, tolerance) &&
        Near(a.Radii.BottomRight, b.Radii.BottomRight, tolerance) && Near(a.Radii.BottomLeft, b.Radii.BottomLeft, tolerance);

    private static bool Near(double a, double b, double tolerance) => Math.Abs(a - b) <= tolerance;

    // The text's line boxes, each cut to the font's ascent and descent about its middle.
    private static Rect TextInk(TextBlock text)
    {
        var typeface = new Avalonia.Media.Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch);
        var glyphs = Avalonia.Media.FontManager.Current.TryGetGlyphTypeface(typeface, out var face)
            ? (face.Metrics.Descent - face.Metrics.Ascent) * text.FontSize / face.Metrics.DesignEmHeight
            : text.FontSize * 1.2;
        Rect? ink = null;
        foreach (var line in text.TextLayout.HitTestTextRange(0, text.Text!.Length))
        {
            var band = new Rect(line.X + text.Padding.Left, line.Center.Y - glyphs / 2 + text.Padding.Top, line.Width, glyphs);
            ink = ink is { } union ? union.Union(band) : band;
        }
        return ink ?? new Rect(text.Bounds.Size);
    }

    /// <summary>
    /// The colors of text, icons and paths: GPUI's glyph and icon sprites and its
    /// paths against the foregrounds of Avalonia's text blocks and selections and
    /// the brushes of its shapes.
    /// </summary>
    public static List<string> CompareInk(GoldenScene scene, Visual root)
    {
        var expected = Distinct(scene.Sprites.Where(s => !s.Color.IsTransparent).Select(s => s.Color)
            .Concat(scene.Underlines.Where(u => !u.Color.IsTransparent).Select(u => u.Color))
            .Concat(scene.PathColors.Where(c => !c.IsTransparent)));
        var actual = new List<Rgba>();
        // A two-tone icon is one sprite in GPUI, its faint half inside the mask; in
        // Avalonia it is a second icon at the half's opacity, in the sprite's color.
        var layers = new List<Rgba>();
        foreach (var visual in root.GetSelfAndVisualDescendants())
        {
            // A shape of no width or height still strokes a line (a dashed separator).
            if (!visual.IsEffectivelyVisible || (visual is not Shape && (visual.Bounds.Width <= 0 || visual.Bounds.Height <= 0)))
            {
                continue;
            }
            // Off the window it paints nothing (GPUI culls its sprites). A text's
            // glyphs fill less than its block: its line boxes are as wide as the text
            // and taller than the glyphs, which GPUI culls by their own bounds.
            var painted = visual is TextBlock { Text.Length: > 0 } text ? TextInk(text) : new Rect(visual.Bounds.Size);
            if (visual.TransformToVisual(root) is { } toRoot && !painted.TransformToAABB(toRoot).Inflate(1).Intersects(new Rect(root.Bounds.Size)))
            {
                continue;
            }
            var opacity = EffectiveOpacity(visual, root);
            switch (visual)
            {
                case TextBlock t when t.Inlines is { Count: > 0 } inlines:
                    foreach (var run in inlines.OfType<Avalonia.Controls.Documents.Run>())
                    {
                        if (!string.IsNullOrEmpty(run.Text?.Trim()) && Solid(run.Foreground, opacity) is { } runColor)
                        {
                            actual.Add(runColor);
                        }
                    }
                    break;
                case TextBlock t when !string.IsNullOrEmpty(t.Text) && Solid(t.Foreground, opacity) is { } c:
                    actual.Add(c);
                    foreach (var decoration in t.TextDecorations ?? [])
                    {
                        actual.Add(Solid(decoration.Stroke, opacity) ?? c);
                    }
                    break;
                case TextPresenter p when !string.IsNullOrEmpty(p.Text) && Solid(p.Foreground, opacity) is { } c:
                    actual.Add(c);
                    // GPUI paints a selection as a path.
                    if (p.SelectionStart != p.SelectionEnd && Solid(p.SelectionBrush, opacity) is { } selection)
                    {
                        actual.Add(selection);
                    }
                    break;
                // A shape whose geometry is empty (an arc of no sweep) paints nothing. A
                // Rectangle is a box (a DataGrid grid line): the structure compares it.
                case Shape shape when shape is not Rectangle && shape.RenderedGeometry is { Bounds: var g } && g.Width + g.Height > 0:
                    var inks = shape.TemplatedParent is PathIcon { Opacity: < 1 } ? layers : actual;
                    if (Solid(shape.Stroke, opacity) is { } stroke && shape.StrokeThickness > 0)
                    {
                        inks.Add(stroke);
                    }
                    if (Solid(shape.Fill, opacity) is { } fill)
                    {
                        inks.Add(fill);
                    }
                    break;
            }
        }
        // R30: ink fading below 5% is compared by pixels only (GPUI culls some of it).
        expected = expected.Where(c => c.A >= 0.05).ToList();
        actual = Distinct(actual.Where(c => !c.IsTransparent && c.A >= 0.05));
        foreach (var layer in layers.Where(c => !c.IsTransparent && c.A >= 0.05))
        {
            // Matched as is, or as the faint half of a stronger sprite of its color.
            var faintHalf = !expected.Any(e => layer.Distance(e) <= ColorTolerance) &&
                expected.Any(e => (layer with { A = e.A }).Distance(e) <= ColorTolerance && layer.A < e.A);
            if (!faintHalf)
            {
                actual.Add(layer);
            }
        }
        var messages = new List<string>();
        foreach (var e in expected.Where(e => !actual.Any(a => a.Distance(e) <= ColorTolerance)))
        {
            messages.Add($"no text or icon in {e} (Avalonia ink: {string.Join(", ", actual)})");
        }
        foreach (var a in actual.Where(a => !expected.Any(e => a.Distance(e) <= ColorTolerance)))
        {
            messages.Add($"text or icon in {a} that GPUI does not paint (GPUI ink: {string.Join(", ", expected)})");
        }
        return messages;
    }

    private static List<Rgba> Distinct(IEnumerable<Rgba> colors)
    {
        var result = new List<Rgba>();
        foreach (var c in colors)
        {
            if (!result.Any(r => r.Distance(c) <= 0.5))
            {
                result.Add(c);
            }
        }
        return result;
    }

    /// <summary>Returns one message per primitive that only one side paints.</summary>
    public static List<string> Compare(GoldenScene scene, IReadOnlyList<Primitive> avalonia, double geometryTolerance = GeometryTolerance)
    {
        var expected = FromScene(scene).ToList();
        var actual = avalonia.ToList();
        var messages = new List<string>();
        foreach (var e in expected)
        {
            var match = actual.FirstOrDefault(a => Same(e, a, geometryTolerance, ColorTolerance))
                ?? actual.FirstOrDefault(a => SameWithTextRounding(e, a, geometryTolerance, ColorTolerance));
            if (match is not null)
            {
                actual.Remove(match);
            }
            else
            {
                var nearest = actual.Where(a => a.Kind == e.Kind)
                    .OrderBy(a => Math.Abs(a.Bounds.X - e.Bounds.X) + Math.Abs(a.Bounds.Y - e.Bounds.Y) + Math.Abs(a.Bounds.Width - e.Bounds.Width) + Math.Abs(a.Bounds.Height - e.Bounds.Height))
                    .FirstOrDefault();
                messages.Add($"missing {e}" + (nearest is null ? "" : $"\n      nearest {nearest}"));
            }
        }
        foreach (var a in actual)
        {
            messages.Add($"extra {a}");
        }
        return messages;
    }
}
