using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AvaloniaUIKit.Generators;

/// <summary>
/// The icons of <c>IconName</c> (Icons.g.tsv, written by <c>uikit-icons</c>):
/// every name, and the geometry of the ones the theme does not carry.
/// </summary>
internal static class IconCatalog
{
    /// <summary>The prefix of an icon's resource key: <c>UIKit.Icon.Bike</c>.</summary>
    public const string KeyPrefix = "UIKit.Icon.";

    /// <summary>The suffix of the key of a two-tone icon's faint part.</summary>
    public const string FaintSuffix = ".Faint";

    private static readonly Lazy<Dictionary<string, IconGeometry?>> s_icons = new(Load);

    /// <summary>Every IconName, with the geometry the generator adds, or null for an icon the theme carries.</summary>
    public static IReadOnlyDictionary<string, IconGeometry?> Icons => s_icons.Value;

    /// <summary>Whether the generator adds the icon: an IconName the theme does not carry.</summary>
    public static bool Adds(string name) => s_icons.Value.TryGetValue(name, out var geometry) && geometry is not null;

    /// <summary>
    /// The IconName a <c>UIKitIcon</c> item names: by IconName (<c>Bike</c>),
    /// file (<c>bike</c>, <c>arrow-down-0-1</c>) or key (<c>UIKit.Icon.Bike</c>);
    /// null for none.
    /// </summary>
    public static string? Resolve(string text)
    {
        var name = text.Trim();
        if (name.StartsWith(KeyPrefix, StringComparison.Ordinal))
        {
            name = name.Substring(KeyPrefix.Length);
        }
        if (s_icons.Value.ContainsKey(name))
        {
            return name;
        }
        var variant = Variant(name);
        return s_icons.Value.ContainsKey(variant) ? variant : null;
    }

    /// <summary>The IconName of a resource key (<c>UIKit.Icon.Bike</c>, or its <c>.Faint</c> part), or null.</summary>
    public static string? NameOfKey(string key)
    {
        if (!key.StartsWith(KeyPrefix, StringComparison.Ordinal))
        {
            return null;
        }
        var name = key.Substring(KeyPrefix.Length);
        if (name.EndsWith(FaintSuffix, StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - FaintSuffix.Length);
        }
        return s_icons.Value.ContainsKey(name) ? name : null;
    }

    /// <summary>
    /// GPUI's IconName variant for an icon file (crates/assets/build.rs): the
    /// parts between '-', '_' and '.', each capitalized.
    /// </summary>
    private static string Variant(string stem)
    {
        var variant = new StringBuilder(stem.Length);
        foreach (var part in stem.Split('-', '_', '.'))
        {
            if (part.Length > 0)
            {
                variant.Append(char.ToUpperInvariant(part[0])).Append(part.Substring(1).ToLowerInvariant());
            }
        }
        return variant.ToString();
    }

    private static Dictionary<string, IconGeometry?> Load()
    {
        var icons = new Dictionary<string, IconGeometry?>(StringComparer.Ordinal);
        using var stream = typeof(IconCatalog).Assembly.GetManifestResourceStream("AvaloniaUIKit.Generators.Icons.tsv")
            ?? throw new InvalidOperationException("The generator's icons (Icons.g.tsv) are missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }
            // <IconName> [TAB <geometry> [TAB <faint opacity> TAB <faint geometry>]]
            var fields = line.Split('\t');
            icons[fields[0]] = fields.Length switch
            {
                1 => null,
                2 => new IconGeometry(fields[1], null),
                _ => new IconGeometry(fields[1], fields[3]),
            };
        }
        return icons;
    }
}

/// <summary>An icon's geometry as path markup, and its faint part's for a two-tone icon.</summary>
internal sealed class IconGeometry(string data, string? faint)
{
    public string Data { get; } = data;

    public string? Faint { get; } = faint;
}
