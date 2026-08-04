namespace Forma.Blazor.Components.Admin.Data;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.QuickGrid;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

/// <summary>A paged, sortable table over a queryable set of items, built on QuickGrid.</summary>
public sealed partial class FormaDataGrid<TGridItem> : ComponentBase, IAsyncDisposable
    where TGridItem : notnull
{
    private const string ModulePath = "./_content/Forma.Blazor/Components/Admin/Data/FormaDataGrid.razor.js";

    private readonly PaginationState _pagination = new();

    // Row identity for the selection script: a token survives while its row stays
    // rendered, and is released once the row pages or scrolls out of view.
    private readonly RowTokenTable _rowTokens = new();

    private readonly RowEditSession<TGridItem> _editSession = new();

    private HashSet<TGridItem> _selectedSet = [];
    private QuickGrid<TGridItem>? _grid;
    private ElementReference _scrollerElement;
    private ElementReference _selectAllElement;
    private JsModule _module = default!;
    private DotNetObjectReference<FormaDataGrid<TGridItem>>? _selfReference;
    private bool? _indeterminateApplied;
    private bool _rowSelectionAttached;
    private SelectionBounds _bounds;

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

    /// <summary>Which parts of the page controls are shown.</summary>
    [Parameter] public FormaPaginationOptions PaginationOptions { get; set; } = FormaPaginationOptions.Default;

    /// <summary>Renders only the rows near the scroll viewport, for a large list with <see cref="EnablePagination"/> disabled. Requires an ancestor that scrolls.</summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>Expected height of a row in pixels, which <see cref="Virtualize"/> uses to size the scroll range and pick how many rows to fetch.</summary>
    [Parameter] public float ItemSize { get; set; } = 32;

    /// <summary>How many rows <see cref="Virtualize"/> renders beyond each edge of the viewport, trading initial load time for scroll smoothness.</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>Content shown in the grid's footer, beside the page controls.</summary>
    [Parameter] public RenderFragment? Status { get; set; }

    /// <summary>Adds a leading checkbox column for mass selection.</summary>
    [Parameter] public bool EnableCheckedSelection { get; set; }

    /// <summary>Allows a row to be selected through direct row interaction (click, arrow keys), independently of <see cref="EnableCheckedSelection"/>.</summary>
    [Parameter] public bool EnableRowSelection { get; set; }

    /// <summary>Allows one row at a time to be edited in place by the columns that offer an editor. Requires <see cref="CloneForEdit"/>.</summary>
    [Parameter] public bool EnableInlineEditing { get; set; }

    /// <summary>Produces the detached copy an edit writes to, so cancelling discards it rather than restoring the row.</summary>
    [Parameter] public Func<TGridItem, TGridItem>? CloneForEdit { get; set; }

    /// <summary>Fired with the edited copy once it passes validation. Persisting it is the host's job.</summary>
    [Parameter] public EventCallback<TGridItem> OnRowSave { get; set; }

    /// <summary>Fired with the untouched row when an edit is abandoned.</summary>
    [Parameter] public EventCallback<TGridItem> OnRowCancel { get; set; }

    /// <summary>The fewest rows that must remain selected once a selection exists. A selected row cannot be individually unchecked below this floor; clearing the whole selection is always allowed.</summary>
    [Parameter] public int? MinimumSelection { get; set; }

    /// <summary>The most rows that may be selected at once.</summary>
    [Parameter] public int? MaximumSelection { get; set; }

    /// <summary>Caption shown in place of the header checkbox. A <see langword="null"/> value keeps the plain checkbox.</summary>
    [Parameter] public CheckedSelectionCaption? CheckedSelectionCaption { get; set; }

    /// <summary>The currently selected rows.</summary>
    [Parameter] public IReadOnlyList<TGridItem> SelectedItems { get; set; } = [];

    /// <summary>Fired when the set of selected rows changes.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TGridItem>> SelectedItemsChanged { get; set; }

    /// <summary>Fired when a selection attempt is blocked by <see cref="MinimumSelection"/> or <see cref="MaximumSelection"/>.</summary>
    [Parameter] public EventCallback<SelectionRejected> OnSelectionRejected { get; set; }

    /// <summary>Fired when a row is clicked outside of a selection gesture.</summary>
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

    /// <summary>Whether <see cref="MaximumSelection"/> has been reached, so no additional row can be selected.</summary>
    public bool IsMaximumSelectionReached
        => !_bounds.CanAdd(_selectedSet.Count);

    /// <summary>Whether the current selection meets <see cref="MinimumSelection"/>.</summary>
    public bool IsMinimumSelectionSatisfied
        => _bounds.Minimum is null || _selectedSet.Count >= _bounds.Minimum.Value;

    // Selecting every item can only ever be offered when it would not itself exceed the
    // maximum — there is no partial "select as many as fit" fallback.
    private bool CanSelectAll
        => _bounds.CanSelectAll(TotalItemCount);

    // Deselecting is always available; selecting all is blocked once it would overshoot
    // the maximum, and there is nothing to clear when the selection is already empty.
    private bool MasterControlDisabled
        => _selectedSet.Count == 0 && !CanSelectAll;

    private bool HasMasterCaption
        => CheckedSelectionCaption is not null;

    private bool MasterCaptionHidden
        => CheckedSelectionCaption is { SelectAllText: null, DeselectAllText: null };

    private bool MasterCaptionInteractive
        => CheckedSelectionCaption is { SelectAllText: not null, DeselectAllText: not null };

    private string? MasterCaptionText
        => CheckedSelectionCaption switch
        {
            null => null,
            { SelectAllText: not null, DeselectAllText: not null }
                => _selectedSet.Count > 0 ? CheckedSelectionCaption.DeselectAllText : CheckedSelectionCaption.SelectAllText,
            { SelectAllText: not null } caption => caption.SelectAllText,
            { DeselectAllText: not null } caption => caption.DeselectAllText,
            _ => null,
        };

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
    {
        _pagination.TotalItemCountChanged += HandleTotalItemCountChanged;
        _module = new JsModule(JS, ModulePath);
    }

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Columns);
        ArgumentNullException.ThrowIfNull(ItemKey);
        ArgumentNullException.ThrowIfNull(QueryParameterNamePrefix);
        ArgumentNullException.ThrowIfNull(PaginationOptions);
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
        if (EnableCheckedSelection && ItemsProvider is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableCheckedSelection)} requires {nameof(Items)}; it is not supported with {nameof(ItemsProvider)}.");
        }

        // Row selection is keyed by ItemKey against the same materialized set; a provider
        // offers no random access to look a key back up outside the page it last fetched.
        if (EnableRowSelection && ItemsProvider is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableRowSelection)} requires {nameof(Items)}; it is not supported with {nameof(ItemsProvider)}.");
        }

        if (EnableInlineEditing && CloneForEdit is null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableInlineEditing)} requires {nameof(CloneForEdit)}; an edit needs a copy to write to.");
        }

        // Editors write through to the draft by reference; a value type would be copied on
        // every read, so the edits would be silently dropped rather than reported.
        if (EnableInlineEditing && typeof(TGridItem).IsValueType)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableInlineEditing)} requires a reference type, but {typeof(TGridItem)} is a value type.");
        }

        _editSession.KeyOf = ItemKey;

        _bounds = new SelectionBounds(MinimumSelection, MaximumSelection);
        _bounds.Validate();

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
            await _pagination.SetCurrentPageIndexAsync(0);
        }
    }

    /// <summary>Re-queries the data source and re-renders the current page.</summary>
    public Task RefreshDataAsync()
        => _grid?.RefreshDataAsync() ?? Task.CompletedTask;

    /// <summary>The row currently being edited, or <see langword="null"/> when no edit is in progress.</summary>
    public TGridItem? EditingItem
        => _editSession.Item;

    /// <summary>Whether <paramref name="item"/> is the row currently being edited.</summary>
    public bool IsEditing(TGridItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _editSession.IsEditing(item);
    }

    /// <summary>Starts editing <paramref name="item"/>, replacing any edit already in progress.</summary>
    public void BeginEdit(TGridItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!EnableInlineEditing)
        {
            throw new InvalidOperationException($"{nameof(BeginEdit)} requires {nameof(EnableInlineEditing)}.");
        }

        _editSession.Begin(item, CloneForEdit!(item));
        StateHasChanged();
    }

    /// <summary>Validates the edit and, if it passes, ends it and reports the edited copy. Returns whether it was accepted.</summary>
    public async Task<bool> SaveEditAsync()
    {
        if (_editSession is not { Draft: { } draft, EditContext: { } editContext })
        {
            return false;
        }

        if (!await editContext.ValidateAsync())
        {
            return false;
        }

        _editSession.End();
        StateHasChanged();
        await OnRowSave.InvokeAsync(draft);

        return true;
    }

    /// <summary>Abandons the edit, discarding the draft.</summary>
    public async Task CancelEditAsync()
    {
        if (_editSession.Item is not { } item)
        {
            return;
        }

        _editSession.End();
        StateHasChanged();
        await OnRowCancel.InvokeAsync(item);
    }

    // "Some but not all selected" is an IDL-only property, so the header checkbox
    // cannot express it from markup. Row-click/keyboard selection is likewise delegated
    // to script, since QuickGrid exposes no row-level attribute or modifier-key hook.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (EnableRowSelection && !_rowSelectionAttached)
        {
            // Claimed before the await so a render arriving mid-call cannot re-enter and
            // attach a second listener to the same scroller.
            _rowSelectionAttached = true;
            _selfReference ??= DotNetObjectReference.Create(this);
            _rowSelectionAttached = await _module.TryInvokeVoidAsync("attachRowSelection", _scrollerElement, _selfReference);
        }

        if (!EnableCheckedSelection || HasMasterCaption)
        {
            _indeterminateApplied = null;
        }
        else if (_indeterminateApplied != PartiallySelected
            && await _module.TryInvokeVoidAsync("setIndeterminate", _selectAllElement, PartiallySelected))
        {
            _indeterminateApplied = PartiallySelected;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        _pagination.TotalItemCountChanged -= HandleTotalItemCountChanged;

        if (_rowSelectionAttached)
        {
            await _module.TryInvokeVoidAsync("detachRowSelection", _scrollerElement);
        }

        await _module.DisposeAsync();
        _selfReference?.Dispose();
    }

    private bool IsSelected(TGridItem item)
        => _selectedSet.Contains(item);

    // Selection state is layered on top of whatever class the host already assigns, so a
    // custom RowClass and the built-in selected/disabled styling never conflict.
    private string? ComposedRowClass(TGridItem item)
    {
        var hostClass = RowClass?.Invoke(item);
        if (!EnableCheckedSelection && !EnableRowSelection)
        {
            return hostClass;
        }

        List<string> classes = hostClass is null ? [] : [hostClass];

        if (IsSelected(item))
        {
            classes.Add("forma-data-grid-row-selected");
        }

        if (!CanInteractWithRow(item))
        {
            classes.Add("forma-data-grid-row-disabled");
        }

        return classes.Count == 0 ? null : string.Join(' ', classes);
    }

    // A row already selected can always be cleared; an unselected row can only be added
    // while doing so would not exceed the maximum.
    private bool CanSelectRow(TGridItem item)
        => IsSelected(item) || _bounds.CanAdd(_selectedSet.Count);

    // The checkbox column enforces both bounds directly: an unselected row cannot be
    // checked once the maximum is reached, and a selected row cannot be unchecked once
    // doing so would drop below the minimum. Row-click and range selection only ever
    // replace the whole selection, so they are a reset rather than an incremental
    // removal and are exempt from the minimum.
    private bool CanToggleRow(TGridItem item)
        => IsSelected(item)
            ? _bounds.CanRemove(_selectedSet.Count)
            : CanSelectRow(item);

    // What the row's own controls can still do, which is what its muted styling and
    // aria-disabled report: the minimum only binds where there is a checkbox to uncheck.
    private bool CanInteractWithRow(TGridItem item)
        => EnableCheckedSelection ? CanToggleRow(item) : CanSelectRow(item);

    // QuickGrid renders the selection column's header once per pass, ahead of the rows,
    // which is the only point either side can see a pass begin.
    private void BeginRowTokenPass()
        => _rowTokens.BeginPass();

    // A row keeps its token for as long as it stays rendered, so the attribute the script
    // reads does not churn under it.
    private int GetRowToken(TGridItem item)
        => _rowTokens.GetToken(ItemKey(item), item);

    private Task RejectSelectionAsync(SelectionBoundary boundary)
        => OnSelectionRejected.HasDelegate
            ? OnSelectionRejected.InvokeAsync(new SelectionRejected
            {
                Boundary = boundary,
                Limit = (boundary == SelectionBoundary.Minimum ? _bounds.Minimum : _bounds.Maximum).GetValueOrDefault(),
            })
            : Task.CompletedTask;

    private async Task ToggleSelectionAsync(TGridItem item, bool isSelected)
    {
        if (isSelected == IsSelected(item))
        {
            return;
        }

        if (!CanToggleRow(item))
        {
            await RejectSelectionAsync(isSelected ? SelectionBoundary.Maximum : SelectionBoundary.Minimum);
            return;
        }

        var updated = new HashSet<TGridItem>(_selectedSet);

        if (isSelected)
        {
            updated.Add(item);
        }
        else
        {
            updated.Remove(item);
        }

        await SelectedItemsChanged.InvokeAsync([.. updated]);
    }

    private async Task HandleMasterClickAsync()
    {
        if (_selectedSet.Count > 0)
        {
            await SelectedItemsChanged.InvokeAsync([]);
        }
        else if (CanSelectAll)
        {
            await SelectedItemsChanged.InvokeAsync([.. Items!]);
        }
        else
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
        }
    }

    /// <summary>Replaces or toggles the selection with a single row, invoked from the row-selection script.</summary>
    [JSInvokable]
    public async Task HandleRowSelectAsync(int rowToken, bool additive)
    {
        if (!_rowTokens.TryGetItem(rowToken, out var item))
        {
            return;
        }

        if (additive)
        {
            await ToggleSelectionAsync(item, !IsSelected(item));
            return;
        }

        if (!CanSelectRow(item))
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        await SelectedItemsChanged.InvokeAsync([item]);
    }

    /// <summary>Replaces or extends the selection with a contiguous range of rows, invoked from the row-selection script.</summary>
    [JSInvokable]
    public async Task HandleRowRangeSelectAsync(int[] rowTokens, bool additive)
    {
        ArgumentNullException.ThrowIfNull(rowTokens);

        var range = new List<TGridItem>(rowTokens.Length);
        foreach (var token in rowTokens)
        {
            if (_rowTokens.TryGetItem(token, out var item))
            {
                range.Add(item);
            }
        }

        HashSet<TGridItem> updated = additive ? [.. _selectedSet] : [];
        foreach (var item in range)
        {
            updated.Add(item);
        }

        // Reuses the same "does this count fit under the maximum" rule that gates a
        // master select-all, since a range select is likewise a whole-set replacement.
        if (!_bounds.CanSelectAll(updated.Count))
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        await SelectedItemsChanged.InvokeAsync([.. updated]);
    }

    // FormaPagination is 1-based; QuickGrid's PaginationState is 0-based.
    private async Task HandlePageChangedAsync(int page)
        => await _pagination.SetCurrentPageIndexAsync(page - 1);

    // QuickGrid raises this from its data-loading path, off the renderer's sync context.
    private void HandleTotalItemCountChanged(object? sender, int? totalItemCount)
        => _ = InvokeAsync(StateHasChanged);

    private string? HostAttribute(string name)
        => AdditionalAttributes is not null && AdditionalAttributes.TryGetValue(name, out var value)
            ? value?.ToString()
            : null;

    /// <summary>Gives the selection script an opaque handle for a row that survives while the row stays rendered.</summary>
    private sealed class RowTokenTable
    {
        private Dictionary<object, int> _tokensByKey = [];
        private Dictionary<object, int> _retiredTokensByKey = [];
        private Dictionary<int, TGridItem> _itemsByToken = [];
        private Dictionary<int, TGridItem> _retiredItemsByToken = [];
        private int _nextToken;

        /// <summary>Retires the current generation of tokens and starts a fresh pass.</summary>
        public void BeginPass()
        {
            (_retiredTokensByKey, _tokensByKey) = (_tokensByKey, _retiredTokensByKey);
            (_retiredItemsByToken, _itemsByToken) = (_itemsByToken, _retiredItemsByToken);
            _tokensByKey.Clear();
            _itemsByToken.Clear();
        }

        /// <summary>Returns the token for <paramref name="key"/>, reusing the one from the previous pass if it had one.</summary>
        public int GetToken(object key, TGridItem item)
        {
            if (!_tokensByKey.TryGetValue(key, out var token)
                && !_retiredTokensByKey.TryGetValue(key, out token))
            {
                token = _nextToken++;
            }

            _tokensByKey[key] = token;
            _itemsByToken[token] = item;
            return token;
        }

        /// <summary>Resolves a token back to its item, checking the retired generation too.</summary>
        public bool TryGetItem(int token, [MaybeNullWhen(false)] out TGridItem item)
            => _itemsByToken.TryGetValue(token, out item)
                || _retiredItemsByToken.TryGetValue(token, out item);
    }
}

// Non-generic marker sharing FormaDataGrid's resx across every closed instantiation —
// IStringLocalizer<T> keys resources off T's FullName, which for an open generic type
// includes its type arguments and would never match the embedded resource name.
internal abstract class FormaDataGrid;
