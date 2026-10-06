using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvaloniaUIKit.Demo.ControlCatalog.Xaml;

/// <summary>
/// The Lucide icons the theme bundles (<c>UIKit.Icon.*</c> geometries), found in the
/// application's resources, for the property grid's icon choice.
/// </summary>
public static class Icons
{
    /// <summary>The prefix of the icons' resource keys.</summary>
    public const string Prefix = "UIKit.Icon.";

    private static IReadOnlyList<string>? s_keys;
    private static Dictionary<Geometry, string>? s_byGeometry;

    /// <summary>Every icon's resource key, by name.</summary>
    public static IReadOnlyList<string> Keys
    {
        get
        {
            Load();
            return s_keys!;
        }
    }

    /// <summary>An icon's name: its key without <see cref="Prefix"/>.</summary>
    public static string NameOf(string key) => key.StartsWith(Prefix, StringComparison.Ordinal) ? key[Prefix.Length..] : key;

    /// <summary>The key of the bundled icon <paramref name="geometry"/> is, or null.</summary>
    public static string? KeyOf(Geometry geometry)
    {
        Load();
        return s_byGeometry!.GetValueOrDefault(geometry);
    }

    /// <summary>The icon with a name (<c>Bell</c>) or key (<c>UIKit.Icon.Bell</c>), or null.</summary>
    public static Geometry? Find(string nameOrKey)
    {
        var key = nameOrKey.StartsWith(Prefix, StringComparison.Ordinal) ? nameOrKey : Prefix + nameOrKey;
        return Application.Current?.TryGetResource(key, null, out var value) == true ? value as Geometry : null;
    }

    private static void Load()
    {
        if (s_keys is not null)
        {
            return;
        }
        var icons = new Dictionary<string, Geometry>(StringComparer.Ordinal);
        if (Application.Current is { } app)
        {
            foreach (var style in app.Styles)
            {
                if (style is Styles { } styles)
                {
                    Collect(styles.Resources, icons);
                }
            }
        }
        s_keys = [.. icons.Keys.OrderBy(k => k, StringComparer.Ordinal)];
        s_byGeometry = [];
        foreach (var (key, geometry) in icons)
        {
            s_byGeometry.TryAdd(geometry, key);
        }
    }

    private static void Collect(IResourceProvider? provider, Dictionary<string, Geometry> icons)
    {
        switch (provider)
        {
            case IResourceDictionary dictionary:
                foreach (var key in dictionary.Keys)
                {
                    // TryGetResource builds a deferred (XAML) resource; enumerating the values would not.
                    if (key is string name && name.StartsWith(Prefix, StringComparison.Ordinal) && !name.EndsWith(".Faint", StringComparison.Ordinal)
                        && dictionary.TryGetResource(name, null, out var value) && value is Geometry geometry)
                    {
                        icons[name] = geometry;
                    }
                }
                foreach (var merged in dictionary.MergedDictionaries)
                {
                    Collect(merged, icons);
                }
                break;
            case ResourceInclude include:
                Collect(include.Loaded, icons);
                break;
        }
    }
}
