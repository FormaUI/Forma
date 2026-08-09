namespace Forma.UI.Blazor.Tests.Support.Shared;

/// <summary>A binding target covering every value type the form inputs bind to.</summary>
public sealed class FakeModel
{
    public List<int> Categories { get; set; } = [];
    public string Color { get; set; } = "#000000";
    public DateOnly? Date { get; set; }
    public int Number { get; set; }
    public double Ratio { get; set; }
    public int? Status { get; set; }
    public string? Text { get; set; }
    public bool Toggle { get; set; }
}
