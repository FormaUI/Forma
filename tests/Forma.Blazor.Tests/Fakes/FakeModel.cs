namespace Forma.Blazor.Tests.Fakes;

public sealed class FakeModel
{
    public string? Text { get; set; }
    public bool Toggle { get; set; }
    public string Color { get; set; } = "#000000";
    public DateOnly? Date { get; set; }
    public List<int> Categories { get; set; } = [];
}
