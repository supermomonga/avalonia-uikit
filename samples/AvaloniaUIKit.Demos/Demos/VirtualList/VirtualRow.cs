namespace AvaloniaUIKit.Demos;

/// <summary>A row of the VirtualList demo: ten thousand of them, made on demand.</summary>
public sealed record VirtualRow(int Number, string Title)
{
    public static IReadOnlyList<VirtualRow> Samples { get; } =
        Enumerable.Range(1, 10_000).Select(i => new VirtualRow(i, $"Row {i}")).ToList();
}
