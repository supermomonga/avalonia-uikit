namespace AvaloniaUIKit.Demos;

/// <summary>
/// Every demo of the documentation site by id (<c>&lt;component&gt;/&lt;name&gt;</c>).
/// The table is generated into DemoRegistry.g.cs from Demos/**/*.axaml by
/// <c>bun sites/scripts/demo-registry.ts</c> (docs/site.md).
/// </summary>
public static partial class DemoRegistry
{
    /// <summary>The ids, in order.</summary>
    public static IEnumerable<string> Ids => Factories.Keys;
}
