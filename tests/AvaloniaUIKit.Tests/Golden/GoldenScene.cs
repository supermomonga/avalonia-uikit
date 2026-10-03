using System.Text.Json.Nodes;
using Avalonia;
using AvaloniaUIKit.Tests.Infrastructure;

namespace AvaloniaUIKit.Tests.Golden;

/// <summary>A color as GPUI hands it to the shaders: straight, 0..1 floats.</summary>
public readonly record struct Rgba(double R, double G, double B, double A)
{
    public bool IsTransparent => A <= 0.5 / 255;

    public static Rgba From(JsonNode? node) =>
        node is JsonArray a ? new Rgba(a[0]!.GetValue<double>(), a[1]!.GetValue<double>(), a[2]!.GetValue<double>(), a[3]!.GetValue<double>()) : default;

    public static Rgba From(Avalonia.Media.Color c, double opacity = 1) =>
        new(c.R / 255.0, c.G / 255.0, c.B / 255.0, c.A / 255.0 * opacity);

    /// <summary>Largest per-channel difference in 1/255 steps; alpha weighs the color channels.</summary>
    public double Distance(Rgba other)
    {
        var a = Math.Abs(A - other.A);
        if (A <= 0.5 / 255 && other.A <= 0.5 / 255)
        {
            return 0;
        }
        var rgb = Math.Max(Math.Abs(R - other.R), Math.Max(Math.Abs(G - other.G), Math.Abs(B - other.B)));
        return Math.Max(a, rgb * Math.Min(A, other.A)) * 255;
    }

    public override string ToString() =>
        $"#{To8(R):x2}{To8(G):x2}{To8(B):x2}{To8(A):x2}";

    private static int To8(double v) => (int)Math.Round(Math.Clamp(v, 0, 1) * 255);
}

public sealed record SceneQuad(int Order, Rect Bounds, Rect Clip, Rgba Background, bool SolidBackground, Rgba BorderColor, Thickness BorderWidths, CornerRadius Radii, SceneGradient? Gradient = null);

/// <summary>A two-stop linear gradient: its CSS angle (180 runs top to bottom) and the colors at 0 and 1.</summary>
public sealed record SceneGradient(double Angle, Rgba Start, Rgba End);

public sealed record SceneShadow(int Order, Rect Bounds, Rect Clip, CornerRadius Radii, double Sigma, Rgba Color, bool Inset, Rect ElementBounds, CornerRadius ElementRadii);

public sealed record SceneUnderline(int Order, Rect Bounds, double Thickness, Rgba Color);

public sealed record SceneSprite(int Order, Rect Bounds, Rect Clip, Rgba Color, bool Transformed, double RotationDegrees);

/// <summary>A raster image (a polychrome sprite), with its rounded corners.</summary>
public sealed record SceneImage(int Order, Rect Bounds, Rect Clip, CornerRadius Radii, double Opacity);

