namespace Forma.Blazor.Components.DataView;

using System.Linq.Expressions;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Navigation;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Css;
using Forma.Framework.Events;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

/// <summary>The collection component: an engine that manages a set of items and the state around them, sorted, paged, selected and kept in the address, handing what is on screen to the view that presents it.</summary>
/// <typeparam name="TItem">The item the data view manages.</typeparam>
public sealed partial class FormaDataView<TItem> : FormaElementBase, IDataView<TItem>, IDataViewInteraction<TItem>, IDataViewSwitcher, IDataViewSorter<TItem>
    where TItem : notnull
{
    private const string DescendingOrder = "desc";

    private static readonly TimeSpan LoadingAnnouncementDelay = TimeSpan.FromSeconds(1);

    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private readonly StateNotifier _stateNotifier = new();
    private readonly ItemTokenTable<TItem> _itemTokens = new();
    private readonly List<DataViewRegistration> _registrations = [];
    private readonly ParameterState<string?> _viewParameter = new();
    private readonly ParameterState<TItem?> _currentItemParameter = new();
    private readonly Dictionary<object, NodeState> _nodes = [];
    private readonly Dictionary<object, TItem?> _parents = [];
    private TItem? _currentItem;
    private object? _loadedParentKey;
    private object? _loadedSortToken;
    private string? _activeViewKey;
    private readonly ParameterState<IReadOnlyList<TItem>?> _selectedItemsParameter = new();
    private readonly ParameterState<int> _pageParameter = new();
    private readonly ParameterState<DataViewGrouping<TItem>?> _groupByParameter = new();
    private readonly HashSet<object?> _foldedGroupKeys = [];

    private ItemKeyComparer<TItem> _keyComparer = default!;
    private Func<TItem, object>? _keyedBy;
    private HashSet<TItem> _selectedSet = [];
    private IReadOnlyList<TItem> _items = [];
    private List<DataViewGroup<TItem>> _groups = [];
    private int? _totalItemCount;
    private bool _hasLoaded;
    private bool _isBusy;
    private string? _announcement;
    private bool _isLoadingAnnounced;
    private bool _hasSettled;
    private int? _settledCount;
    private int _page = 1;
    private object? _loadedSource;
    private int _loadedPage;
    private int _loadedPageSize;
    private bool _loadedPagination;
    private DataViewGrouping<TItem>? _loadedGrouping;
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
    private bool _isSelectionAttached;
    private bool _isAttachedToSelect;
    private bool _isAttachedToSeek;
    private int _orderVersion;
    private SelectionBounds _selectionBounds;

    /// <summary>The items to show, sorted, counted and paged by the data view itself. Supply this or <see cref="ItemsProvider"/>, not both. A new queryable is a new set the data view asks again, so a page keeps one across its renders until its set changes.</summary>
    [Parameter] public IQueryable<TItem>? Items { get; set; }

    /// <summary>The provider the items are read from, a slice at a time. Supply this or <see cref="Items"/>, not both.</summary>
    [Parameter] public DataViewItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>Identifies an item across reloads, so a selection or an edit survives its item being replaced by an equal copy.</summary>
    [Parameter] public Func<TItem, object> ItemKey { get; set; } = item => item;

    /// <summary>The key of the view to show among several, bindable; null shows the first declared.</summary>
    [Parameter] public string? View { get; set; }

    /// <summary>Raised with the key of the view the reader switched to.</summary>
    [Parameter] public EventCallback<string?> ViewChanged { get; set; }

    /// <summary>The name an item goes by, which a view without columns shows for it; the item's own text when omitted.</summary>
    [Parameter] public Func<TItem, string>? ItemName { get; set; }

    /// <summary>The key of the item's parent, as <see cref="ItemKey"/> reads it on the parent, null for a root, which gives a set its hierarchy; a provider answers with a node's children from the request instead.</summary>
    [Parameter] public Expression<Func<TItem, object?>>? ParentKey { get; set; }

    /// <summary>Whether an item has children, so it opens in the tree and, activated in another view, becomes the current node; absent, a node opens until its children come back empty.</summary>
    [Parameter] public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>The node whose children the views without a tree show, bindable; null shows the roots.</summary>
    [Parameter] public TItem? CurrentItem { get; set; }

    /// <summary>Raised with the node the reader moved into, or null for the roots.</summary>
    [Parameter] public EventCallback<TItem?> CurrentItemChanged { get; set; }

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

    /// <summary>The height of the unit a view virtualizes by, in pixels.</summary>
    [Parameter] public float ItemHeight { get; set; } = 32;

    /// <summary>How many units a view renders beyond each edge of the viewport while virtualizing.</summary>
    [Parameter] public int OverscanCount { get; set; } = 3;

    /// <summary>Where the viewport holds when items arrive at an edge while virtualizing.</summary>
    [Parameter] public VirtualizeAnchorMode AnchorMode { get; set; } = VirtualizeAnchorMode.Start;

    /// <summary>The index of the item the data view opens on while virtualizing, applied once; zero opens at the start.</summary>
    [Parameter] public int InitialItemIndex { get; set; }

    /// <summary>The grouping the items fold under, each group a heading over its members; null shows the items ungrouped.</summary>
    [Parameter] public DataViewGrouping<TItem>? GroupBy { get; set; }

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
    public IReadOnlyList<DataViewRegistration> Views
        => _registrations;

    /// <inheritdoc />
    public string? ActiveViewKey
        => _activeViewKey is { } key && _registrations.Any(registration => registration.Key == key && !registration.IsCompanion)
            ? key
            : _registrations.FirstOrDefault(registration => !registration.IsCompanion)?.Key;

    /// <inheritdoc />
    public bool HasHierarchy
        => ParentKey is not null || (ItemsProvider is not null && HasChildren is not null);

    /// <inheritdoc />
    TItem? IDataView<TItem>.CurrentItem
        => _currentItem;

    /// <inheritdoc />
    public IReadOnlyList<TItem> Roots
        => GetLoadedChildren(RootsKey);

    /// <inheritdoc />
    public bool AreRootsLoaded
        => _nodes.TryGetValue(RootsKey, out var node) && node.Children is not null;

    /// <inheritdoc />
    public bool IsLoadingRoots
        => _nodes.TryGetValue(RootsKey, out var node) && node.IsLoading;

    /// <inheritdoc />
    public IReadOnlyList<TItem> GetChildren(TItem parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        return GetLoadedChildren(ItemKey(parent));
    }

    /// <inheritdoc />
    public bool AreChildrenLoaded(TItem parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        return _nodes.TryGetValue(ItemKey(parent), out var node) && node.Children is not null;
    }

    /// <inheritdoc />
    public bool IsLoadingChildren(TItem parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        return _nodes.TryGetValue(ItemKey(parent), out var node) && node.IsLoading;
    }

    /// <inheritdoc />
    public bool TryGetParent(TItem node, out TItem? parent)
    {
        ArgumentNullException.ThrowIfNull(node);

        return _parents.TryGetValue(ItemKey(node), out parent);
    }

    private List<TItem> GetLoadedChildren(object key)
        => _nodes.TryGetValue(key, out var node) ? node.Children ?? [] : [];

    /// <inheritdoc />
    public bool CanOpen(TItem node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (HasChildren is { } hasChildren)
        {
            return hasChildren(node);
        }

        // Unasked, a node is presumed to open; answered with no children, it is a leaf.
        return !_nodes.TryGetValue(GetNodeKey(node), out var state) || state.Children is not { Count: 0 };
    }

    /// <inheritdoc />
    public bool IsOpen(TItem node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return _nodes.TryGetValue(GetNodeKey(node), out var state) && state.IsOpen;
    }

    /// <inheritdoc />
    Func<TItem, string> IDataView<TItem>.ItemName
        => ItemName ?? GetItemText;

    /// <inheritdoc />
    public IReadOnlyList<DataViewGroup<TItem>> Groups
        => _groups;

    // The groups are the ones read from the items on screen, which a new grouping replaces only once its items arrive.
    /// <inheritdoc />
    public bool IsGrouped
        => _groups.Count > 0;

    /// <inheritdoc />
    public bool IsFolded(DataViewGroup<TItem> group)
    {
        ArgumentNullException.ThrowIfNull(group);

        return _foldedGroupKeys.Contains(group.Key);
    }

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
        => MasterReachesSet
            ? _totalItemCount is > 0 && _selectedSet.Count >= _totalItemCount
            : _items.Count > 0 && PageSelectedCount == _items.Count;

    /// <inheritdoc />
    public bool PartiallySelected
        => (MasterReachesSet ? _selectedSet.Count : PageSelectedCount) > 0 && !AllSelected;

    /// <inheritdoc />
    public bool IsMasterControlDisabled
        => MasterReachesSet
            ? _selectedSet.Count == 0 && !CanSelectAll
            : PageSelectedCount == 0 && !CanSelectPage;

    /// <inheritdoc />
    public bool HasMasterControl
        => CheckedSelectionOptions.SelectAllMode != SelectAllMode.None && (!IsVirtualizing || HoldsSet);

    /// <inheritdoc />
    public bool HasMasterMenu
        => HasMasterControl && CheckedSelectionOptions.SelectAllMode == SelectAllMode.FromMenu && !MasterReachesSet;

    /// <inheritdoc />
    public int SelectedCount
        => _selectedSet.Count;

    /// <inheritdoc />
    public bool HoldsSet
        => Items is not null;

    /// <inheritdoc />
    public bool CanOfferSet
        => HoldsSet && _totalItemCount is { } total && total > _items.Count;

    /// <inheritdoc />
    IEqualityComparer<TItem> IDataView<TItem>.ItemComparer
        => _keyComparer;

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
        => _selectionBounds.CanSelectAll(_totalItemCount ?? _items.Count);

    private int PageSelectedCount
        => _items.Count(IsSelected);

    // The master control's state follows what it reaches: the whole set in direct mode, or where nothing pages and so no page exists to select; the items on screen otherwise.
    private bool MasterReachesSet
        => CheckedSelectionOptions.SelectAllMode == SelectAllMode.Direct || IsVirtualizing;

    // A virtualizing view fetches its own slices, so the engine holds no page of items.
    private bool IsVirtualizing
        => Virtualize && !EnablePagination;

    // Selecting the page adds what is not yet selected on it to what is selected, on this page or another.
    private bool CanSelectPage
        => _selectionBounds.CanSelectAll(_selectedSet.Count + _items.Count - PageSelectedCount);

    private bool ShowsPaginator
        => EnablePagination && _totalItemCount is { } total && total > PageSize;

    // The offer to select the whole set, once the page is selected and the set holds more than it.
    private bool OffersSet
        => EnableCheckedSelection
            && CheckedSelectionOptions.SelectAllMode == SelectAllMode.FromPager
            && CanOfferSet
            && AllSelected
            && _totalItemCount is { } total
            && _selectedSet.Count < total;

    // Skipped entirely when there is nothing to put in it, so the region's gap leaves no dead space beneath it.
    private bool HasFooter
        => Status is not null || ShowsPaginator || OffersSet;

    private int StartIndex
        => EnablePagination ? (_page - 1) * PageSize : 0;

    private int? SliceSize
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
        ArgumentOutOfRangeException.ThrowIfNegative(InitialItemIndex);

        // Every group opens again under a new grouping, whose keys mean something else.
        if (_groupByParameter.Update(GroupBy))
        {
            _foldedGroupKeys.Clear();
        }

        if (EnablePagination)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PageSize, 0);
        }

        if (Items is null == ItemsProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FormaDataView<TItem>)} requires exactly one of {nameof(Items)} or {nameof(ItemsProvider)}.");
        }

        _selectionBounds = new SelectionBounds(CheckedSelectionOptions.Minimum, CheckedSelectionOptions.Maximum);
        _selectionBounds.Validate();

        // The selection is kept by key, so an item re-fetched as an equal copy is still the item that was selected.
        var keyChanged = !ReferenceEquals(_keyedBy, ItemKey);
        if (keyChanged)
        {
            _keyedBy = ItemKey;
            _keyComparer = new ItemKeyComparer<TItem>(ItemKey);
        }

        // The set is rebuilt only when the host hands over a different list, since every change this data view reports arrives as a new one.
        if (_selectedItemsParameter.Update(SelectedItems) || keyChanged)
        {
            _selectedSet = new HashSet<TItem>(SelectedItems, _keyComparer);
        }

        // The address speaks first, once; after that a page the host sets is the page shown.
        if (_pageParameter.Update(Page))
        {
            _page = Math.Max(1, Page);
        }

        if (_viewParameter.Update(View))
        {
            _activeViewKey = View;
        }

        if (_currentItemParameter.Update(CurrentItem))
        {
            _currentItem = CurrentItem;
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
            || _loadedPagination != EnablePagination
            || !ReferenceEquals(_loadedGrouping, GroupBy)
            || !Equals(_loadedParentKey, GetNodeKey(_currentItem)))
        {
            await LoadAsync();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The views register while the data view first renders, so whatever reads them, the switch above all, is told once they have.
        if (firstRender && _registrations.Count > 0)
        {
            await NotifyStateAsync();
        }

        // The behavior answers every press on an item: under item selection a press selects and a double press activates, and
        // otherwise a press activates, as Enter on an item's marker does either way; a change of selection since it attached lets
        // it go, so it attaches again as the selection now is.
        if (_selectionInteraction.IsAttached && (_isAttachedToSelect != EnableItemSelection || _isAttachedToSeek != (ActivePositions is not null)))
        {
            await _selectionInteraction.DetachAsync();
            _isSelectionAttached = false;
        }

        if (!_isSelectionAttached)
        {
            // Claimed before the await so a render arriving mid-call cannot re-enter and attach a second listener to the same region.
            _isSelectionAttached = true;
            _isAttachedToSelect = EnableItemSelection;
            _isAttachedToSeek = ActivePositions is not null;
            _selfReference ??= DotNetObjectReference.Create(this);

            // The keys are routes the script declares itself, so the host's policy reaches the router before they do.
            await InputRouter.ConfigureAsync();
            _isSelectionAttached = await _selectionInteraction.AttachAsync(_regionElement, _selfReference, ItemSelector, ItemAttribute, ControlSelector, _isAttachedToSelect, PositionAttribute, _isAttachedToSeek);
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

    /// <summary>Reads the items being shown again.</summary>
    public Task RefreshDataAsync()
        => LoadAsync();

    /// <summary>Folds the group keyed <paramref name="key"/> away under its heading, its items hidden until it opens again.</summary>
    /// <param name="key">The group's key.</param>
    public Task FoldGroupAsync(object? key)
        => _foldedGroupKeys.Add(key) ? NotifyStateAsync() : Task.CompletedTask;

    /// <summary>Opens the group keyed <paramref name="key"/> again, its items shown under its heading.</summary>
    /// <param name="key">The group's key.</param>
    public Task UnfoldGroupAsync(object? key)
        => _foldedGroupKeys.Remove(key) ? NotifyStateAsync() : Task.CompletedTask;

    /// <summary>Brings the item at <paramref name="index"/> on screen: a paged data view moves to its page, a virtualizing view scrolls it to the start of its viewport.</summary>
    /// <param name="index">The index of the item in the whole set.</param>
    /// <param name="cancellationToken">Canceled when the scroll is no longer wanted.</param>
    public async Task ScrollToItemAsync(int index, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (EnablePagination)
        {
            await HandlePageChangedAsync(index / PageSize + 1);
            return;
        }

        if (ActiveViewport is { } viewport)
        {
            await viewport.ScrollToItemAsync(index, cancellationToken);
        }
    }

    /// <inheritdoc />
    public ValueTask<DataViewPage<TItem>> LoadSliceAsync(int startIndex, int count, CancellationToken cancellationToken)
        => LoadSliceCoreAsync(startIndex, count, cancellationToken);

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

    /// <summary>Selects every item of the set where the data view holds it, else every item on screen; the selection bounds may refuse.</summary>
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

        var whole = await LoadSliceCoreAsync(0, null, CancellationToken.None);
        await ReplaceSelectionAsync(CreateSelection(whole.Items));
    }

    /// <summary>Selects every item on screen, which the selection bounds may refuse.</summary>
    public async Task SelectPageAsync()
    {
        if (!CanSelectPage)
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        var updated = CreateSelection(_selectedSet);
        updated.UnionWith(_items);
        await ReplaceSelectionAsync(updated);
    }

    /// <summary>Clears the selection, which is always allowed.</summary>
    public Task ClearSelectionAsync()
        => _selectedSet.Count == 0 ? Task.CompletedTask : ReplaceSelectionAsync(CreateSelection([]));

    /// <summary>Clears the items on screen from the selection; a reset rather than a removal, so the floor does not bind.</summary>
    public Task ClearPageAsync()
    {
        if (PageSelectedCount == 0)
        {
            return Task.CompletedTask;
        }

        var updated = CreateSelection(_selectedSet);
        updated.ExceptWith(_items);

        return ReplaceSelectionAsync(updated);
    }

    /// <summary>Adds <paramref name="items"/> to the selection, which the selection bounds may refuse.</summary>
    /// <param name="items">The items to select.</param>
    public async Task SelectAsync(IEnumerable<TItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var updated = CreateSelection(_selectedSet);
        updated.UnionWith(items);

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

        var updated = CreateSelection(_selectedSet);
        updated.ExceptWith(items);

        if (updated.Count > 0 && _selectionBounds.Minimum is { } minimum && updated.Count < minimum)
        {
            await RejectSelectionAsync(SelectionBoundary.Minimum);
            return;
        }

        await ReplaceSelectionAsync(updated);
    }

    /// <inheritdoc />
    public void RegisterView(DataViewRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        UnregisterView(registration.Key);
        _registrations.Add(registration);
    }

    /// <inheritdoc />
    public void UnregisterView(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _registrations.RemoveAll(registration => registration.Key == key);
    }

    /// <inheritdoc />
    public async Task SwitchViewAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!_registrations.Any(registration => registration.Key == key))
        {
            throw new ArgumentException($"No view is registered under '{key}'.", nameof(key));
        }

        if (key == ActiveViewKey)
        {
            return;
        }

        // A move still waiting for its item was the last view's, and the next view draws other items at its position.
        await _selectionInteraction.CancelSeekAsync();
        _activeViewKey = key;
        WriteAddress();
        await ViewChanged.InvokeAsync(key);
        await NotifyStateAsync();
    }

    // A companion view stands beside the active view, so the region lays them out across rather than down.
    private string RegionClass
        => _registrations.Any(registration => registration.IsCompanion)
            ? "forma-data-view-region forma-data-view-region-companions"
            : "forma-data-view-region";

    // The viewport of the view showing, which the engine scrolls and asks to fetch again.
    private IDataViewViewport? ActiveViewport
        => _registrations.FirstOrDefault(registration => registration.Key == ActiveViewKey)?.Viewport;

    // A virtualizing view reaches items beyond the ones it draws, by their positions in its order.
    private IDataViewPositions<TItem>? ActivePositions
        => IsVirtualizing ? ActiveViewport as IDataViewPositions<TItem> : null;

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
    public Task ToggleGroupAsync(DataViewGroup<TItem> group)
    {
        ArgumentNullException.ThrowIfNull(group);

        return IsFolded(group) ? UnfoldGroupAsync(group.Key) : FoldGroupAsync(group.Key);
    }

    /// <inheritdoc />
    public Task LoadRootsAsync()
        => LoadChildrenCoreAsync(default);

    /// <inheritdoc />
    public Task LoadChildrenAsync(TItem parent)
    {
        ArgumentNullException.ThrowIfNull(parent);

        return LoadChildrenCoreAsync(parent);
    }

    // The source is asked once for a node's children, or the roots for none, and the answer kept until the source or the sort changes.
    private async Task LoadChildrenCoreAsync(TItem? parent)
    {
        var key = GetNodeKey(parent);

        if (!_nodes.TryGetValue(key, out var node))
        {
            node = new NodeState();
            _nodes[key] = node;
        }

        if ((node.Children is not null && !node.IsStale) || node.IsLoading)
        {
            return;
        }

        node.Item = parent;
        node.IsLoading = true;
        await NotifyStateAsync();

        try
        {
            var answer = await AskForChildrenAsync(parent, CancellationToken.None);
            node.Children = [.. answer.Items];
            node.IsStale = false;
            RecordParent(answer.Items, parent);
        }
        finally
        {
            node.IsLoading = false;
        }

        await NotifyStateAsync();
    }

    /// <inheritdoc />
    public async Task OpenNodeAsync(TItem node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!CanOpen(node))
        {
            return;
        }

        GetNodeState(node).IsOpen = true;
        await LoadChildrenCoreAsync(node);
        await NotifyStateAsync();
    }

    /// <inheritdoc />
    public Task CloseNodeAsync(TItem node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!_nodes.TryGetValue(GetNodeKey(node), out var state) || !state.IsOpen)
        {
            return Task.CompletedTask;
        }

        state.IsOpen = false;

        return NotifyStateAsync();
    }

    /// <inheritdoc />
    public async Task MakeCurrentAsync(TItem? item)
    {
        if (Equals(GetNodeKey(item), GetNodeKey(_currentItem)))
        {
            return;
        }

        _currentItem = item;
        await CurrentItemChanged.InvokeAsync(item);

        // A node's children are a set of their own, which starts on its first page.
        await MoveToPageAsync(1);
        await LoadAsync();
    }

    /// <inheritdoc />
    public Task SelectItemAsync(TItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return SelectAloneAsync(item);
    }

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

        var updated = CreateSelection(_selectedSet);

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
        => CheckedSelectionOptions.SelectAllMode switch
        {
            SelectAllMode.None => Task.CompletedTask,
            _ when MasterReachesSet => _selectedSet.Count > 0 ? ClearSelectionAsync() : SelectAllAsync(),
            _ => PageSelectedCount > 0 ? ClearPageAsync() : SelectPageAsync(),
        };

    /// <inheritdoc />
    public Task ActivateAsync(TItem item)
        => HasHierarchy && HasChildren is { } hasChildren && hasChildren(item)
            ? MakeCurrentAsync(item)
            : OnItemClick.InvokeAsync(item);

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

        await SelectAloneAsync(item);
    }

    // A plain press selects the item alone, which the bounds may refuse.
    private async Task SelectAloneAsync(TItem item)
    {
        if (!CanSelect(item))
        {
            await RejectSelectionAsync(SelectionBoundary.Maximum);
            return;
        }

        await ReplaceSelectionAsync(CreateSelection([item]));
    }

    /// <summary>Activates the item whose marker held the focus when Enter was pressed, invoked from the selection behavior.</summary>
    /// <param name="itemToken">The token of the item.</param>
    [JSInvokable]
    public Task HandleActivateAsync(int itemToken)
        => _itemTokens.TryGetItem(itemToken, out var item) ? ActivateAsync(item) : Task.CompletedTask;

    /// <summary>Replaces or extends the selection with a contiguous range of items, invoked from the selection behavior.</summary>
    /// <param name="itemTokens">The tokens of the items in the range.</param>
    /// <param name="additive">Whether the range extends the selection rather than replacing it.</param>
    [JSInvokable]
    public Task HandleRangeSelectAsync(int[] itemTokens, bool additive)
    {
        ArgumentNullException.ThrowIfNull(itemTokens);

        List<TItem> range = [];

        foreach (var token in itemTokens)
        {
            if (_itemTokens.TryGetItem(token, out var item))
            {
                range.Add(item);
            }
        }

        return SelectRangeAsync(range, additive);
    }

    /// <summary>Replaces or extends the selection with the items the active view reaches between two positions in its order, invoked from the selection behavior where the view virtualizes.</summary>
    /// <param name="fromPosition">The position of the range's anchor.</param>
    /// <param name="toPosition">The position of the item the gesture landed on.</param>
    /// <param name="additive">Whether the range extends the selection rather than replacing it.</param>
    [JSInvokable]
    public async Task HandleRangeSelectByPositionAsync(int fromPosition, int toPosition, bool additive)
    {
        if (ActivePositions is not { } positions)
        {
            return;
        }

        var order = _orderVersion;
        var range = await positions.ReadAsync(Math.Min(fromPosition, toPosition), Math.Max(fromPosition, toPosition), CancellationToken.None);

        // The positions were read against the order as it stood; a new one leaves them naming other items.
        if (order == _orderVersion)
        {
            await SelectRangeAsync(range, additive);
        }
    }

    /// <summary>Moves to the first or the last item the active view reaches, invoked from the selection behavior where the view virtualizes: the move selects by its rules, the view scrolls the item into range, and the behavior lands the focus on it once it is drawn.</summary>
    /// <param name="request">The request the behavior names the move by.</param>
    /// <param name="last">Whether the move reaches the last item rather than the first.</param>
    /// <param name="mode">How the move selects: <c>select</c> the item alone, <c>extend</c> the range from the anchor, or <c>focus</c> the item alone.</param>
    /// <param name="anchorPosition">The position of the range's anchor, for a move that extends.</param>
    [JSInvokable]
    public async Task HandleSeekAsync(int request, bool last, string mode, int? anchorPosition)
    {
        var order = _orderVersion;
        var view = ActiveViewKey;

        // A move the order or the view changed under is let go, so it never takes the focus back afterward.
        bool IsCurrent()
        {
            return order == _orderVersion && view == ActiveViewKey;
        }

        // Every way out answers the behavior, landing the focus or letting the move go, so a move that failed never holds its end.
        var isLanded = false;

        try
        {
            if (ActivePositions is not { } positions || positions.GetEdgePosition(last) is not { } position)
            {
                return;
            }

            if (mode == SeekSelect && await positions.ReadAsync(position, position, CancellationToken.None) is [var item] && IsCurrent())
            {
                await SelectAloneAsync(item);
            }
            else if (mode == SeekExtend && anchorPosition is { } anchor)
            {
                var range = await positions.ReadAsync(Math.Min(anchor, position), Math.Max(anchor, position), CancellationToken.None);

                if (IsCurrent())
                {
                    await SelectRangeAsync(range, additive: false);
                }
            }

            if (IsCurrent())
            {
                await positions.ScrollToPositionAsync(position, CancellationToken.None);
            }

            if (IsCurrent())
            {
                isLanded = true;
                await _selectionInteraction.FocusItemAsync(request, position);
            }
        }
        finally
        {
            if (!isLanded)
            {
                await _selectionInteraction.CancelSeekAsync();
            }
        }
    }

    // A range select is a whole-set replacement, so it is judged as a master select-all is.
    private async Task SelectRangeAsync(IEnumerable<TItem> range, bool additive)
    {
        var updated = CreateSelection(additive ? _selectedSet : []);
        updated.UnionWith(range);

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

    private HashSet<TItem> CreateSelection(IEnumerable<TItem> items)
        => new(items, _keyComparer);

    private static string GetItemText(TItem item)
        => item.ToString() ?? string.Empty;

    // The roots stand under no node, so they are kept under a key of their own.
    private static readonly object RootsKey = new();

    private object GetNodeKey(TItem? node)
        => node is null ? RootsKey : ItemKey(node);

    private NodeState GetNodeState(TItem node)
    {
        var key = GetNodeKey(node);

        if (!_nodes.TryGetValue(key, out var state))
        {
            state = new NodeState();
            _nodes[key] = state;
        }

        return state;
    }

    private void RecordParent(IReadOnlyList<TItem> children, TItem? parent)
    {
        foreach (var child in children)
        {
            _parents[ItemKey(child)] = parent;
        }
    }

    // A node's children come sorted as the data view is, whole and ungrouped, since the tree shows a level at a time.
    private async ValueTask<DataViewPage<TItem>> AskForChildrenAsync(TItem? parent, CancellationToken cancellationToken)
    {
        var request = new DataViewRequest<TItem>(0, null, parent, null, _sort, _sortDirection, cancellationToken);

        if (ItemsProvider is { } provider)
        {
            return await provider(request);
        }

        var query = request.ApplySorting(FilterChildren(Items!, parent));
        var items = await GetExecutor(query).ToArrayAsync(query, cancellationToken);

        return new DataViewPage<TItem>(items, items.Length);
    }

    // The items naming parent as theirs, compared in the store on the member the parent key reads, so a store-backed set filters where it stands.
    private IQueryable<TItem> FilterChildren(IQueryable<TItem> source, TItem? parent)
    {
        if (ParentKey is not { } parentKey)
        {
            return source;
        }

        var body = parentKey.Body;

        while (body is UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked } conversion)
        {
            body = conversion.Operand;
        }

        var key = parent is null ? null : ItemKey(parent);

        // A member that cannot be null names no root, so the roots are the items whose parent is nowhere in the set.
        if (key is null && body.Type.IsValueType && Nullable.GetUnderlyingType(body.Type) is null)
        {
            return source.Where(Expression.Lambda<Func<TItem, bool>>(Expression.Constant(false), parentKey.Parameters[0]));
        }

        // The key is typed as the parent's own; a nullable member takes it converted, so an int key compares with an int? parent.
        Expression constant = Expression.Constant(key, key is null ? body.Type : key.GetType());

        if (constant.Type != body.Type)
        {
            constant = Expression.Convert(constant, body.Type);
        }

        var comparison = Expression.Equal(body, constant);

        return source.Where(Expression.Lambda<Func<TItem, bool>>(comparison, parentKey.Parameters[0]));
    }

    /// <summary>What the data view keeps of a node the tree has opened or asked about: the node, its children once answered and whether they came from a source since replaced, and whether it is open or still loading.</summary>
    private sealed class NodeState
    {
        public TItem? Item { get; set; }

        public List<TItem>? Children { get; set; }

        public bool IsStale { get; set; }

        public bool IsOpen { get; set; }

        public bool IsLoading { get; set; }
    }

    // The roots and every open node, the nodes a tree shows, are asked for their children again from the source as it now is; a closed node asks when it opens.
    private async Task RefreshStaleNodesAsync()
    {
        var shown = _nodes.Where(pair => pair.Value.IsStale && (ReferenceEquals(pair.Key, RootsKey) || pair.Value.IsOpen)).ToList();

        foreach (var (key, node) in shown)
        {
            await LoadChildrenCoreAsync(ReferenceEquals(key, RootsKey) ? default : node.Item);
        }
    }

    /// <summary>The attribute an element carries to be an item, whatever the view draws it as, its value the item's token.</summary>
    internal const string ItemAttribute = "data-item";

    // The selection interaction reads a click and a key against the item the marker sits in, which every view marks the same way.
    private const string ItemSelector = "[" + ItemAttribute + "]";

    /// <summary>The attribute a part of an item carries when it holds the item's own controls, its box's cell or a card's actions, whose press activates nothing.</summary>
    internal const string ControlAttribute = "data-item-control";

    // The attribute an item a virtualizing view draws carries its position in the view's order in.
    private const string PositionAttribute = "data-item-position";

    private const string SeekSelect = "select";

    private const string SeekExtend = "extend";

    private const string ControlSelector = "[" + ControlAttribute + "]";

    // The items arrive with each group's members standing together, so a group is a run of one key.
    // A virtualizing view fetches the items as it scrolls, so its groups are the whole set's, known by their sizes alone.
    private List<DataViewGroup<TItem>> CreateGroups(IReadOnlyList<DataViewGroupSize>? sizes)
        => GroupBy is { } grouping && sizes is not null
            ? [.. sizes.Where(size => size.Count > 0).Select(size => new DataViewGroup<TItem>(size.Key, grouping.GetName(size.Key), []) { Count = size.Count })]
            : [];

    private List<DataViewGroup<TItem>> CreateGroups(IReadOnlyList<TItem> items)
    {
        if (GroupBy is not { } grouping || items.Count == 0)
        {
            return [];
        }

        List<DataViewGroup<TItem>> groups = [];
        List<TItem> members = [];
        var key = grouping.GetKey(items[0]);

        foreach (var item in items)
        {
            var itemKey = grouping.GetKey(item);

            if (!Equals(itemKey, key))
            {
                groups.Add(new DataViewGroup<TItem>(key, grouping.GetName(key), members));
                members = [];
                key = itemKey;
            }

            members.Add(item);
        }

        groups.Add(new DataViewGroup<TItem>(key, grouping.GetName(key), members));

        return groups;
    }

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
        // Every load may order the items anew, so a position read against the last order names nothing now.
        _orderVersion++;
        if (_loadCancellation is { } superseded)
        {
            await superseded.CancelAsync();
            superseded.Dispose();
        }

        var cancellation = _loadCancellation = new CancellationTokenSource();
        var source = (object?)Items ?? ItemsProvider;
        _isBusy = true;
        _ = AnnounceLoadingAsync(cancellation);
        _loadedPage = _page;
        _loadedPageSize = PageSize;
        _loadedPagination = EnablePagination;
        _loadedGrouping = GroupBy;
        _loadedParentKey = GetNodeKey(_currentItem);

        // The children a tree keeps were sorted and read from the source as it was, so a new source or sort asks again; the nodes the
        // reader opened stay open, and their children go on showing until the answer comes. A fresh state per node leaves an answer
        // still on its way from the old source nowhere to land.
        object sortToken = (_sortName, _sortDirection);
        var isSourceNew = !ReferenceEquals(_loadedSource, source) || !Equals(_loadedSortToken, sortToken);

        if (isSourceNew)
        {
            foreach (var (key, node) in _nodes.ToList())
            {
                _nodes[key] = new NodeState { Item = node.Item, Children = node.Children, IsStale = node.Children is not null, IsOpen = node.IsOpen };
            }

            _loadedSortToken = sortToken;
        }

        _loadedSource = source;

        try
        {
            // A virtualizing view fetches its own slices, so the engine asks for the count alone, with the size of each group where it groups.
            var page = IsVirtualizing
                ? await LoadSliceCoreAsync(0, 0, cancellation.Token, includesGroupSizes: GroupBy is not null)
                : await LoadSliceCoreAsync(StartIndex, SliceSize, cancellation.Token);

            if (cancellation.IsCancellationRequested)
            {
                return;
            }

            _items = page.Items;
            _groups = IsVirtualizing ? CreateGroups(page.GroupSizes) : CreateGroups(page.Items);
            _hasLoaded = true;
            await SetTotalItemCountAsync(page.TotalCount);

            // A virtualizing view fetches its own slices, so it is asked to fetch them again.
            if (IsVirtualizing && ActiveViewport is { } viewport)
            {
                await viewport.RefreshAsync();
            }

            // A page past the last, because the rows on it went away, gives way to the last page that exists.
            if (EnablePagination && _page > 1 && _items.Count == 0 && page.TotalCount is { } total)
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

        AnnounceSettled();
        await NotifyStateAsync();

        if (isSourceNew)
        {
            await RefreshStaleNodesAsync();
        }
    }

    // A load still answering after a moment is announced as loading; a quicker one says nothing until it settles.
    private async Task AnnounceLoadingAsync(CancellationTokenSource cancellation)
    {
        try
        {
            await Task.Delay(LoadingAnnouncementDelay, cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!_isBusy || !ReferenceEquals(_loadCancellation, cancellation))
        {
            return;
        }

        _isLoadingAnnounced = true;
        _announcement = L["Loading"].Value;
        StateHasChanged();
    }

    // A settled load announces the count when it changed, or after loading was announced; the first quiet load says nothing.
    private void AnnounceSettled()
    {
        if (_isLoadingAnnounced || (_hasSettled && _settledCount != _totalItemCount))
        {
            _announcement = _totalItemCount is { } count
                ? L["[$ plural; one: # item; other: # items; $]", count].Value
                : L["Loaded"].Value;
        }

        _isLoadingAnnounced = false;
        _hasSettled = true;
        _settledCount = _totalItemCount;
    }

    private async ValueTask<DataViewPage<TItem>> FetchSliceAsync(int startIndex, int? count, bool includesGroupSizes, CancellationToken cancellationToken)
    {
        // The views without a tree show the current node's children, which over a set means the items naming it as parent.
        var request = new DataViewRequest<TItem>(startIndex, count, _currentItem, GroupBy, _sort, _sortDirection, cancellationToken)
        {
            IncludesGroupSizes = includesGroupSizes,
        };

        if (ItemsProvider is { } provider)
        {
            var answer = await provider(request);

            return includesGroupSizes && answer.GroupSizes is null
                ? throw new InvalidOperationException(
                    $"A grouped data view that virtualizes asks its {nameof(ItemsProvider)} for the size of each group; answer a request whose {nameof(DataViewRequest<>.IncludesGroupSizes)} is set with {nameof(DataViewPage<>.GroupSizes)}.")
                : answer;
        }

        var children = FilterChildren(Items!, _currentItem);
        var query = request.ApplySorting(children);
        var executor = GetExecutor(query);
        var total = await executor.CountAsync(query, cancellationToken);
        var slice = query.Skip(startIndex);

        if (count is { } sliceSize)
        {
            slice = slice.Take(sliceSize);
        }

        var items = await executor.ToArrayAsync(slice, cancellationToken);
        var groupSizes = includesGroupSizes ? await executor.ToArrayAsync(request.CountGroups(children), cancellationToken) : null;

        return new DataViewPage<TItem>(items, total) { GroupSizes = groupSizes };
    }

    // Every slice is a set of the current node's children, so each item in it is remembered under that node.
    private async ValueTask<DataViewPage<TItem>> LoadSliceCoreAsync(int startIndex, int? count, CancellationToken cancellationToken, bool includesGroupSizes = false)
    {
        var page = await FetchSliceAsync(startIndex, count, includesGroupSizes, cancellationToken);

        if (HasHierarchy && !cancellationToken.IsCancellationRequested)
        {
            RecordParent(page.Items, _currentItem);
        }

        return page;
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

        if (query.TryGetValue(options.GetParameterName(options.ViewName), out var viewValue)
            && !string.IsNullOrWhiteSpace(viewValue))
        {
            _activeViewKey = viewValue;
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
            [options.GetParameterName(options.ViewName)] = ActiveViewKey is { } view && view != _registrations[0].Key ? view : null,
        };

        var uri = Navigation.GetUriWithQueryParameters(parameters);

        if (!string.Equals(uri, Navigation.Uri, StringComparison.Ordinal))
        {
            Navigation.NavigateTo(uri, replace: true);
        }
    }
}
