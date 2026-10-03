namespace AvaloniaUIKit.AotSmoke;

/// <summary>A row of the gallery's tables.</summary>
public sealed record Invoice(string Id, string Method, string Amount)
{
    public static IReadOnlyList<Invoice> Samples { get; } =
    [
        new("INV001", "Credit Card", "$250.00"),
        new("INV002", "PayPal", "$150.00"),
        new("INV003", "Bank Transfer", "$350.00"),
    ];
}
