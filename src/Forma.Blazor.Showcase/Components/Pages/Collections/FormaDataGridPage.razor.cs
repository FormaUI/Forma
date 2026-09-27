namespace Forma.Blazor.Showcase.Components.Pages.Collections;

using System.ComponentModel.DataAnnotations;

using Forma.Blazor.Components.Collections;
using Forma.Blazor.Components.Inputs;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public sealed partial class FormaDataGridPage : ComponentBase
{
    private const int SelectionLimit = 3;
    private const int ManyRowCount = 47;
    private const int ManyPageSize = 10;
    private const int HugeRowCount = 5000;

    [Inject] private IStringLocale<FormaDataGridPage> L { get; set; } = default!;

    private static readonly IQueryable<Employee> _employees = new[]
    {
        new Employee(1, "Alice Johnson", "Engineering", true),
        new Employee(2, "Bob Smith", "Marketing", true),
        new Employee(3, "Carol Davis", "Engineering", false),
        new Employee(4, "Dan Wilson", "Sales", true),
        new Employee(5, "Eve Martinez", "Marketing", false),
        new Employee(6, "Frank Lee", "Sales", true),
    }.AsQueryable();

    private static readonly IQueryable<GridItem> _manyItems = Enumerable.Range(1, ManyRowCount)
        .Select(id => new GridItem(id, $"Sample item {id}"))
        .AsQueryable();

    private static readonly IQueryable<GridItem> _hugeItems = Enumerable.Range(1, HugeRowCount)
        .Select(id => new GridItem(id, $"Sample item {id}"))
        .AsQueryable();

    private readonly List<EditableEmployee> _editableEmployees =
    [
        new() { Id = 1, Name = "Alice Johnson", Department = "Engineering", StartDate = new DateOnly(2019, 4, 2), Salary = 92000 },
        new() { Id = 2, Name = "Bob Smith", Department = "Marketing", StartDate = new DateOnly(2021, 9, 13), Salary = 74500 },
        new() { Id = 3, Name = "Carol Davis", Department = "Sales", StartDate = new DateOnly(2020, 1, 27), Salary = 81250 },
    ];

    private IReadOnlyList<Employee> _selectedEmployees = [];

    private IReadOnlyList<Employee> _rowSelectedEmployees = [];

    private readonly List<Assignment> _assignments =
    [
        new() { Id = 1, Name = "Alice Johnson", Priority = 2 },
        new() { Id = 2, Name = "Bob Smith", Priority = 1 },
        new() { Id = 3, Name = "Carol Davis" },
        new() { Id = 4, Name = "Dan Wilson", Priority = 3 },
        new() { Id = 5, Name = "Eve Martinez", Priority = 2 },
    ];

    private IReadOnlyList<Assignment> _assignmentSelection = [];

    private IReadOnlyList<SelectOption<int?>> _priorityOptions = [];

    private IReadOnlyList<Employee> _limitsSelectedEmployees = [];

    private CheckedSelectionOptions _limitsOptions = default!;

    private Employee? _lastClickedEmployee;

    private string? _lastAction;

    private bool _isDetailsOpen;

    private Employee? _detailsEmployee;

    private FormaDataGrid<EditableEmployee>? _editGrid;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _limitsOptions = new CheckedSelectionOptions
        {
            Maximum = SelectionLimit,
            Caption = new CheckedSelectionCaption
            {
                SelectAllText = L["Select all"],
                DeselectAllText = L["Deselect all"],
            },
        };

        _priorityOptions =
        [
            new(1, L["Low"]),
            new(2, L["Normal"]),
            new(3, L["High"]),
        ];
    }

    private void HandleSelectionChanged(IReadOnlyList<Employee> selected)
        => _selectedEmployees = selected;

    private void HandleRowSelectionChanged(IReadOnlyList<Employee> selected)
        => _rowSelectedEmployees = selected;

    private void HandleAssignmentSelectionChanged(IReadOnlyList<Assignment> selected)
        => _assignmentSelection = selected;

    private void HandleLimitsSelectionChanged(IReadOnlyList<Employee> selected)
        => _limitsSelectedEmployees = selected;

    private void HandleRowClick(Employee employee)
        => _lastClickedEmployee = employee;

    private static string? GetRowClass(Employee employee)
        => employee.IsActive ? null : "row-inactive";

    private void RecordAction(RowAction action, Employee employee)
        => _lastAction = $"{GetActionLabel(action)} — {employee.Name}";

    private string GetActionLabel(RowAction action)
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
        _isDetailsOpen = true;
    }

    private void CloseDetails()
    {
        _isDetailsOpen = false;
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
        var target = _editableEmployees.Find(employee => employee.Id == edited.Id);

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

    private sealed class Assignment
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int? Priority { get; set; }
    }

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
