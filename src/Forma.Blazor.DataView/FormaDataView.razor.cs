namespace Forma.Blazor.Components.DataView;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Navigation;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Events;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

/// <summary>The engine that browses a set of items, sorted, paged, selected and kept in the address, handing what is on screen to the view that presents it.</summary>
/// <typeparam name="TItem">The item the data view browses.</typeparam>
public sealed partial class FormaDataView<TItem> : FormaElementBase, IDataView<TItem>, IDataViewInteraction<TItem>
    where TItem : notnull
{
    private const string DescendingOrder = "desc";

    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private readonly StateNotifier _stateNotifier = new();
    private readonly ItemTokenTable<TItem> _itemTokens = new();
    private readonly HashSet<string> _views = [with(StringComparer.Ordinal)];
    private readonly ParameterState<IReadOnlyList<TItem>?> _selectedItemsParameter = new();
    private readonly ParameterState<int> _pageParameter = new();

    private HashSet<TItem> _selectedSet = [];
    private IReadOnlyList<TItem> _items = [];
    private int? _totalItemCount;
    private bool _hasLoaded;
    private bool _isBusy;
    private int _page = 1;
    private object? _loadedSource;
    private int _loadedPage;
    private int _loadedPageSize;
    private bool _loadedPagination;
    private CancellationTokenSource? _loadCancellation;
    private string? _sortName;
    private DataViewSort<TItem>? _sort;
    private DataViewSortDirection _sortDirection;
    private string? _requestedSortName;
    private DataViewSortDirection _requestedSortDirection;
    private bool _isAddressRead;
    private ElementReference _regionElement;
    private SelectionInteraction _selectionInteraction = default!;
    private HeightReservation _heightReservation = default!;
    private DotNetObjectReference<FormaDataView<TItem>>? _selfReference;
    private bool _isItemSelectionAttached;
    private SelectionBounds _selectionBounds;

    /// <summary>The items to browse, sorted, counted and paged by the data view itself. Supply this or <see cref="ItemsProvider"/>, not both.</summary>
    [Parameter] public IQueryable<TItem>? Items { get; set; }

    /// <summary>Answers one window of items at a time, for a store the data view does not hold. Supply this or <see cref="Items"/>, not both.</summary>
    [Parameter] public DataViewItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>Identifies an item across reloads, so a selection or an edit survives its item being replaced by an equal copy.</summary>
    [Parameter] public Func<TItem, object> ItemKey { get; set; } = item => item;

    /// <summary>The views that present the items; one of them shows.</summary>
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;

    /// <summary>The class an item's row, card or node carries, from its item.</summary>
    [Parameter] public Func<TItem, string?>? ItemClass { get; set; }

    /// <summary>What shows in place of the items when the source answers with none; absent, the library's own sentence.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>What shows in place of the items while the source has not answered yet; absent, the library's busy indicator.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>Content shown in the footer, beside the page navigation.</summary>
    [Parameter] public RenderFragment? Status { get; set; }

    /// <summary>Whether the items are shown a page at a time. Off, every item shows in one scroll region.</summary>
    [Parameter] public bool EnablePagination { get; set; } = true;

    /// <summary>How many items a page holds. Required while <see cref="EnablePagination"/> is on.</summary>
    [Parameter] public int PageSize { get; set; }

    /// <summary>The page showing, counted from one.</summary>
    [Parameter] public int Page { get; set; } = 1;

    /// <summary>Raised with the page the reader moved to.</summary>
    [Parameter] public EventCallback<int> PageChanged { get; set; }

    /// <summary>Which parts of the page navigation are shown.</summary>
    [Parameter] public PaginationOptions PaginationOptions { get; set; } = PaginationOptions.Default;

    /// <summary>Where the sort and the page are written in the address; absent, they stay in memory.</summary>
    [Parameter] public DataViewAddressOptions? AddressOptions { get; set; }

    /// <summary>Raised with the size of the whole set whenever the source reports a different one.</summary>
    [Parameter] public EventCallback<int?> TotalItemCountChanged { get; set; }

    /// <summary>Renders only the items near the scroll viewport, for a large set with <see cref="EnablePagination"/> off. Requires an ancestor that scrolls.</summary>
    [Parameter] public bool Virtualize { get; set; }

    /// <summary>The height of the unit a view virtualizes by, in pixels, which sizes the scroll range and how many items a window fetches.</summary>
    [Parameter] public float ItemHeight { get; set; } = 32;

    /// <summary>How many units a view renders beyond each edge of the viewport while virtualizing.</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>Gives every item a box to check, with a master control over them.</summary>
    [Parameter] public bool EnableCheckedSelection { get; set; }

    /// <summary>Lets items be selected through pointer and keyboard gestures on themselves, with or without <see cref="EnableCheckedSelection"/>.</summary>
    [Parameter] public bool EnableItemSelection { get; set; }

    /// <summary>The items selected.</summary>
    [Parameter] public IReadOnlyList<TItem> SelectedItems { get; set; } = [];

    /// <summary>Raised with the items selected whenever the selection changes.</summary>
    [Parameter] public EventCallback<IReadOnlyList<TItem>> SelectedItemsChanged { get; set; }

    /// <summary>The checked selection's bounds and the caption of its master control.</summary>
    [Parameter] public CheckedSelectionOptions CheckedSelectionOptions { get; set; } = CheckedSelectionOptions.Default;

    /// <summary>Raised when a selection attempt is blocked by <see cref="CheckedSelectionOptions.Minimum"/> or <see cref="CheckedSelectionOptions.Maximum"/>.</summary>
    [Parameter] public EventCallback<SelectionRejected> OnSelectionRejected { get; set; }

    /// <summary>Raised when an item is activated outside any selection gesture.</summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <inheritdoc />
    IReadOnlyList<TItem> IDataView<TItem>.Items
        => _items;

    /// <inheritdoc />
    public bool HasLoaded
        => _hasLoaded;

    /// <inheritdoc />
    public bool IsBusy
        => _isBusy;

    /// <summary>How many items the whole set holds, or null while the source has not said.</summary>
    public int? TotalItemCount
        => _totalItemCount;

    /// <inheritdoc />
    public string? SortName
        => _sortName;

    /// <inheritdoc />
    public DataViewSortDirection SortDirection
        => _sortDirection;

    /// <inheritdoc />
    public string? RequestedSortName
        => _requestedSortName;

    /// <inheritdoc />
    public DataViewSortDirection RequestedSortDirection
        => _requestedSortDirection;

    /// <inheritdoc />
    public bool AllSelected
        => _totalItemCount is > 0 && _selectedSet.Count >= _totalItemCount;

    /// <inheritdoc />
    public bool PartiallySelected
        => _selectedSet.Count > 0 && !AllSelected;

    /// <inheritdoc />
    public bool IsMasterControlDisabled
        => _selectedSet.Count == 0 && !CanSelectAll;

    /// <inheritdoc />
    RenderFragment IDataView<TItem>.LoadingContent
        => LoadingContent ?? RenderDefaultLoading;

    /// <inheritdoc />
    RenderFragment IDataView<TItem>.EmptyContent
        => EmptyContent ?? RenderDefaultEmpty;

    /// <summary>Whether <see cref="CheckedSelectionOptions.Maximum"/> has been reached, so no further item can be selected.</summary>
    public bool IsMaximumSelectionReached
        => !_selectionBounds.CanAdd(_selectedSet.Count);

    /// <summary>Whether the selection meets <see cref="CheckedSelectionOptions.Minimum"/>.</summary>
    public bool IsMinimumSelectionSatisfied
        => _selectionBounds.Minimum is null || _selectedSet.Count >= _selectionBounds.Minimum.Value;

    // Selecting every item is offered only where it would not overshoot the maximum; there is no partial select-as-many-as-fit.
    private bool CanSelectAll
        => _selectionBounds.CanSelectAll(_totalItemCount ?? 0);

    private bool ShowsPaginator
        => EnablePagination && _totalItemCount is { } total && total > PageSize;

    // Skipped entirely when there is nothing to put in it, so the region's gap leaves no dead space beneath it.
    private bool HasFooter
        => Status is not null || ShowsPaginator;

    private int StartIndex
        => EnablePagination ? (_page - 1) * PageSize : 0;

    private int? WindowCount
        => EnablePagination ? PageSize : null;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _selectionInteraction = new SelectionInteraction(Context.JS);
        _heightReservation = new HeightReservation(Context.JS);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(ChildContent);
        ArgumentNullException.ThrowIfNull(ItemKey);
        ArgumentNullException.ThrowIfNull(PaginationOptions);
        ArgumentNullException.ThrowIfNull(CheckedSelectionOptions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ItemHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(OverscanCount);

        if (EnablePagination)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PageSize, 0);
        }

        if (Items is null == ItemsProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FormaDataView<TItem>)} requires exactly one of {nameof(Items)} or {nameof(ItemsProvider)}.");
        }

        // Select-all names the whole set, which a provider can only produce by fetching every window it has.
        if (EnableCheckedSelection && ItemsProvider is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableCheckedSelection)} requires {nameof(Items)}; it is not supported with {nameof(ItemsProvider)}.");
        }

        // Item selection is keyed against the same materialized set; a provider offers no random access to look a key back up outside the window it last fetched.
        if (EnableItemSelection && ItemsProvider is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(EnableItemSelection)} requires {nameof(Items)}; it is not supported with {nameof(ItemsProvider)}.");
        }

        _selectionBounds = new SelectionBounds(CheckedSelectionOptions.Minimum, CheckedSelectionOptions.Maximum);
        _selectionBounds.Validate();

        // The set is rebuilt only when the host hands over a different list, since every change this data view reports arrives as a new one.
        if (_selectedItemsParameter.Update(SelectedItems))
        {
            _selectedSet = [.. SelectedItems];
        }

        // The address speaks first, once; after that a page the host sets is the page shown.
        if (_pageParameter.Update(Page))
        {
            _page = Math.Max(1, Page);
        }

        if (!_isAddressRead)
        {
            _isAddressRead = true;
            ReadAddress();
        }
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // Showing every item makes whichever page the data view was left on meaningless, so turning paging back on starts from the first page rather than resuming.
        if (!EnablePagination && _page > 1)
        {
            await MoveToPageAsync(1);
        }

        var source = (object?)Items ?? ItemsProvider;

        if (!ReferenceEquals(source, _loadedSource)
            || _loadedPage != _page
            || _loadedPageSize != PageSize
            || _loadedPagination != EnablePagination)
        {
            await LoadAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (EnableItemSelection && !_isItemSelectionAttached)
        {
            // Claimed before the await so a render arriving mid-call cannot re-enter and attach a second listener to the same region.
            _isItemSelectionAttached = true;
            _selfReference ??= DotNetObjectReference.Create(this);

            // The keys are routes the script declares itself, so the host's policy reaches the router before they do.
            await InputRouter.ConfigureAsync();
            _isItemSelectionAttached = await _selectionInteraction.AttachAsync(_regionElement, _selfReference);
        }

        // Virtualization sizes its own scroll range, and an unpaged data view has no short page to hold a height for.
        if (EnablePagination && !Virtualize)
        {
            if (!_heightReservation.IsAttached)
            {
                await _heightReservation.AttachAsync(_regionElement);
            }

            await _heightReservation.ReserveAsync(PageSize);
        }
    }

    /// <summary>Asks the source again for the window being shown, which is how a host reports that the records changed.</summary>
    public Task RefreshDataAsync()
        => LoadAsync();

    /// <summary>The items of one window, for a view that virtualizes; sorted as the data view is.</summary>
    /// <param name="startIndex">The index of the first item asked for.</param>
    /// <param name="count">How many items are asked for.</param>
    /// <param name="cancellationToken">Canceled when the window is no longer wanted.</param>
    public ValueTask<DataViewPage<TItem>> LoadWindowAsync(int startIndex, int count, CancellationToken cancellationToken)
        => LoadWindowCoreAsync(startIndex, count, cancellationToken);

    /// <inheritdoc />
    public async Task SortByAsync(string name, DataViewSort<TItem> sort, DataViewSortDirection direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(sort);

        _sortName = name;
        _sort = sort;
        _sortDirection = direction;
        _requestedSortName = null;
        WriteAddress();

        await MoveToPageAsync(1);
        await LoadAsync();
    }

    /// <inheritdoc />
    public async Task ClearSortAsync()
    {
        _sortName = null;
        _sort = null;
        _sortDirection = DataViewSortDirection.Ascending;
        _requestedSortName = null;
        WriteAddress();

        await LoadAsync();
    }

    /// <summary>Selects every item of the set, which the selection bounds may refuse.</summary>
    public async Task SelectAllAsync()
    {
        if (Items is null)
        {
            await SelectPageAsync();
            return;
        }

        if (!CanSelectAll)
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        await ReplaceSelectionAsync([.. Items]);
    }

    /// <summary>Selects every item on screen, which the selection bounds may refuse.</summary>
    public async Task SelectPageAsync()
    {
        HashSet<TItem> updated = [.. _selectedSet, .. _items];

        if (!_selectionBounds.CanSelectAll(updated.Count))
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        await ReplaceSelectionAsync(updated);
    }

    /// <summary>Clears the selection, which is always allowed.</summary>
    public Task ClearSelectionAsync()
        => _selectedSet.Count == 0 ? Task.CompletedTask : ReplaceSelectionAsync([]);

    /// <summary>Adds <paramref name="items"/> to the selection, which the selection bounds may refuse.</summary>
    /// <param name="items">The items to select.</param>
    public async Task SelectAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        HashSet<TItem> updated = [.. _selectedSet, .. items];

        if (!_selectionBounds.CanSelectAll(updated.Count))
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        await ReplaceSelectionAsync(updated);
    }

    /// <summary>Removes <paramref name="items"/> from the selection, which the selection bounds may refuse.</summary>
    /// <param name="items">The items to clear.</param>
    public async Task DeselectAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        HashSet<TItem> updated = [.. _selectedSet];
        updated.ExceptWith(items);

        if (updated.Count > 0 && _selectionBounds.Minimum is { } minimum && updated.Count < minimum)
        {
            await RejectSelectionAsync(SelectionBoundary.Minimum);
            return;
        }

        await ReplaceSelectionAsync(updated);
    }

    /// <inheritdoc />
    public void RegisterView(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _views.Add(key);
    }

    /// <inheritdoc />
    public void UnregisterView(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _views.Remove(key);
    }

    /// <inheritdoc />
    public void BeginItemPass()
        => _itemTokens.BeginPass();

    /// <inheritdoc />
    public bool IsSelected(TItem item)
        => _selectedSet.Contains(item);

    /// <inheritdoc />
    public bool CanInteractWith(TItem item)
        => EnableCheckedSelection ? CanToggle(item) : CanSelect(item);

    /// <inheritdoc />
    public int GetItemToken(TItem item)
        => _itemTokens.GetToken(ItemKey(item), item);

    /// <inheritdoc />
    public async Task ToggleSelectionAsync(TItem item, bool selected)
    {
        if (selected == IsSelected(item))
        {
            return;
        }

        if (!CanToggle(item))
        {
            await RejectSelectionAsync(selected ? SelectionBoundary.Maximum : SelectionBoundary.Minimum);
            return;
        }

        HashSet<TItem> updated = [.. _selectedSet];

        if (selected)
        {
            updated.Add(item);
        }
        else
        {
            updated.Remove(item);
        }

        await ReplaceSelectionAsync(updated);
    }

    /// <inheritdoc />
    public Task ToggleMasterAsync()
        => _selectedSet.Count > 0 ? ClearSelectionAsync() : SelectAllAsync();

    /// <inheritdoc />
    public Task ActivateAsync(TItem item)
        => OnItemClick.InvokeAsync(item);

    /// <inheritdoc />
    public void Subscribe(Func<Task> callback)
        => _stateNotifier.Subscribe(callback);

    /// <inheritdoc />
    public void Unsubscribe(Func<Task> callback)
        => _stateNotifier.Unsubscribe(callback);

    /// <summary>Replaces or toggles the selection with a single item, invoked from the selection behavior.</summary>
    /// <param name="itemToken">The token of the item the gesture landed on.</param>
    /// <param name="additive">Whether the gesture toggles the item rather than replacing the selection with it.</param>
    [JSInvokable]
    public async Task HandleSelectAsync(int itemToken, bool additive)
    {
        if (!_itemTokens.TryGetItem(itemToken, out var item))
        {
            return;
        }

        if (additive)
        {
            await ToggleSelectionAsync(item, !IsSelected(item));
            return;
        }

        if (!CanSelect(item))
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        await ReplaceSelectionAsync([item]);
    }

    /// <summary>Replaces or extends the selection with a contiguous range of items, invoked from the selection behavior.</summary>
    /// <param name="itemTokens">The tokens of the items in the range.</param>
    /// <param name="additive">Whether the range extends the selection rather than replacing it.</param>
    [JSInvokable]
    public async Task HandleRangeSelectAsync(int[] itemTokens, bool additive)
    {
        ArgumentNullException.ThrowIfNull(itemTokens);

        HashSet<TItem> updated = additive ? [.. _selectedSet] : [];

        foreach (var token in itemTokens)
        {
            if (_itemTokens.TryGetItem(token, out var item))
            {
                updated.Add(item);
            }
        }

        // A range select is a whole-set replacement, so it is judged as a master select-all is.
        if (!_selectionBounds.CanSelectAll(updated.Count))
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        await ReplaceSelectionAsync(updated);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Add("forma-data-view")
            .Add("forma-data-view-busy", _isBusy);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_loadCancellation is not null)
        {
            await _loadCancellation.CancelAsync();
            _loadCancellation.Dispose();
        }

        await _selectionInteraction.DisposeAsync();
        await _heightReservation.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }

    private void RenderDefaultEmpty(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        => builder.AddContent(0, L["No items."]);

    private void RenderDefaultLoading(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<FormaSpinner>(0);
        builder.AddComponentParameter(1, nameof(FormaSpinner.Label), L["Loading"].Value);
        builder.CloseComponent();
    }

    // An item already selected can always be cleared; an unselected one can only be added while doing so would not exceed the maximum.
    private bool CanSelect(TItem item)
        => IsSelected(item) || _selectionBounds.CanAdd(_selectedSet.Count);

    // A box enforces both bounds: an unselected item cannot be checked once the maximum is reached, and a selected one cannot be unchecked once
    // doing so would drop below the minimum. A plain click or a range replaces the whole selection, so it is a reset rather than a removal and is exempt from the minimum.
    private bool CanToggle(TItem item)
        => IsSelected(item)
            ? _selectionBounds.CanRemove(_selectedSet.Count)
            : CanSelect(item);

    private async Task ReplaceSelectionAsync(HashSet<TItem> updated)
    {
        _selectedSet = updated;
        IReadOnlyList<TItem> reported = [.. updated];
        _selectedItemsParameter.Update(reported);
        await SelectedItemsChanged.InvokeAsync(reported);
        await NotifyStateAsync();
    }

    private Task RejectSelectionAsync(SelectionBoundary boundary)
        => OnSelectionRejected.HasDelegate
            ? OnSelectionRejected.InvokeAsync(new SelectionRejected
            {
                Boundary = boundary,
                Limit = (boundary == SelectionBoundary.Minimum ? _selectionBounds.Minimum : _selectionBounds.Maximum).GetValueOrDefault(),
            })
            : Task.CompletedTask;

    private async Task HandlePageChangedAsync(int page)
    {
        await MoveToPageAsync(page);
        await LoadAsync();
    }

    private async Task MoveToPageAsync(int page)
    {
        if (_page == page)
        {
            return;
        }

        _page = page;
        WriteAddress();
        await PageChanged.InvokeAsync(page);
    }

    private async Task LoadAsync()
    {
        if (_loadCancellation is { } superseded)
        {
            await superseded.CancelAsync();
            superseded.Dispose();
        }

        var cancellation = _loadCancellation = new CancellationTokenSource();
        _isBusy = true;
        _loadedSource = (object?)Items ?? ItemsProvider;
        _loadedPage = _page;
        _loadedPageSize = PageSize;
        _loadedPagination = EnablePagination;

        try
        {
            // A virtualizing view fetches its own windows, so the engine asks for the count alone.
            var window = Virtualize && !EnablePagination
                ? await LoadWindowCoreAsync(0, 0, cancellation.Token)
                : await LoadWindowCoreAsync(StartIndex, WindowCount, cancellation.Token);

            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            _items = window.Items;
            _hasLoaded = true;
            await SetTotalItemCountAsync(window.TotalCount);

            // A page past the last, because the rows on it went away, gives way to the last page that exists.
            if (EnablePagination && _page > 1 && _items.Count == 0 && window.TotalCount is { } total)
            {
                await MoveToPageAsync(Math.Max(1, (total + PageSize - 1) / PageSize));
                await LoadAsync();
                return;
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return;
        }
        finally
        {
            if (ReferenceEquals(_loadCancellation, cancellation))
            {
                _isBusy = false;
            }
        }

        await NotifyStateAsync();
    }

    private async ValueTask<DataViewPage<TItem>> LoadWindowCoreAsync(int startIndex, int? count, CancellationToken cancellationToken)
    {
        var request = new DataViewRequest<TItem>(startIndex, count, _sort, _sortDirection, cancellationToken);

        if (ItemsProvider is { } provider)
        {
            return await provider(request);
        }

        var query = request.ApplySorting(Items!);
        var executor = GetExecutor(query);
        var total = await executor.CountAsync(query, cancellationToken);
        var window = query.Skip(startIndex);

        if (count is { } windowCount)
        {
            window = window.Take(windowCount);
        }

        var items = await executor.ToArrayAsync(window, cancellationToken);

        return new DataViewPage<TItem>(items, total);
    }

    // The host's executor answers for the queryables it supports; every other one is enumerated where it stands.
    private IDataViewQueryExecutor GetExecutor(IQueryable<TItem> query)
        => Services.GetService<IDataViewQueryExecutor>() is { } executor && executor.Supports(query)
            ? executor
            : InMemoryQueryExecutor.Instance;

    private async Task SetTotalItemCountAsync(int? totalItemCount)
    {
        if (_totalItemCount == totalItemCount)
        {
            return;
        }

        _totalItemCount = totalItemCount;
        await TotalItemCountChanged.InvokeAsync(totalItemCount);
    }

    private async Task NotifyStateAsync()
    {
        await _stateNotifier.NotifyAsync();
        StateHasChanged();
    }

    private void ReadAddress()
    {
        if (AddressOptions is not { } options)
        {
            return;
        }

        var query = QueryHelpers.ParseQuery(new Uri(Navigation.Uri).Query);

        if (query.TryGetValue(options.GetParameterName(options.PageName), out var pageValue)
            && int.TryParse(pageValue, out var page)
            && page > 0)
        {
            _page = page;
        }

        if (query.TryGetValue(options.GetParameterName(options.SortName), out var sortValue)
            && !string.IsNullOrWhiteSpace(sortValue))
        {
            _requestedSortName = sortValue;
            _requestedSortDirection = query.TryGetValue(options.GetParameterName(options.OrderName), out var orderValue)
                && string.Equals(orderValue, DescendingOrder, StringComparison.OrdinalIgnoreCase)
                ? DataViewSortDirection.Descending
                : DataViewSortDirection.Ascending;
        }
    }

    // Written in place rather than as a new entry, so a reader stepping back leaves the page rather than retracing every sort and page they tried.
    private void WriteAddress()
    {
        if (AddressOptions is not { } options)
        {
            return;
        }

        var parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [options.GetParameterName(options.PageName)] = EnablePagination && _page > 1 ? _page : null,
            [options.GetParameterName(options.SortName)] = _sortName,
            [options.GetParameterName(options.OrderName)] = _sortName is not null && _sortDirection == DataViewSortDirection.Descending ? DescendingOrder : null,
        };

        var uri = Navigation.GetUriWithQueryParameters(parameters);

        if (!string.Equals(uri, Navigation.Uri, StringComparison.Ordinal))
        {
            Navigation.NavigateTo(uri, replace: true);
        }
    }
}
