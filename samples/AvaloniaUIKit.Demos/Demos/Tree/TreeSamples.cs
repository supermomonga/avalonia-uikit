namespace AvaloniaUIKit.Demos;

/// <summary>The Tree demos' data: a workspace of 50 packages with 200 files each.</summary>
public static class TreeSamples
{
    /// <summary>A new tree for each demo, so expanding in one does not expand another.</summary>
    public static IReadOnlyList<TreeItem> Workspace =>
        Enumerable.Range(1, 50).Select(p =>
        {
            var package = new TreeItem { Label = $"package-{p:00}", IsExpanded = p <= 2 };
            for (var f = 1; f <= 200; f++)
            {
                package.Children.Add(new TreeItem { Label = $"file-{f:000}.rs" });
            }
            return package;
        }).ToList();
}
