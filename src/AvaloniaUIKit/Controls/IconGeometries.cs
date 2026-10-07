using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Styling;

namespace AvaloniaUIKit;

/// <summary>
/// The geometry an assembly has for an icon's key (<c>UIKit.Icon.Bike</c>,
/// or its <c>.Faint</c> part) as UTF-8 path markup, or empty for none.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate ReadOnlySpan<byte> IconGeometrySource(string key);

/// <summary>
/// The icons the generator that comes with the package (AvaloniaUIKit.Generators)
/// adds to the assemblies that use them: the <see cref="IconName"/>s outside
/// <see cref="IconNames.Bundled"/>. The theme finds them by key like the ones
/// it carries, and parses each the first time it is looked up (ADR 38).
/// </summary>
public static class IconGeometries
{
    private static readonly Lock s_lock = new();
    private static readonly List<IconGeometrySource> s_sources = [];
    private static readonly Dictionary<string, Geometry> s_parsed = new(StringComparer.Ordinal);
    private static readonly HashSet<string> s_reported = new(StringComparer.Ordinal);

    /// <summary>Adds an assembly's icons; its generated module initializer calls this as it loads.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Register(IconGeometrySource source)
    {
        lock (s_lock)
        {
            s_sources.Add(source);
        }
    }

    /// <summary>The geometry an assembly added for <paramref name="key"/>.</summary>
    internal static bool TryGet(string key, [NotNullWhen(true)] out Geometry? geometry)
    {
        lock (s_lock)
        {
            if (s_parsed.TryGetValue(key, out geometry))
            {
                return true;
            }
            foreach (var source in s_sources)
            {
                var data = source(key);
                if (!data.IsEmpty)
                {
                    geometry = StreamGeometry.Parse(Encoding.UTF8.GetString(data));
                    s_parsed[key] = geometry;
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>Warns, once a key, that no assembly added an icon something looked up.</summary>
    internal static void ReportMissing(string key)
    {
        lock (s_lock)
        {
            if (!s_reported.Add(key))
            {
                return;
            }
        }
        Logger.TryGet(LogEventLevel.Warning, LogArea.Control)?.Log(null,
            "The icon {Key} is not in the app. The AvaloniaUIKit generator adds the icons a project names (IconName.Bike or \"UIKit.Icon.Bike\" in C#, Kind=\"Bike\" or UIKit.Icon.Bike in XAML); for an icon chosen at run time, add <UIKitIcon Include=\"Bike\" /> to the project, or <UIKitIcons>All</UIKitIcons> for every icon.",
            key);
    }
}

/// <summary>
/// The theme's resources for the icons the generator added
/// (<see cref="IconGeometries"/>). It is the first of the theme's merged
/// dictionaries, so it is asked last, after the icons the theme carries.
/// </summary>
internal sealed class GeneratedIcons : ResourceProvider
{
    private const string Prefix = "UIKit.Icon.";

    /// <inheritdoc />
    public override bool HasResources => true;

    /// <inheritdoc />
    public override bool TryGetResource(object key, ThemeVariant? theme, out object? value)
    {
        if (key is string name && name.StartsWith(Prefix, StringComparison.Ordinal))
        {
            if (IconGeometries.TryGet(name, out var geometry))
            {
                value = geometry;
                return true;
            }
            IconGeometries.ReportMissing(name);
        }
        value = null;
        return false;
    }
}
