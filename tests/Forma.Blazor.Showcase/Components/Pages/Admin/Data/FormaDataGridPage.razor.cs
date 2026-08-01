using Forma.Blazor.Components.Admin.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Forma.Blazor.Showcase.Components.Pages.Admin.Data;

public sealed partial class FormaDataGridPage : ComponentBase
{
    [Inject] private IStringLocalizer<FormaDataGridPage> L { get; set; } = default!;

    private static readonly IQueryable<Employee> _employees = new[]
    {
        new Employee(1, "Alice Johnson", "Engineering", true),
        new Employee(2, "Bob Smith", "Marketing", true),
        new Employee(3, "Carol Davis", "Engineering", false),
        new Employee(4, "Dan Wilson", "Sales", true),
        new Employee(5, "Eve Martinez", "Marketing", false),
        new Employee(6, "Frank Lee", "Sales", true),
    }.AsQueryable();

    private static readonly IQueryable<GridItem> _manyItems = Enumerable.Range(1, 47)
        .Select(id => new GridItem(id, $"Sample item {id}"))
        .AsQueryable();

    private static readonly IQueryable<GridItem> _hugeItems = Enumerable.Range(1, 5000)
        .Select(id => new GridItem(id, $"Sample item {id}"))
        .AsQueryable();

    private IReadOnlyList<Employee> _selected = [];

    private IReadOnlyList<Employee> _rowSelected = [];

    private IReadOnlyList<Employee> _limitsSelected = [];

    private CheckedSelectionCaption _limitsCaption = default!;

    private Employee? _lastClicked;

    protected override void OnInitialized()
        => _limitsCaption = new CheckedSelectionCaption
        {
            SelectAllText = L["Limits.SelectAll"],
            DeselectAllText = L["Limits.DeselectAll"],
        };

    private void HandleSelectionChanged(IReadOnlyList<Employee> selected)
        => _selected = selected;

    private void HandleRowSelectionChanged(IReadOnlyList<Employee> selected)
        => _rowSelected = selected;

    private void HandleLimitsSelectionChanged(IReadOnlyList<Employee> selected)
        => _limitsSelected = selected;

    private void HandleRowClick(Employee employee)
        => _lastClicked = employee;

    private static string? GetRowClass(Employee employee)
        => employee.IsActive ? null : "row-inactive";

    private sealed record Employee(int Id, string Name, string Department, bool IsActive);

    private sealed record GridItem(int Id, string Title);
}
