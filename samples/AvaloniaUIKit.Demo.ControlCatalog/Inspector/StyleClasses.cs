using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace AvaloniaUIKit.Demo.ControlCatalog.Inspector;

/// <summary>
/// The style classes the themes give a control: the classes of the selectors in its
/// control theme (<c>^.primary</c>) and of the application's styles for its type
/// (<c>StackPanel.button-group</c>), read from the loaded styles. Classes of one kind
/// (sizes, colors, corners) exclude each other.
/// </summary>
public static partial class StyleClasses
{
    private static readonly string[][] ExclusiveSets =
    [
        ["xsmall", "small", "medium", "large"],
        [
            "primary", "secondary", "danger", "destructive", "warning", "success", "info", "error", "ghost", "link", "text",
            "muted", "tinted", "neutral", "gray", "red", "orange", "amber", "yellow", "lime", "green", "emerald", "teal",
            "cyan", "sky", "blue", "indigo", "violet", "purple", "fuchsia", "pink", "rose", "segmented", "underline", "pill",
        ],
        ["rounded-none", "rounded-small", "rounded-large", "rounded-full", "rounded", "rounded-lg"],
        ["start", "end"],
        ["left", "right", "top", "bottom"],
    ];

    private static readonly Dictionary<Type, IReadOnlyList<string>> Cache = [];

    [GeneratedRegex(@"\[[^\]]*\]|:not\([^)]*\)|:is\([^)]*\)|:nth-[a-z-]+\([^)]*\)")]
    private static partial Regex Groups();

    [GeneratedRegex(@"\.([A-Za-z_][A-Za-z0-9_-]*)")]
    private static partial Regex ClassName();

    /// <summary>The classes the themes style <paramref name="control"/> with, sizes first, then colors, then the rest.</summary>
    public static IReadOnlyList<string> For(Control control)
    {
        var theme = control.Theme ?? (control.TryFindResource(control.StyleKey, out var resource) ? resource as ControlTheme : null);
        var key = control.Theme is null ? control.StyleKey : null;
        if (key is not null && Cache.TryGetValue(key, out var cached))
        {
            return cached;
        }
        var classes = new HashSet<string>(StringComparer.Ordinal);
        if (theme is not null)
        {
            CollectNested(theme.Children, classes);
        }
        if (Application.Current is { } app)
        {
            CollectStyles(app.Styles, control.StyleKey.Name, classes);
        }
        IReadOnlyList<string> result = [.. classes.OrderBy(Rank).ThenBy(c => c, StringComparer.Ordinal)];
        if (key is not null)
        {
            Cache[key] = result;
        }
        return result;
    }

    /// <summary>The classes that cannot be set together with <paramref name="name"/>.</summary>
    public static IEnumerable<string> ExcludedBy(string name) =>
        ExclusiveSets.FirstOrDefault(set => set.Contains(name))?.Where(c => c != name) ?? [];

    private static int Rank(string name)
    {
        for (var i = 0; i < ExclusiveSets.Length; i++)
        {
            if (ExclusiveSets[i].Contains(name))
            {
                return i == 0 ? 1 : i == 1 ? 0 : i + 1;
            }
        }
        return ExclusiveSets.Length + 1;
    }

    // Styles nested in a control theme: "^.primary", "^.outline:pointerover /template/ Border".
    private static void CollectNested(IEnumerable<IStyle> styles, HashSet<string> classes)
    {
        foreach (var style in styles)
        {
            if (style is Style { Selector: { } selector } nested)
            {
                foreach (var part in selector.ToString().Split(','))
                {
                    var first = FirstCompound(part);
                    if (first.StartsWith('^'))
                    {
                        Add(first, classes);
                    }
                }
                CollectNested(nested.Children, classes);
            }
        }
    }

    // The application's styles for the type: "StackPanel.button-group > Button" gives button-group.
    private static void CollectStyles(IEnumerable<IStyle> styles, string type, HashSet<string> classes)
    {
        foreach (var style in styles)
        {
            switch (style)
            {
                case Style { Selector: { } selector }:
                    foreach (var part in selector.ToString().Split(','))
                    {
                        var first = FirstCompound(part);
                        if (first.StartsWith(type + ".", StringComparison.Ordinal))
                        {
                            Add(first, classes);
                        }
                    }
                    break;
                case StyleInclude { Loaded: Styles loaded }:
                    CollectStyles(loaded, type, classes);
                    break;
                case CatalogStyles:
                    // The catalog's own look, not the themes'.
                    break;
                case Styles nested:
                    CollectStyles(nested, type, classes);
                    break;
            }
        }
    }

    private static string FirstCompound(string selector)
    {
        selector = selector.Trim();
        var end = selector.IndexOfAny([' ', '>']);
        return end < 0 ? selector : selector[..end];
    }

    private static void Add(string compound, HashSet<string> classes)
    {
        foreach (Match match in ClassName().Matches(Groups().Replace(compound, "")))
        {
            classes.Add(match.Groups[1].Value);
        }
    }
}
