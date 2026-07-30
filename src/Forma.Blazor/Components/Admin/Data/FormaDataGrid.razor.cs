using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Forma.Blazor.Components.Admin.Data;

/// <summary>A paged, sortable table over a queryable set of items, built on QuickGrid.</summary>
public sealed partial class FormaDataGrid<TGridItem> : ComponentBase, IAsyncDisposable
    where TGridItem : notnull
{
    private readonly PaginationState _pagination = new();

    private HashSet<TGridItem> _selectedSet = [];
    private QuickGrid<TGridItem>? _grid;
    private ElementReference _selectAllElement;
    private IJSObjectReference? _module;
    private bool? _indeterminateApplied;

    /// <summary>The rows to display. Supply this or <see cref="ItemsProvider"/>, not both.</summary>
    [Parameter] public IQueryable<TGridItem>? Items { get; set; }

    /// <summary>Fetches a page of rows on demand, for data paged in its own store. Supply this or <see cref="Items"/>, not both.</summary>
    [Parameter] public GridItemsProvider<TGridItem>? ItemsProvider { get; set; }

    /// <summary>Column definitions, authored with QuickGrid's own column components.</summary>
    [Parameter, EditorRequired] public RenderFragment Columns { get; set; } = default!;

    /// <summary>Number of rows shown per page. Required unless <see cref="EnablePagination"/> is disabled.</summary>
    [Parameter] public int PageSize { get; set; }

    /// <summary>Identifies a row across reloads, so a row survives its item being replaced by an equal copy.</summary>
    [Parameter] public Func<TGridItem, object> ItemKey { get; set; } = item => item;

    /// <summary>Prefix for the page and sort URL parameters, so several grids can share one page without colliding.</summary>
    [Parameter] public string QueryParameterNamePrefix { get; set; } = string.Empty;

    /// <summary>Whether the grid pages its rows. Disable for a single scrollable list, typically paired with <see cref="Virtualize"/>.</summary>
    [Parameter] public bool EnablePagination { get; set; } = true;

    /// <summary>Renders only the rows near the scroll viewport, for a large list with <see cref="EnablePagination"/> disabled. Requires an ancestor that scrolls.</summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>Expected height of a row in pixels, which <see cref="Virtualize"/> uses to size the scroll range and pick how many rows to fetch.</summary>
    [Parameter] public float ItemSize { get; set; } = 32;

    /// <summary>How many rows <see cref="Virtualize"/> renders beyond each edge of the viewport, trading initial load time for scroll smoothness.</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>Content shown in the grid's footer, beside the page controls.</summary>
    [Parameter] public RenderFragment? Status { get; set; }

    /// <summary>Adds a leading checkbox column for mass selection.</summary>
    [Parameter] public bool EnableCheckboxes { get; set; }

    /// <summary>The currently selected rows.</summary>
    [Parameter] public IReadOnlyList<TGridItem> SelectedItems { get; set; } = [];

    /// <summary>Fired when the set of selected rows changes.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TGridItem>> SelectedItemsChanged { get; set; }

    /// <summary>Fired when a row is clicked.</summary>
    [Parameter] public EventCallback<TGridItem> OnRowClick { get; set; }

    /// <summary>Assigns a CSS class to a row based on its item.</summary>
    [Parameter] public Func<TGridItem, string?>? RowClass { get; set; }

    /// <summary>Additional attributes applied to the grid's wrapping element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocalizer<FormaDataGrid> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    // Select-all spans every item in Items, not just the visible page: QuickGrid owns the
    // sort and the page slice, so the rows on screen are not a set this component can name.
    // Only the checkbox column reads this while unpaginated, so the count query stays rare.
    private int TotalItemCount
        => EnablePagination ? _pagination.TotalItemCount ?? 0 : Items?.Count() ?? 0;

    // Hidden once pagination is off or unnecessary, even while the status text still shows.
    private bool ShowPaginator
        => EnablePagination && TotalItemCount > PageSize;

    // Skipped entirely when there is nothing to put in it, so the row's gap leaves no
    // dead space under the table.
    private bool HasFooter
        => Status is not null || ShowPaginator;

    private bool AllSelected
        => TotalItemCount > 0 && _selectedSet.Count >= TotalItemCount;

    private bool PartiallySelected
        => _selectedSet.Count > 0 && !AllSelected;

    // Host class is merged rather than splatted, so supplying one never drops
    // the component's own class.
    private string CssClass
    {
        get
        {
            var hostClass = HostAttribute("class");
            return string.IsNullOrWhiteSpace(hostClass)
                ? "forma-data-grid"
                : $"forma-data-grid {hostClass}";
        }
    }

    protected override void OnInitialized()
        => _pagination.TotalItemCountChanged += HandleTotalItemCountChanged;

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Columns);
        ArgumentNullException.ThrowIfNull(ItemKey);
        ArgumentNullException.ThrowIfNull(QueryParameterNamePrefix);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ItemSize);
        ArgumentOutOfRangeException.ThrowIfNegative(OverscanCount);

        if (EnablePagination)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PageSize, 0);
        }

        if (Items is null == ItemsProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FormaDataGrid<TGridItem>)} requires exactly one of {nameof(Items)} or {nameof(ItemsProvider)}.");
        }

        // Select-all names the whole set, which a provider can only produce by fetching
        // every page it has.
        if (EnableCheckboxes && ItemsProvider is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableCheckboxes)} requires {nameof(Items)}; it is not supported with {nameof(ItemsProvider)}.");
        }

        // An unpaginated grid leaves Pagination unbound, so virtualization sizes its scroll
        // range from the real row count rather than a page.
        if (EnablePagination)
        {
            _pagination.ItemsPerPage = PageSize;
        }

        _selectedSet = [.. SelectedItems];
    }

    protected override async Task OnParametersSetAsync()
    {
        // Showing every row makes whichever page the grid was left on meaningless, so
        // turning pagination back on starts from the first page rather than resuming.
        if (!EnablePagination && _pagination.CurrentPageIndex > 0)
        {
            await _pagination.SetCurrentPageIndexAsync(0).ConfigureAwait(false);
        }
    }

    /// <summary>Re-queries the data source and re-renders the current page.</summary>
    public Task RefreshDataAsync()
        => _grid?.RefreshDataAsync() ?? Task.CompletedTask;

    // "Some but not all selected" is an IDL-only property, so the header checkbox
    // cannot express it from markup.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!EnableCheckboxes)
        {
            _indeterminateApplied = null;
            return;
        }

        if (_indeterminateApplied == PartiallySelected)
        {
            return;
        }

        try
        {
            _module ??= await JS.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Forma.Blazor/Components/Admin/Data/FormaDataGrid.razor.js").ConfigureAwait(false);
            await _module.InvokeVoidAsync("setIndeterminate", _selectAllElement, PartiallySelected).ConfigureAwait(false);
            _indeterminateApplied = PartiallySelected;
        }
        catch (JSException)
        {
            // Circuit disconnected before the module loaded — safe to ignore.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _pagination.TotalItemCountChanged -= HandleTotalItemCountChanged;

        var module = _module;
        _module = null;

        if (module is not null)
        {
            try
            {
                await module.DisposeAsync().ConfigureAwait(false);
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected — safe to ignore.
            }
        }
    }

    private bool IsSelected(TGridItem item)
        => _selectedSet.Contains(item);

    private async Task ToggleSelectionAsync(TGridItem item, bool isSelected)
    {
        var updated = new HashSet<TGridItem>(_selectedSet);

        if (isSelected)
        {
            updated.Add(item);
        }
        else
        {
            updated.Remove(item);
        }

        await SelectedItemsChanged.InvokeAsync([.. updated]).ConfigureAwait(false);
    }

    private async Task ToggleAllAsync(bool isSelected)
    {
        IReadOnlyList<TGridItem> updated = isSelected ? [.. Items!] : [];
        await SelectedItemsChanged.InvokeAsync(updated).ConfigureAwait(false);
    }

    // FormaPagination is 1-based; QuickGrid's PaginationState is 0-based.
    private async Task HandlePageChangedAsync(int page)
        => await _pagination.SetCurrentPageIndexAsync(page - 1).ConfigureAwait(false);

    // QuickGrid raises this from its data-loading path, off the renderer's sync context.
    private void HandleTotalItemCountChanged(object? sender, int? totalItemCount)
        => _ = InvokeAsync(StateHasChanged);

    private string? HostAttribute(string name)
        => AdditionalAttributes is not null && AdditionalAttributes.TryGetValue(name, out var value)
            ? value?.ToString()
            : null;
}

// Non-generic marker sharing FormaDataGrid's resx across every closed instantiation —
// IStringLocalizer<T> keys resources off T's FullName, which for an open generic type
// includes its type arguments and would never match the embedded resource name.
internal abstract class FormaDataGrid;
