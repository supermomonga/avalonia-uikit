using Avalonia.Platform;

namespace AvaloniaUIKit.Demo.ControlCatalog;

/// <summary>The sidebar sections, as the site groups its components (docs/site.md).</summary>
public enum CatalogSection
{
    /// <summary>The theme of Avalonia's own controls.</summary>
    Avalonia,

    /// <summary>The <c>uikit:</c> controls Avalonia lacks.</summary>
    UIKit,

    /// <summary>The themes of third-party libraries' controls (ADR 28).</summary>
    ThirdParty,
}

/// <summary>A demo of a component, as its site page shows it.</summary>
/// <param name="Id">The demo id (<c>button/variants</c>), a key of <see cref="Demos.DemoRegistry.Factories"/>.</param>
/// <param name="Source">The XAML's path under the demos' directory (<c>Button/Variants</c>).</param>
/// <param name="Title">The title, or null for the page's first demo.</param>
/// <param name="Heading">The page heading the demo is under, or null.</param>
/// <param name="Description">The paragraph before the demo, with <c>`code`</c> spans, or null.</param>
public sealed record CatalogDemo(string Id, string Source, string? Title, string? Heading, string? Description);

/// <summary>A component of the catalog: an entry of the site's sidebar and its page.</summary>
/// <param name="Slug">The site's slug (<c>button-group</c>).</param>
/// <param name="Title">The page title.</param>
/// <param name="Aliases">Other names the search matches.</param>
/// <param name="Section">The sidebar section.</param>
/// <param name="Group">The kind of component (Buttons, Forms, …).</param>
/// <param name="Controls">The controls, as written in XAML (<c>Button</c>, <c>uikit:Buttons</c>, <c>StackPanel.button-group</c>).</param>
/// <param name="Package">The optional package whose theme covers it, or null.</param>
/// <param name="Library">The third-party library, or null.</param>
/// <param name="Description">The page description, with <c>`code`</c> spans.</param>
/// <param name="Demos">The demos, in the page's order.</param>
public sealed record CatalogComponent(
    string Slug,
    string Title,
    string Aliases,
    CatalogSection Section,
    string Group,
    IReadOnlyList<string> Controls,
    string? Package,
    string? Library,
    string Description,
    IReadOnlyList<CatalogDemo> Demos)
{
    /// <summary>The component's page on the documentation site.</summary>
    public Uri DocsUri => new($"https://avalonia-uikit.omofla.sh/components/{Slug}");

    /// <summary>Whether the search <paramref name="query"/> matches the title, the aliases or the controls.</summary>
    public bool Matches(string query)
    {
        query = query.Trim();
        return query.Length == 0
            || Title.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Aliases.Contains(query, StringComparison.OrdinalIgnoreCase)
            || Controls.Any(c => c.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// The type names the controls name, without prefixes, classes or notes:
    /// <c>uikit:Buttons</c> → Buttons, <c>StackPanel.button-group</c> → StackPanel.
    /// </summary>
    public IEnumerable<string> ControlTypeNames => Controls.Select(c =>
    {
        var name = c.Split(' ')[0];
        var colon = name.IndexOf(':');
        name = colon < 0 ? name : name[(colon + 1)..];
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    });
}

public static partial class Catalog
{
    /// <summary>The component with <paramref name="slug"/>, or null.</summary>
    public static CatalogComponent? Find(string slug) =>
        Components.FirstOrDefault(c => string.Equals(c.Slug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>The components of a sidebar section, by title as the site sorts them.</summary>
    public static IEnumerable<CatalogComponent> In(CatalogSection section) =>
        Components.Where(c => c.Section == section).OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Slug, StringComparer.Ordinal);

    /// <summary>The sidebar heading of a section.</summary>
    public static string Heading(CatalogSection section) => section switch
    {
        CatalogSection.Avalonia => "Avalonia Controls",
        CatalogSection.UIKit => "UIKit Controls",
        _ => "Third-party Controls",
    };

    /// <summary>The demo's XAML, as it is in samples/AvaloniaUIKit.Demos (embedded as an asset).</summary>
    public static string Xaml(CatalogDemo demo) => Read($"{demo.Source}.axaml") ?? "";

    /// <summary>The demo's code-behind, or null when it has none.</summary>
    public static string? CodeBehind(CatalogDemo demo) => Read($"{demo.Source}.axaml.cs");

    private static string? Read(string path)
    {
        // The project links the demos' files as Sources/<path>.txt (the .txt keeps the XAML compiler off them).
        var uri = new Uri($"avares://AvaloniaUIKit.Demo.ControlCatalog/Sources/{path}.txt");
        if (!AssetLoader.Exists(uri))
        {
            return null;
        }
        using var stream = AssetLoader.Open(uri);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