/// <summary>Everything GPUI painted for one frame, in logical pixels.</summary>
public sealed record GoldenScene(
    IReadOnlyList<SceneQuad> Quads,
    IReadOnlyList<SceneShadow> Shadows,
    IReadOnlyList<SceneUnderline> Underlines,
    IReadOnlyList<SceneSprite> Sprites,
    IReadOnlyList<Rect> Paths,
    IReadOnlyList<Rgba> PathColors,
    IReadOnlyList<SceneImage> Images)
{
    public static GoldenScene Load(string relativePath)
    {
        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(Repo.Goldens, relativePath)))!.AsObject();
        var quads = root["quads"]!.AsArray().Select(n =>
        {
            var q = n!.AsObject();
            var bg = q["background"]!.AsObject();
            var solid = (string)bg["kind"]! == "solid";
            var w = q["border_widths"]!.AsArray();
            var r = q["corner_radii"]!.AsArray();
            SceneGradient? gradient = null;
            if ((string)bg["kind"]! == "linear" && bg["stops"] is JsonArray { Count: 2 } stops &&
                D(stops[0]!["percentage"]) == 0 && D(stops[1]!["percentage"]) == 1)
            {
                gradient = new SceneGradient(D(bg["angle"]), Rgba.From(stops[0]!["rgba"]), Rgba.From(stops[1]!["rgba"]));
            }
            return new SceneQuad(
                (int)q["order"]!.GetValue<double>(),
                GoldenManifest.ReadRect(q["bounds"]),
                GoldenManifest.ReadRect(q["clip"]),
                solid ? Rgba.From(bg["rgba"]) : default,
                solid,
                Rgba.From(q["border_color"]),
                new Thickness(D(w[3]), D(w[0]), D(w[1]), D(w[2])),
                new CornerRadius(D(r[0]), D(r[1]), D(r[2]), D(r[3])),
                gradient);
        }).ToList();
        var shadows = root["shadows"]!.AsArray().Select(n =>
        {
            var s = n!.AsObject();
            var r = s["corner_radii"]!.AsArray();
            return new SceneShadow(
                (int)s["order"]!.GetValue<double>(),
                GoldenManifest.ReadRect(s["bounds"]),
                GoldenManifest.ReadRect(s["clip"]),
                new CornerRadius(D(r[0]), D(r[1]), D(r[2]), D(r[3])),
                D(s["sigma"]),
                Rgba.From(s["color"]),
                s["inset"]!.GetValue<bool>(),
                GoldenManifest.ReadRect(s["element_bounds"]),
                Radii(s["element_corner_radii"]));
        }).ToList();
        var underlines = root["underlines"]!.AsArray().Select(n =>
        {
            var u = n!.AsObject();
            return new SceneUnderline((int)u["order"]!.GetValue<double>(), GoldenManifest.ReadRect(u["bounds"]), D(u["thickness"]), Rgba.From(u["color"]));
        }).ToList();
        var sprites = new List<SceneSprite>();
        foreach (var key in new[] { "mono_sprites", "subpixel_sprites" })
        {
            foreach (var n in root[key]!.AsArray())
            {
                var s = n!.AsObject();
                var rs = s["transform"]!["rotation_scale"]!.AsArray();
                var identity = D(rs[0]![0]) == 1 && D(rs[0]![1]) == 0 && D(rs[1]![0]) == 0 && D(rs[1]![1]) == 1;
                // GPUI's rotation matrix is [[cos, -sin], [sin, cos]] in a y-down space: clockwise.
                var rotation = Math.Atan2(D(rs[1]![0]), D(rs[0]![0])) * 180 / Math.PI;
                sprites.Add(new SceneSprite((int)s["order"]!.GetValue<double>(), GoldenManifest.ReadRect(s["bounds"]), GoldenManifest.ReadRect(s["clip"]), Rgba.From(s["color"]), !identity, rotation));
            }
        }
        var paths = root["paths"]!.AsArray().Select(n => GoldenManifest.ReadRect(n!["bounds"])).ToList();
        var pathColors = root["paths"]!.AsArray().Select(n => Rgba.From(n!["color"]?["rgba"])).ToList();
        var images = root["poly_sprites"]!.AsArray().Select(n => new SceneImage(
            (int)n!["order"]!.GetValue<double>(),
            GoldenManifest.ReadRect(n["bounds"]),
            GoldenManifest.ReadRect(n["clip"]),
            Radii(n["corner_radii"]),
            D(n["opacity"]))).ToList();
        return new GoldenScene(quads, shadows, underlines, sprites, paths, pathColors, images);
    }

    private static double D(JsonNode? n) => n!.GetValue<double>();

    private static CornerRadius Radii(JsonNode? n)
    {
        var r = n!.AsArray();
        return new CornerRadius(D(r[0]), D(r[1]), D(r[2]), D(r[3]));
    }
}
