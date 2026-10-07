using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging;
using Avalonia.Media;
using AvaloniaUIKit.Generators;
using AvaloniaUIKit.Tests.Golden;
using AvaloniaUIKit.Tests.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace AvaloniaUIKit.Tests.Behavior;

/// <summary>
/// The icon generator (ADR 38): which icons it adds to a project, run on
/// in-memory projects, and what an app gets from it, in this test assembly,
/// which it runs on as on an app (AvaloniaUIKit.Tests.csproj).
/// </summary>
public partial class IconGeneratorTests
{
    private static readonly Lazy<ImmutableArray<MetadataReference>> s_runtime = new(() =>
    [
        .. ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
            .Where(path => !Path.GetFileName(path).StartsWith("AvaloniaUIKit", StringComparison.Ordinal))
            .Select(path => MetadataReference.CreateFromFile(path)),
    ]);

    [GeneratedRegex("\"UIKit\\.Icon\\.(\\w+)\" =>")]
    private static partial Regex AddedKey();

    /// <summary>Runs the generator on a project with <paramref name="code"/>, its XAML and its MSBuild properties.</summary>
    private static (GeneratorDriverRunResult Result, Compilation Output) Run(
        string code,
        (string Path, string Text)[]? files = null,
        Dictionary<string, string>? properties = null,
        bool withTheme = true)
    {
        var references = withTheme
            ? s_runtime.Value.Add(MetadataReference.CreateFromFile(typeof(IconName).Assembly.Location))
            : s_runtime.Value;
        var parse = new CSharpParseOptions(LanguageVersion.Latest);
        var compilation = CSharpCompilation.Create("App", [CSharpSyntaxTree.ParseText(code, parse)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new IconGenerator().AsSourceGenerator()],
            [.. (files ?? []).Select(file => (AdditionalText)new InMemoryText(file.Path, file.Text))],
            parse,
            new Options(properties ?? []));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }

    /// <summary>The icons the generator added: the keys of the generated switch, without the faint parts.</summary>
    private static string[] Added(GeneratorDriverRunResult result) =>
    [
        .. result.GeneratedTrees.SelectMany(tree => AddedKey().Matches(tree.ToString()))
            .Select(match => match.Groups[1].Value)
            .Order(StringComparer.Ordinal),
    ];

