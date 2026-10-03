namespace AvaloniaUIKit.Demos;

/// <summary>A row of the Table demos.</summary>
public sealed record Invoice(string Id, string Status, string Method, string Amount)
{
    public static IReadOnlyList<Invoice> Samples { get; } =
    [
        new("INV001", "Paid", "Credit Card", "$250.00"),
        new("INV002", "Pending", "PayPal", "$150.00"),
        new("INV003", "Unpaid", "Bank Transfer", "$350.00"),
        new("INV004", "Paid", "Credit Card", "$450.00"),
        new("INV005", "Paid", "PayPal", "$550.00"),
        new("INV006", "Pending", "Bank Transfer", "$200.00"),
        new("INV007", "Unpaid", "Credit Card", "$300.00"),
        new("INV008", "Paid", "PayPal", "$120.00"),
    ];
}
