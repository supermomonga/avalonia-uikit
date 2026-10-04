using System.Globalization;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Tokens;

/// <summary>
/// Layer 1: every color GPUI Kit resolves for a theme (and every color its components
/// derive while painting, and every token background they paint) is a resource of the
/// theme with exactly the same 8-bit value, a gradient where GPUI Kit's is one, in Default
/// Light / Default Dark and in every theme GPUI Kit bundles; and the theme has no other colors.
/// </summary>
public class TokenTests
{
    public static IEnumerable<Func<string>> Themes() =>
        new[] { "Default Light", "Default Dark" }.Concat(GoldenManifest.BundledTokens().Keys).Select(name => (Func<string>)(() => name));

    [Test]
    [MethodDataSource(nameof(Themes))]
    public async Task Every_gpui_color_is_a_theme_resource_with_the_same_value(string theme)
    {
        var tokens = theme switch
        {
            "Default Light" => GoldenManifest.Tokens("light"),
            "Default Dark" => GoldenManifest.Tokens("dark"),
            _ => GoldenManifest.BundledTokens()[theme],
        };
        var variant = UIKitThemeVariants.Find(theme)!;
        var failures = new List<string>();
        foreach (var (name, value) in Colors(tokens))
        {
            Check(ResourceKey(name), value, failures, variant);
            // The color stands for the brush: itself, or a gradient's first stop.
            Check(ResourceKey(name) + ".Color", Paint.Parse(value).Stops[0].Color, failures, variant);
        }
        foreach (var (name, value) in tokens["fills"]!.AsObject())
        {
            Check(ResourceKey(name) + ".Fill", (string)value!, failures, variant);
        }
        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task Every_bundled_theme_is_a_variant_of_its_mode()
    {
        var bundled = GoldenManifest.BundledTokens();
        var failures = new List<string>();
        await Assert.That(UIKitThemeVariants.All.Select(v => (string)v.Key)).IsEquivalentTo(bundled.Keys);
        foreach (var (name, tokens) in bundled)
        {
            var dark = (string)tokens["mode"]! == "Dark";
            var variant = UIKitThemeVariants.Find(name);
            if (variant?.InheritVariant != (dark ? ThemeVariant.Dark : ThemeVariant.Light) || UIKitThemeVariants.IsDark(variant) != dark)
            {
                failures.Add($"{name}: inherits {variant?.InheritVariant}");
            }
        }
        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task The_theme_defines_no_color_gpui_does_not_have()
    {
        var tokens = GoldenManifest.Tokens("light");
        var expected = new HashSet<string>();
        foreach (var (name, _) in Colors(tokens))
        {
            expected.Add(ResourceKey(name));
        }
        var fills = tokens["fills"]!.AsObject().Select(f => ResourceKey(f.Key) + ".Fill").ToHashSet();
        await Assert.That(Palette.ColorKeys).IsEquivalentTo(expected);
        await Assert.That(Palette.FillKeys).IsEquivalentTo(fills);
        // No control theme defines a color of its own.
        var resources = Application.Current!.Styles.OfType<UIKitTheme>().Single().Resources;
        var extra = new List<string>();
        foreach (var dictionary in Dictionaries(resources))
        {
            foreach (var key in dictionary.Keys.OfType<string>())
            {
                if (dictionary.TryGetValue(key, out var value) && value is IBrush && key.StartsWith("UIKit.", StringComparison.Ordinal))
                {
                    extra.Add(key);
                }
            }
        }
        await Assert.That(extra).IsEmpty();
    }

    private static IEnumerable<(string Name, string Value)> Colors(JsonObject tokens) =>
        new[] { "colors", "derived" }.SelectMany(section => tokens[section]!.AsObject().Select(c => (c.Key, (string)c.Value!)));

    private static void Check(string key, string expected, List<string> failures, ThemeVariant variant)
    {
        if (!Application.Current!.TryGetResource(key, variant, out var resource) || resource is null)
        {
            failures.Add($"{key}: missing");
            return;
        }
        var actual = Paint.Of(resource);
        var want = Paint.Parse(expected).ToString();
        if (actual != want)
        {
            failures.Add($"{key}: {actual} != gpui {want}");
        }
    }

    private static IEnumerable<IResourceDictionary> Dictionaries(IResourceDictionary root)
    {
        yield return root;
        foreach (var themed in root.ThemeDictionaries.Values.OfType<IResourceDictionary>().SelectMany(Dictionaries))
        {
            yield return themed;
        }
        foreach (var merged in root.MergedDictionaries)
        {
            var nested = merged as IResourceDictionary ?? (merged as Avalonia.Markup.Xaml.Styling.MergeResourceInclude)?.Loaded as IResourceDictionary;
            if (nested is not null)
            {
                foreach (var d in Dictionaries(nested))
                {
                    yield return d;
                }
            }
        }
    }

    /// <summary>The key reference/src/tokens.rs gives a dumped color name.</summary>
    public static string ResourceKey(string name) =>
        "UIKit." + string.Join('.', name.Split('.').Select(segment =>
            string.Concat(segment.Split('_', '-').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..]))));

    /// <summary>
    /// A paint as the generator writes it: <c>#rrggbbaa</c>, or
    /// <c>linear-gradient(180deg, #rrggbbaa 0%, #rrggbbaa 100%)</c>.
    /// </summary>
    private sealed record Paint(double? Angle, IReadOnlyList<(string Color, double Offset)> Stops)
    {
        public static Paint Parse(string value)
        {
            if (!value.StartsWith("linear-gradient(", StringComparison.Ordinal))
            {
                return new Paint(null, [(value, 0)]);
            }
            var parts = value["linear-gradient(".Length..^1].Split(", ");
            var stops = parts[1..].Select(stop => stop.Split(' ')).Select(s => (s[0], double.Parse(s[1].TrimEnd('%'), CultureInfo.InvariantCulture) / 100)).ToList();
            return new Paint(double.Parse(parts[0].Replace("deg", "", StringComparison.Ordinal), CultureInfo.InvariantCulture), stops);
        }

        public static string Of(object resource) => resource switch
        {
            Color color => Hex(color),
            ISolidColorBrush brush => Hex(brush.Color),
            ILinearGradientBrush gradient => new Paint(
                AngleOf(gradient.StartPoint, gradient.EndPoint),
                [.. gradient.GradientStops.Select(s => (Hex(s.Color), s.Offset))]).ToString(),
            _ => $"a {resource.GetType().Name}",
        };

        // The CSS angle of the line from start to end: 180 runs down, 90 to the right.
        private static double AngleOf(RelativePoint start, RelativePoint end)
        {
            if (start.Unit != RelativeUnit.Relative || end.Unit != RelativeUnit.Relative)
            {
                return double.NaN;
            }
            var degrees = Math.Atan2(end.Point.Y - start.Point.Y, end.Point.X - start.Point.X) * 180 / Math.PI + 90;
            return (degrees % 360 + 360) % 360;
        }

        private static string Hex(Color c) => $"#{c.R:x2}{c.G:x2}{c.B:x2}{c.A:x2}";

        public override string ToString() => Angle is null
            ? Stops[0].Color
            : $"linear-gradient({Math.Round(Angle.Value, 3).ToString(CultureInfo.InvariantCulture)}deg, "
              + string.Join(", ", Stops.Select(s => $"{s.Color} {Math.Round(s.Offset * 100, 3).ToString(CultureInfo.InvariantCulture)}%")) + ")";
    }
}