    private static Diagnostic[] Errors(Compilation output) =>
        [.. output.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)];

    // C#: IconName.<icon> however the type is spelled, and a resource key; not
    // an icon the theme carries, nor another type's member of an icon's name.
    [Test]
    public async Task It_adds_the_icons_the_code_names_that_the_theme_does_not_carry()
    {
        var (result, output) = Run("""
            using Avalonia.Controls;
            using AvaloniaUIKit;
            using Kinds = AvaloniaUIKit.IconName;

            class Page
            {
                IconName a = IconName.Bike;
                IconName b = AvaloniaUIKit.IconName.Anchor;
                IconName c = Kinds.Award;
                IconName d = IconName.Check;
                string e = "UIKit" + ".Icon.Bird";
                object? f = Avalonia.Application.Current?.FindResource("UIKit.Icon.Apple");
                Other g = Other.Rocket;
            }

            enum Other { Rocket }
            """);
        await Assert.That(Added(result)).IsEquivalentTo(["Anchor", "Apple", "Award", "Bike"]);
        await Assert.That(Errors(output)).IsEmpty();
    }

    // XAML: Kind, a Setter of Kind, a resource key, an x:Static and the element
    // forms; not other attributes, nor files that are not XAML.
    [Test]
    public async Task It_adds_the_icons_the_xaml_names()
    {
        var (result, _) = Run("", files:
        [
            ("Views/Main.axaml", """
                <UserControl xmlns="https://github.com/avaloniaui"
                             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                             xmlns:uikit="using:AvaloniaUIKit">
                  <UserControl.Styles>
                    <Style Selector="uikit|Icon.go">
                      <Setter Property="Kind" Value="Rocket" />
                    </Style>
                  </UserControl.Styles>
                  <StackPanel>
                    <uikit:Icon Kind="Bike" />
                    <uikit:Icon Kind="Check" />
                    <PathIcon Data="{StaticResource UIKit.Icon.Camera}" />
                    <uikit:Icon Kind="{x:Static uikit:IconName.Anchor}" />
                    <uikit:Icon>
                      <uikit:Icon.Kind>Award</uikit:Icon.Kind>
                    </uikit:Icon>
                    <ItemsControl>
                      <x:Array Type="uikit:IconName">
                        <uikit:IconName>Bird</uikit:IconName>
                      </x:Array>
                    </ItemsControl>
                    <TextBlock Text="Apple" />
                  </StackPanel>
                </UserControl>
                """),
            ("Notes.txt", "Kind=\"Zap\""),
        ]);
        await Assert.That(Added(result)).IsEquivalentTo(["Anchor", "Award", "Bike", "Bird", "Camera", "Rocket"]);
    }

    // <UIKitIcon Include="..." />: by IconName, file or key, warning of a name
    // that is no icon; an icon the theme carries needs nothing.
    [Test]
    public async Task It_adds_the_icons_the_project_lists_and_warns_of_unknown_names()
    {
        var (result, _) = Run("", properties: new() { ["_UIKitIconList"] = "bike;UIKit.Icon.Anchor;ArrowDown01;arrow-up-1-0;check;Bicycle" });
        await Assert.That(Added(result)).IsEquivalentTo(["Anchor", "ArrowDown01", "ArrowUp10", "Bike"]);
        var diagnostics = result.Diagnostics.Select(diagnostic => (diagnostic.Id, diagnostic.GetMessage())).ToArray();
        await Assert.That(diagnostics.Length).IsEqualTo(1);
        await Assert.That(diagnostics[0].Id).IsEqualTo("UIKIT001");
        await Assert.That(diagnostics[0].Item2).Contains("'Bicycle'");
    }

    // <UIKitIcons>All</UIKitIcons>: every icon the theme does not carry, and
    // the result compiles; an unknown mode warns and adds the used icons.
    [Test]
    public async Task It_adds_every_icon_for_all_and_warns_of_an_unknown_mode()
    {
        var (all, output) = Run("", properties: new() { ["UIKitIcons"] = "all" });
        await Assert.That(Added(all).Length).IsEqualTo(Enum.GetValues<IconName>().Length - 1 - IconNames.Bundled.Count);
        await Assert.That(Errors(output)).IsEmpty();
        await Assert.That(all.Diagnostics).IsEmpty();

        var (some, _) = Run("class Page { AvaloniaUIKit.IconName a = AvaloniaUIKit.IconName.Bike; }",
            properties: new() { ["UIKitIcons"] = "Some" });
        await Assert.That(Added(some)).IsEquivalentTo(["Bike"]);
        await Assert.That(some.Diagnostics.Single().Id).IsEqualTo("UIKIT002");
    }

    // A project without AvaloniaUIKit has no theme to add icons to; one that
    // names none gets no source.
    [Test]
    public async Task It_adds_nothing_without_the_theme_or_icons()
    {
        var (without, _) = Run("", properties: new() { ["UIKitIcons"] = "All" }, withTheme: false);
        await Assert.That(without.GeneratedTrees).IsEmpty();
        var (unused, _) = Run("class Page { AvaloniaUIKit.IconName a = AvaloniaUIKit.IconName.Check; }");
        await Assert.That(unused.GeneratedTrees).IsEmpty();
    }

    // The generator's icons (Icons.g.tsv) and IconName come from the same
    // GPUI Kit SVGs: the same names, with geometry for the ones the theme
    // does not carry.
    [Test]
    public async Task The_generators_icons_are_the_icon_names()
    {
        var names = Enum.GetValues<IconName>().Where(name => name != IconName.None).ToArray();
        await Assert.That(IconCatalog.Icons.Keys).IsEquivalentTo(names.Select(name => name.ToString()));
        foreach (var name in names)
        {
            await Assert.That(IconCatalog.Adds(name.ToString())).IsEqualTo(!IconNames.Bundled.Contains(name));
        }
    }

    // This assembly names IconName.Bike: uikit:Icon draws the geometry the
    // generator added, in the icon's 24x24 box, and the theme hands it out by key.
    [Test]
    public async Task An_icon_the_app_names_draws_with_the_generated_geometry()
    {
        var golden = GoldenManifest.Get("uikit-icon/inherit.base/normal/light");
        var icon = new AvaloniaUIKit.Icon { Kind = IconName.Bike };
        using var host = CaseHost.Open(golden, icon);
        await Assert.That(icon.Data).IsTypeOf<StreamGeometry>();
        await Assert.That(icon.Data!.Bounds).IsEqualTo(new Rect(0, 0, 24, 24));
        await Assert.That(icon.Data).IsSameReferenceAs(Application.Current!.FindResource(IconName.Bike.ResourceKey()));
        await Assert.That(icon.Bounds.Size).IsEqualTo(new Size(16, 16));
    }

    // AvaloniaUIKit.Tests.csproj lists tent (<UIKitIcon Include="tent" />),
    // which nothing here names: the app has it all the same.
    [Test]
    public async Task An_icon_the_project_lists_reaches_the_app()
    {
        var listed = Enum.Parse<IconName>("Tent");
        await Assert.That(Application.Current!.FindResource(listed.ResourceKey())).IsTypeOf<StreamGeometry>();
    }

    // An icon nothing named is not in the app: the lookup fails, and the theme
    // warns once that the generator did not add it.
    [Test]
    public async Task An_icon_no_one_named_is_missing_with_a_warning()
    {
        var missing = Enum.Parse<IconName>("Volleyball");
        var sink = new WarningSink();
        var previous = Logger.Sink;
        Logger.Sink = sink;
        try
        {
            await Assert.That(Application.Current!.TryFindResource(missing.ResourceKey(), out _)).IsFalse();
            await Assert.That(Application.Current!.TryFindResource(missing.ResourceKey(), out _)).IsFalse();
        }
        finally
        {
            Logger.Sink = previous;
        }
        // The key is built, not written: a "UIKit.Icon.<name>" literal here would make the generator add the icon.
        var warnings = sink.Messages.Where(message => message.Contains(missing.ResourceKey(), StringComparison.Ordinal)).ToArray();
        await Assert.That(warnings.Length).IsEqualTo(1);
        await Assert.That(warnings[0]).Contains("<UIKitIcon Include=");
    }

    private sealed class InMemoryText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }

    /// <summary>MSBuild's properties as the generator sees them (build_property.*).</summary>
    private sealed class Options(Dictionary<string, string> properties) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Values(properties);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Values.Empty;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Values.Empty;

        private sealed class Values(Dictionary<string, string> properties) : AnalyzerConfigOptions
        {
            public static readonly Values Empty = new([]);

            public override bool TryGetValue(string key, out string value)
            {
                const string prefix = "build_property.";
                value = string.Empty;
                return key.StartsWith(prefix, StringComparison.Ordinal)
                    && properties.TryGetValue(key[prefix.Length..], out value!);
            }
        }
    }

    /// <summary>The warnings logged while it is <see cref="Logger.Sink"/>, with their values filled in.</summary>
    private sealed class WarningSink : ILogSink
    {
        public List<string> Messages { get; } = [];

        public bool IsEnabled(LogEventLevel level, string area) => level >= LogEventLevel.Warning;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
            Messages.Add(messageTemplate);

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
        {
            var values = new Queue<object?>(propertyValues);
            Messages.Add(Regex.Replace(messageTemplate, @"\{\w+\}", _ => values.Count > 0 ? values.Dequeue()?.ToString() ?? "" : ""));
        }
    }
}
