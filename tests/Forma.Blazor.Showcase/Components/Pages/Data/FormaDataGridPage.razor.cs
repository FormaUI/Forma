namespace Forma.Blazor.Showcase.Components.Pages.Data;

using System.ComponentModel.DataAnnotations;

using Forma.Blazor.Components.Data;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Localization;

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

    private readonly List<EditableEmployee> _editable =
    [
        new() { Id = 1, Name = "Alice Johnson", Department = "Engineering", StartDate = new DateOnly(2019, 4, 2), Salary = 92000 },
        new() { Id = 2, Name = "Bob Smith", Department = "Marketing", StartDate = new DateOnly(2021, 9, 13), Salary = 74500 },
        new() { Id = 3, Name = "Carol Davis", Department = "Sales", StartDate = new DateOnly(2020, 1, 27), Salary = 81250 },
    ];

    private IReadOnlyList<Employee> _selected = [];

    private IReadOnlyList<Employee> _rowSelected = [];

    private IReadOnlyList<Employee> _limitsSelected = [];

    private CheckedSelectionCaption _limitsCaption = default!;

    private Employee? _lastClicked;

    private string? _lastAction;

    private bool _detailsOpen;

    private Employee? _detailsEmployee;

    private FormaDataGrid<EditableEmployee>? _editGrid;

    protected override void OnInitialized()
        => _limitsCaption = new CheckedSelectionCaption
        {
            SelectAllText = L["Select all"],
            DeselectAllText = L["Deselect all"],
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

    private void RecordAction(RowAction action, Employee employee)
        => _lastAction = $"{ActionLabel(action)} — {employee.Name}";

    private string ActionLabel(RowAction action)
        => action switch
        {
            RowAction.View => L["View"],
            RowAction.Edit => L["Edit"],
            RowAction.Delete => L["Delete"],
            _ => action.ToString(),
        };

    private void OpenDetails(Employee employee)
    {
        _detailsEmployee = employee;
        _detailsOpen = true;
    }

    private void CloseDetails()
    {
        _detailsOpen = false;
        _detailsEmployee = null;
    }

    private static EditableEmployee CloneEmployee(EditableEmployee employee)
        => new()
        {
            Id = employee.Id,
            Name = employee.Name,
            Department = employee.Department,
            StartDate = employee.StartDate,
            Salary = employee.Salary,
        };

    private void SaveEmployee(EditableEmployee edited)
    {
        var target = _editable.Find(employee => employee.Id == edited.Id);

        if (target is null)
        {
            return;
        }

        target.Name = edited.Name;
        target.Department = edited.Department;
        target.StartDate = edited.StartDate;
        target.Salary = edited.Salary;
    }

    private enum RowAction
    {
        View,
        Edit,
        Delete,
    }

    private sealed record Employee(int Id, string Name, string Department, bool IsActive);

    private sealed record GridItem(int Id, string Title);

    private sealed class EditableEmployee
    {
        public int Id { get; set; }

        [Required, StringLength(40)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(40)]
        public string Department { get; set; } = string.Empty;

        public DateOnly StartDate { get; set; }

        [Range(0, 1_000_000)]
        public decimal Salary { get; set; }
    }
}
