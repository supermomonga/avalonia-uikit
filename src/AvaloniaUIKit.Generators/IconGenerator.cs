using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AvaloniaUIKit.Generators;

/// <summary>
/// Adds the icons a project uses, and the theme does not carry, to the
/// project's assembly (ADR 38). An icon is used when the project names it:
/// <c>IconName.Bike</c> or <c>"UIKit.Icon.Bike"</c> in C#; <c>Kind="Bike"</c>,
/// <c>UIKit.Icon.Bike</c> or <c>IconName.Bike</c> in XAML; or a
/// <c>&lt;UIKitIcon Include="Bike" /&gt;</c> item for an icon only chosen at
/// run time. <c>&lt;UIKitIcons&gt;All&lt;/UIKitIcons&gt;</c> adds every icon.
/// The geometry is UTF-8 in the assembly, and a module initializer hands it
/// to the theme (<c>AvaloniaUIKit.IconGeometries</c>), which parses an icon
/// the first time it is looked up.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class IconGenerator : IIncrementalGenerator
{
    private const string Category = "AvaloniaUIKit.Icons";

    private static readonly DiagnosticDescriptor s_unknownIcon = new(
        "UIKIT001",
        "A UIKitIcon item names no icon",
        "UIKitIcon '{0}' is not an IconName: name the icon by its IconName (Bike), its file (bike) or its key (UIKit.Icon.Bike)",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor s_unknownMode = new(
        "UIKIT002",
        "UIKitIcons is neither All nor Used",
        "UIKitIcons is '{0}': it is All (every icon) or Used (the icons the project names, the default)",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // C#: IconName.Bike, however the type is spelled (qualified, an alias).
        var inMembers = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name }
                    && IconCatalog.Adds(name.Identifier.ValueText),
                static (syntax, token) => syntax.SemanticModel.GetSymbolInfo(syntax.Node, token).Symbol is IFieldSymbol
                {
                    ContainingType:
                    {
                        Name: "IconName",
                        ContainingNamespace: { Name: "AvaloniaUIKit", ContainingNamespace.IsGlobalNamespace: true },
                    },
                } field ? field.Name : null)
            .Collect();

        // C#: a resource key, "UIKit.Icon.Bike".
        var inKeys = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, _) => node is LiteralExpressionSyntax literal
                    && literal.IsKind(SyntaxKind.StringLiteralExpression)
                    && literal.Token.ValueText.StartsWith(IconCatalog.KeyPrefix, StringComparison.Ordinal),
                static (syntax, _) => IconCatalog.NameOfKey(((LiteralExpressionSyntax)syntax.Node).Token.ValueText))
            .Collect();

        // XAML: the AvaloniaXaml items, which Avalonia's build passes as additional files.
        var inXaml = context.AdditionalTextsProvider
            .Where(static file => file.Path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)
                || file.Path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .Select(static (file, token) => Join(XamlScanner.Names(file.GetText(token)?.ToString() ?? string.Empty)))
            .Collect();

        // MSBuild: <UIKitIcons> and the <UIKitIcon> items (buildTransitive/AvaloniaUIKit.targets).
        var options = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => (
            Mode: Property(provider, "UIKitIcons"),
            Listed: Property(provider, "_UIKitIconList")));

        // Without AvaloniaUIKit's registry there is no theme to add icons to.
        var hasRegistry = context.CompilationProvider.Select(static (compilation, _) =>
            compilation.GetTypeByMetadataName("AvaloniaUIKit.IconGeometries") is not null);

        var used = inMembers.Combine(inKeys).Combine(inXaml).Select(static (sources, _) =>
            Join(sources.Left.Left.Concat(sources.Left.Right).Concat(sources.Right.SelectMany(Split))));

        // The names to add, joined, so that an unchanged set adds nothing anew: "*" for every icon.
        var added = used.Combine(options).Combine(hasRegistry).Select(static (input, _) =>
        {
            var ((names, (mode, listed)), registry) = input;
            if (!registry)
            {
                return string.Empty;
            }
            if (IsAll(mode))
            {
                return "*";
            }
            var requested = Split(listed).Select(IconCatalog.Resolve).Where(static name => name is not null);
            return Join(Split(names).Concat(requested).Where(static name => IconCatalog.Adds(name!)));
        });

        context.RegisterSourceOutput(options, static (output, options) =>
        {
            if (!string.IsNullOrWhiteSpace(options.Mode) && !IsAll(options.Mode)
                && !string.Equals(options.Mode.Trim(), "Used", StringComparison.OrdinalIgnoreCase))
            {
                output.ReportDiagnostic(Diagnostic.Create(s_unknownMode, Location.None, options.Mode.Trim()));
            }
            foreach (var item in Split(options.Listed))
            {
                if (IconCatalog.Resolve(item) is null)
                {
                    output.ReportDiagnostic(Diagnostic.Create(s_unknownIcon, Location.None, item.Trim()));
                }
            }
        });

        context.RegisterSourceOutput(added, static (output, names) =>
        {
            if (names.Length > 0)
            {
                output.AddSource("UIKitIcons.g.cs", Source(names == "*"
                    ? IconCatalog.Icons.Where(static icon => icon.Value is not null).Select(static icon => icon.Key)
                    : Split(names)));
            }
        });
    }

    private static string Property(AnalyzerConfigOptionsProvider provider, string name) =>
        provider.GlobalOptions.TryGetValue("build_property." + name, out var value) ? value : string.Empty;

    private static bool IsAll(string mode) => string.Equals(mode.Trim(), "All", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> Split(string? names) =>
        (names ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(static name => name.Trim())
            .Where(static name => name.Length > 0);

    private static string Join(IEnumerable<string?> names) =>
        string.Join(";", names.OfType<string>().Distinct(StringComparer.Ordinal).OrderBy(static name => name, StringComparer.Ordinal));

    /// <summary>The source that hands the icons' geometry to the theme as the assembly loads.</summary>
    private static string Source(IEnumerable<string> names)
    {
        var source = new StringBuilder();
        source.Append("""
            // <auto-generated/>
            // The icons this assembly uses that the AvaloniaUIKit theme does not carry, added by
            // AvaloniaUIKit.Generators. Lucide is ISC licensed: see the AvaloniaUIKit package's
            // THIRD-PARTY-NOTICES.md.
            #nullable enable

            namespace AvaloniaUIKit.Generated
            {
                /// <summary>The icons this assembly adds to the AvaloniaUIKit theme.</summary>
                internal static class UIKitIcons
                {
                    [global::System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void Register() => global::AvaloniaUIKit.IconGeometries.Register(Geometry);

                    private static global::System.ReadOnlySpan<byte> Geometry(string key) => key switch
                    {

            """);
        foreach (var name in names.OrderBy(static name => name, StringComparer.Ordinal))
        {
            var geometry = IconCatalog.Icons[name]!;
            Case(source, IconCatalog.KeyPrefix + name, geometry.Data);
            if (geometry.Faint is { } faint)
            {
                Case(source, IconCatalog.KeyPrefix + name + IconCatalog.FaintSuffix, faint);
            }
        }
        source.Append("""
                        _ => default,
                    };
                }
            }

            """);
        return source.ToString();
    }

    // Path markup is letters, digits, spaces, commas, dots and minus signs: nothing to escape.
    private static void Case(StringBuilder source, string key, string data) =>
        source.Append("            \"").Append(key).Append("\" => \"").Append(data).Append("\"u8,\n");
}
