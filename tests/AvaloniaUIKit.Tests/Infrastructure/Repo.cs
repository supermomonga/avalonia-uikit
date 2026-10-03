namespace AvaloniaUIKit.Tests.Infrastructure;

/// <summary>Paths inside the repository checkout.</summary>
public static class Repo
{
    public static string Root { get; } = FindRoot();

    /// <summary>The golden data generated from the pinned GPUI Kit commit.</summary>
    public static string Goldens => Path.Combine(Root, "goldens", "gpui-2c5162f");

    /// <summary>Where failing comparisons leave their images.</summary>
    public static string Artifacts => Path.Combine(Root, "tests", "artifacts");

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AvaloniaUIKit.slnx")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? throw new InvalidOperationException("AvaloniaUIKit.slnx not found above the test binaries");
    }
}
