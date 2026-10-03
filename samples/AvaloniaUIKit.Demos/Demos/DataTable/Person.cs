namespace AvaloniaUIKit.Demos;

/// <summary>A row of the DataTable demos (the TableView and the DataGrid).</summary>
public sealed record Person(string Name, string Email, string Amount)
{
    public static IReadOnlyList<Person> Samples { get; } =
    [
        new("Ada", "ada@example.com", "$250.00"),
        new("Grace", "grace@example.com", "$150.00"),
        new("Linus", "linus@example.com", "$350.00"),
        new("Ken", "ken@example.com", "$450.00"),
        new("Barbara", "barbara@example.com", "$550.00"),
        new("Dennis", "dennis@example.com", "$200.00"),
        new("Margaret", "margaret@example.com", "$300.00"),
        new("Alan", "alan@example.com", "$120.00"),
    ];
}
