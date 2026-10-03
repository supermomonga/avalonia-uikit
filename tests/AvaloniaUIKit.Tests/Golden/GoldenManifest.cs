using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Avalonia;
using AvaloniaUIKit.Tests.Infrastructure;

namespace AvaloniaUIKit.Tests.Golden;

/// <summary>One frame of a recorded motion.</summary>
public sealed record GoldenFrame(int TimeMs, string Png, string Scene);

/// <summary>A state change GPUI recorded frame by frame.</summary>
public sealed record GoldenMotion(string Name, string Trigger, string From, IReadOnlyList<GoldenFrame> Frames);

/// <summary>
/// One rendered GPUI Kit case from <c>goldens/.../manifest.json</c>: what was
/// built, the state it was put in, and where its image and scene are.
/// </summary>
public sealed record GoldenCase(
    string Id,
    string Component,
    string? Group,
    JsonObject Params,
    string State,
    string Theme,
    Size Viewport,
    Point Anchor,
    Rect ComponentBounds,
    string? Png,
    string? Scene,
    GoldenMotion? Motion)
{
    public bool IsDark => Theme == "dark";

    public string Str(string key, string fallback = "") =>
        Params.TryGetPropertyValue(key, out var node) && node is JsonValue v && v.TryGetValue<string>(out var s) ? s : fallback;

    public bool Bool(string key) =>
        Params.TryGetPropertyValue(key, out var node) && node is JsonValue v && v.TryGetValue<bool>(out var b) && b;

    public double Num(string key, double fallback) =>
        Params.TryGetPropertyValue(key, out var node) && node is JsonValue v && v.TryGetValue<double>(out var d) ? d : fallback;

    public bool Has(string key) => Params.ContainsKey(key);

    public override string ToString() => Id;
}

public static class GoldenManifest
{
    private static readonly Lazy<IReadOnlyList<GoldenCase>> AllCases = new(Load);

    public static IReadOnlyList<GoldenCase> All => AllCases.Value;

    /// <summary>Static cases of a component, as a TUnit data source.</summary>
    public static IEnumerable<Func<GoldenCase>> Cases(string component) =>
        All.Where(c => c.Component == component && c.Motion is null).Select(c => (Func<GoldenCase>)(() => c));

    /// <summary>Motion cases of a component, as a TUnit data source.</summary>
    public static IEnumerable<Func<GoldenCase>> Motions(string component) =>
        All.Where(c => c.Component == component && c.Motion is not null).Select(c => (Func<GoldenCase>)(() => c));

    public static GoldenCase Get(string id) =>
        All.FirstOrDefault(c => c.Id == id) ?? throw new KeyNotFoundException($"no golden case {id}");

    private static IReadOnlyList<GoldenCase> Load()
    {
        var path = Path.Combine(Repo.Goldens, "manifest.json");
        var root = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        var cases = new List<GoldenCase>();
        foreach (var node in root["cases"]!.AsArray())
        {
            var o = node!.AsObject();
            cases.Add(new GoldenCase(
                Id: (string)o["id"]!,
                Component: (string)o["component"]!,
                Group: (string?)o["group"],
                Params: o["params"]!.AsObject(),
                State: (string)o["state"]!,
                Theme: (string)o["theme"]!,
                Viewport: new Size(Num(o["viewport"]![0]), Num(o["viewport"]![1])),
                Anchor: new Point(Num(o["anchor"]![0]), Num(o["anchor"]![1])),
                ComponentBounds: ReadRect(o["component_bounds"]),
                Png: (string?)o["png"],
                Scene: (string?)o["scene"],
                Motion: ReadMotion(o["motion"])));
        }
        return cases;
    }

    private static double Num(JsonNode? node) => node!.GetValue<double>();

    internal static Rect ReadRect(JsonNode? node) =>
        node is JsonArray a ? new Rect(Num(a[0]), Num(a[1]), Num(a[2]), Num(a[3])) : default;

    private static GoldenMotion? ReadMotion(JsonNode? node)
    {
        if (node is not JsonObject o)
        {
            return null;
        }
        var frames = o["frames"]!.AsArray()
            .Select(f => new GoldenFrame((int)f!["t_ms"]!.GetValue<double>(), (string)f["png"]!, (string)f["scene"]!))
            .ToList();
        return new GoldenMotion((string)o["name"]!, (string)o["trigger"]!, (string)o["from"]!, frames);
    }

    /// <summary>The resolved theme tokens GPUI Kit dumped, by mode.</summary>
    public static JsonObject Tokens(string mode)
    {
        var path = Path.Combine(Repo.Goldens, "tokens", "gpui-theme.json");
        return JsonNode.Parse(File.ReadAllText(path))![mode]!.AsObject();
    }

    internal static string Format(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
}
