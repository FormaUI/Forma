using System.ComponentModel.DataAnnotations;
using Forma.Blazor.Components.Admin.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
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

    private readonly List<EditableEmployee> _editable =
    [
        new() { Id = 1, Name = "Alice Johnson", Department = "Engineering" },
        new() { Id = 2, Name = "Bob Smith", Department = "Marketing" },
        new() { Id = 3, Name = "Carol Davis", Department = "Sales" },
    ];

    private IReadOnlyList<Employee> _selected = [];

    private IReadOnlyList<Employee> _rowSelected = [];

    private IReadOnlyList<Employee> _limitsSelected = [];

    private CheckedSelectionCaption _limitsCaption = default!;

    private Employee? _lastClicked;

    private string? _lastAction;

    // The row being edited and a detached copy of it: edits land on the copy, so cancelling
    // discards rather than restores.
    private int? _editingId;

    private EditableEmployee _draft = new();

    private EditContext _editContext = default!;

    protected override void OnInitialized()
    {
        _limitsCaption = new CheckedSelectionCaption
        {
            SelectAllText = L["Limits.SelectAll"],
            DeselectAllText = L["Limits.DeselectAll"],
        };

        _editContext = new EditContext(_draft);
    }

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
        => _lastAction = $"{L[$"RowActions.{action}"]} — {employee.Name}";

    private bool IsEditing(EditableEmployee employee)
        => _editingId == employee.Id;

    private void BeginEdit(EditableEmployee employee)
    {
        _draft = new EditableEmployee
        {
            Id = employee.Id,
            Name = employee.Name,
            Department = employee.Department,
        };

        _editContext = new EditContext(_draft);
        _editingId = employee.Id;
    }

    private void CancelEdit()
        => _editingId = null;

    private void SaveEdit()
    {
        var target = _editable.Find(employee => employee.Id == _editingId);

        if (target is not null)
        {
            target.Name = _draft.Name;
            target.Department = _draft.Department;
        }

        _editingId = null;
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
    }
}
