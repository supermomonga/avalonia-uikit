using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaUIKit.Tests.Golden;

namespace AvaloniaUIKit.Tests.Tokens;

/// <summary>
/// Layer 1: every color GPUI Kit resolves for Default Light / Default Dark (and
/// every color its components derive while painting) is a resource of the
/// theme with exactly the same 8-bit value, and the theme has no other colors.
/// </summary>
public class TokenTests
{
    public static IEnumerable<Func<string>> Modes() => [() => "light", () => "dark"];

    [Test]
    [MethodDataSource(nameof(Modes))]
    public async Task Every_gpui_color_is_a_theme_resource_with_the_same_value(string mode)
    {
        var variant = mode == "dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        var tokens = GoldenManifest.Tokens(mode);
        var expected = new Dictionary<string, string>();
        foreach (var section in new[] { "colors", "derived" })
        {
            foreach (var (name, value) in tokens[section]!.AsObject())
            {
                expected[ResourceKey(name)] = (string)value!;
            }
        }
        var failures = new List<string>();
        foreach (var (key, hex) in expected)
        {
            if (!Application.Current!.TryGetResource(key, variant, out var resource) || resource is not ISolidColorBrush brush)
            {
                failures.Add($"{key}: missing");
                continue;
            }
            var actual = $"#{brush.Color.R:x2}{brush.Color.G:x2}{brush.Color.B:x2}{brush.Color.A:x2}";
            if (actual != hex)
            {
                failures.Add($"{key}: {actual} != gpui {hex}");
            }
        }
        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task The_theme_defines_no_color_gpui_does_not_have()
    {
        var expected = new HashSet<string>();
        foreach (var section in new[] { "colors", "derived" })
        {
            foreach (var (name, _) in GoldenManifest.Tokens("light")[section]!.AsObject())
            {
                expected.Add(ResourceKey(name));
            }
        }
        var colors = Application.Current!.Styles.OfType<GpuiTheme>().Single().Resources;
        var extra = new List<string>();
        foreach (var dictionary in Dictionaries(colors))
        {
            foreach (var key in dictionary.Keys.OfType<string>())
            {
                if (dictionary.TryGetValue(key, out var value) && value is ISolidColorBrush && key.StartsWith("Gpui.", StringComparison.Ordinal) && !expected.Contains(key))
                {
                    extra.Add(key);
                }
            }
        }
        await Assert.That(extra).IsEmpty();
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
        "Gpui." + string.Join('.', name.Split('.').Select(segment =>
            string.Concat(segment.Split('_', '-').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..]))));
}
