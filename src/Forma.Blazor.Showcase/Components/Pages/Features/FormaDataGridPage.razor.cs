namespace Forma.Blazor.Showcase.Components.Pages.Features;

using System.ComponentModel.DataAnnotations;

using Forma.Blazor.Components.DataView;
using Forma.Blazor.Components.Inputs;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web.Virtualization;

public sealed partial class FormaDataGridPage : ComponentBase
{
    private const int SelectionLimit = 3;
    private const int ManyRowCount = 47;
    private const int ManyPageSize = 10;
    private const int HugeRowCount = 100_000;
    private const int BlockRowCount = 10_000;
    private const int InitialRowIndex = 49_999;
    private const int FeedBatchSize = 5;
    private static readonly TimeSpan _slowSliceDelay = TimeSpan.FromMilliseconds(600);

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

    // The huge set in blocks of ten thousand rows, each named by the rows it holds.
    private DataViewGrouping<GridItem> _blockGrouping = default!;

    private readonly List<EditableEmployee> _editableEmployees =
    [
        new() { Id = 1, Name = "Alice Johnson", Department = "Engineering", StartDate = new DateOnly(2019, 4, 2), Salary = 92000 },
        new() { Id = 2, Name = "Bob Smith", Department = "Marketing", StartDate = new DateOnly(2021, 9, 13), Salary = 74500 },
        new() { Id = 3, Name = "Carol Davis", Department = "Sales", StartDate = new DateOnly(2020, 1, 27), Salary = 81250 },
    ];

    private FormaDataView<GridItem>? _openingDataView;

    private int _scrollTargetRow = 1;

    private readonly List<GridItem> _feedList = [.. Enumerable.Range(1, 200).Select(id => new GridItem(id, $"Feed item {id}"))];

    private IQueryable<GridItem> _feedItems = default!;

    private FormaDataView<GridItem>? _feedDataView;

    private VirtualizeAnchorMode _feedAnchorMode = VirtualizeAnchorMode.Start;

    private int _nextFeedId = 201;

    private static readonly DataViewGrouping<Employee> _departmentGrouping = DataViewGrouping<Employee>.By(employee => employee.Department);

    private static readonly IQueryable<Shipment> _shipments = new[]
    {
        new Shipment(1001, "Acme Corporation", "Anvils", "Springfield", "United States", 4200, new DateOnly(2026, 3, 2)),
        new Shipment(1002, "Globex", "Reactors", "Cypress Creek", "United States", 128000, new DateOnly(2026, 3, 9)),
        new Shipment(1003, "Initech", "Printers", "Austin", "United States", 1350, new DateOnly(2026, 3, 11)),
        new Shipment(1004, "Umbrella", "Samples", "Raccoon City", "Japan", 9800, new DateOnly(2026, 3, 15)),
        new Shipment(1005, "Wonka Industries", "Confectionery", "Nottingham", "United Kingdom", 760, new DateOnly(2026, 3, 20)),
        new Shipment(1006, "Stark Industries", "Arc reactors", "Malibu", "United States", 250000, new DateOnly(2026, 3, 22)),
        new Shipment(1007, "Tyrell", "Replicants", "Los Angeles", "United States", 51000, new DateOnly(2026, 3, 28)),
    }.AsQueryable();

    private IReadOnlyList<Employee> _pickedEmployees = [];

    private IReadOnlyList<Employee> _selectedEmployees = [];

    private IReadOnlyList<Employee> _rowSelectedEmployees = [];

    private SelectAllMode _selectAllMode = SelectAllMode.FromMenu;

    private CheckedSelectionOptions _selectAllModeOptions = new();

    private IReadOnlyList<GridItem> _modeSelectedItems = [];

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
        _feedItems = _feedList.AsQueryable();
        _blockGrouping = DataViewGrouping<GridItem>.By(item => (item.Id - 1) / BlockRowCount, block => L["Rows {First:N0} to {Last:N0}", (block * BlockRowCount) + 1, (block + 1) * BlockRowCount].Value);

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

    private void HandlePickedChanged(IReadOnlyList<Employee> picked)
        => _pickedEmployees = picked;

    private void HandleModeSelectionChanged(IReadOnlyList<GridItem> selected)
        => _modeSelectedItems = selected;

    private Task ScrollToRowAsync()
        => _openingDataView is { } dataView
            ? dataView.ScrollToItemAsync(Math.Clamp(_scrollTargetRow, 1, HugeRowCount) - 1)
            : Task.CompletedTask;

    // Answers a slice after a pause, with its placeholders shown meanwhile.
    private static async ValueTask<DataViewPage<GridItem>> ProvideSlowItemsAsync(DataViewRequest<GridItem> request)
    {
        if (request.Count is not 0)
        {
            await Task.Delay(_slowSliceDelay, request.CancellationToken);
        }

        var slice = _hugeItems.Skip(request.StartIndex).Take(request.Count ?? HugeRowCount).ToArray();

        return new DataViewPage<GridItem>(slice, HugeRowCount);
    }

    private async Task PrependFeedItemsAsync()
    {
        var arrivals = Enumerable.Range(0, FeedBatchSize)
            .Select(offset => new GridItem(_nextFeedId + FeedBatchSize - 1 - offset, $"Feed item {_nextFeedId + FeedBatchSize - 1 - offset}"))
            .ToList();
        _nextFeedId += FeedBatchSize;
        _feedList.InsertRange(0, arrivals);

        if (_feedDataView is { } dataView)
        {
            await dataView.RefreshDataAsync();
        }
    }

    private void ToggleFeedAnchor()
        => _feedAnchorMode = _feedAnchorMode == VirtualizeAnchorMode.Start ? VirtualizeAnchorMode.None : VirtualizeAnchorMode.Start;

    private void SetSelectAllMode(SelectAllMode mode)
    {
        _selectAllMode = mode;
        _selectAllModeOptions = new CheckedSelectionOptions { SelectAllMode = mode };
    }

    private string GetModeLabel(SelectAllMode mode)
        => mode switch
        {
            SelectAllMode.None => L["None"],
            SelectAllMode.Direct => L["Direct"],
            SelectAllMode.FromMenu => L["From menu"],
            SelectAllMode.FromPager => L["From pager"],
            _ => mode.ToString(),
        };

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
            Remote = employee.Remote,
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
        target.Remote = edited.Remote;
    }

    private enum RowAction
    {
        View,
        Edit,
        Delete,
    }

    private sealed record Employee(int Id, string Name, string Department, bool IsActive);

    private sealed record GridItem(int Id, string Title);

    private sealed record Shipment(int Id, string Customer, string Product, string City, string Country, decimal Amount, DateOnly Shipped);

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

        public bool Remote { get; set; }
    }
}
