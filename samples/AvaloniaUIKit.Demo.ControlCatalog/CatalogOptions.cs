namespace AvaloniaUIKit.Demo.ControlCatalog;

/// <summary>What the command line asks for: <c>--component &lt;slug&gt;</c>, <c>--theme &lt;name&gt;</c>.</summary>
public sealed record CatalogOptions(string? Component = null, string? Theme = null)
{
    public static CatalogOptions Parse(IReadOnlyList<string> args)
    {
        string? Value(string name)
        {
            for (var i = 0; i < args.Count - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
        return new CatalogOptions(Value("--component"), Value("--theme"));
    }
}
