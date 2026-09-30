namespace Forma.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Files;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Catalog.Requests;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Css;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Actions;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Navigation;
using Forma.Framework.Explorer.Selection;
using Forma.Framework.Files;
using Forma.Framework.Icons;
using Forma.Framework.Resources;
using Forma.Framework.Timing;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A file manager for what is already on the server: thumbnail, details and list views, folders, navigation, drag-and-drop organization, upload, preview, rename, and delete.</summary>
public sealed partial class FormaExplorer : FormaElementBase, IExplorerView, IExplorerInteraction
{
    private const int DefaultPageSize = 24;

    private static readonly DialogOptions DialogOptions = new()
    {
        ShowCancelAction = false,
        ShowConfirmAction = false
    };

    private static readonly FilePickerOptions UploadPickerOptions = new() { Accept = "image/*,video/*,audio/*" };

    /// <summary>The currently active folder, or null for every item.</summary>
    [Parameter] public ICatalogFolderItem? CurrentFolder { get; set; }

    /// <summary>The active page, 1-based.</summary>
    [Parameter] public int CurrentPage { get; set; } = 1;

    /// <summary>The items to display, held in full. Supply this or <see cref="ItemsProvider"/>, not both.</summary>
    [Parameter] public IReadOnlyList<ICatalogItem>? Items { get; set; }

    /// <summary>The provider the items are read from, a slice at a time. Supply this or <see cref="Items"/>, not both.</summary>
    [Parameter] public ICatalogProvider? ItemsProvider { get; set; }

    /// <summary>The folders the sidebar's tree shows and items are moved between.</summary>
    [Parameter] public IReadOnlyList<ICatalogItem>? Folders { get; set; }

    private bool ShowsSidebar
        => Options.ShowSidebar && Folders is { Count: > 0 };

    /// <summary>How many items the whole library holds, for hosts that supply one folder at a time; the supplied items are counted when null.</summary>
    [Parameter] public int? TotalCount { get; set; }

    /// <summary>How many bytes the whole library holds, for hosts that supply one folder at a time; the supplied items are measured when null.</summary>
    [Parameter] public long? TotalBytes { get; set; }

    /// <summary>The ceilings the explorer keeps, refusing anything that would cross one.</summary>
    [Parameter] public CatalogConstraints Constraints { get; set; } = CatalogConstraints.None;

    /// <summary>Raised when a ceiling stopped an operation, with the reason already written for a reader.</summary>
    [Parameter] public EventCallback<CatalogRejection> OnRejected { get; set; }

    /// <summary>How the explorer presents itself and moves through the collection.</summary>
    [Parameter] public ExplorerOptions Options { get; set; } = ExplorerOptions.Default;

    /// <summary>Which operations the explorer offers on the items it lists.</summary>
    [Parameter] public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <summary>Raised when items are deleted.</summary>
    [Parameter] public EventCallback<CatalogDeleteRequest> OnDelete { get; set; }

    /// <summary>Raised when items are dropped onto a target folder.</summary>
    [Parameter] public EventCallback<CatalogDropRequest> OnDrop { get; set; }

    /// <summary>Raised when the active folder changes.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem?> OnFolderChanged { get; set; }

    /// <summary>Raised when a request is made to create a new folder.</summary>
    [Parameter] public EventCallback<CatalogFolderCreateRequest> OnFolderCreate { get; set; }

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<CatalogFolderDeleteRequest> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder is dropped onto another folder.</summary>
    [Parameter] public EventCallback<CatalogFolderDropRequest> OnFolderDrop { get; set; }

    /// <summary>Raised when a folder rename is confirmed.</summary>
    [Parameter] public EventCallback<CatalogFolderRenameRequest> OnFolderRename { get; set; }

    /// <summary>Raised when the page changes.</summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>Raised when the refresh action is clicked.</summary>
    [Parameter] public EventCallback OnRefresh { get; set; }

    /// <summary>Raised when an item rename is confirmed.</summary>
    [Parameter] public EventCallback<CatalogRenameRequest> OnRename { get; set; }

    /// <summary>Raised when new files have been uploaded.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> OnUploaded { get; set; }

    /// <summary>The number of items shown per page.</summary>
    [Parameter] public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>Custom search placeholder text.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>The currently selected items.</summary>
    [Parameter] public IReadOnlyList<ICatalogItem> Selected { get; set; } = [];

    /// <summary>Raised when the selected items change.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ICatalogItem>> SelectedChanged { get; set; }

    /// <summary>Active sort direction.</summary>
    [Parameter] public CatalogSortDirection SortDirection { get; set; } = CatalogSortDirection.Ascending;

    /// <summary>Active sort field.</summary>
    [Parameter] public CatalogSortField SortField { get; set; } = CatalogSortField.Name;

    /// <summary>File store used to persist uploads.</summary>
    [Parameter] public IFileStore? Store { get; set; }

    /// <summary>Active view presentation mode.</summary>
    [Parameter] public ExplorerViewMode ViewMode { get; set; } = ExplorerViewMode.Thumbnails;

    /// <summary>Raised when the view mode changes.</summary>
    [Parameter] public EventCallback<ExplorerViewMode> ViewModeChanged { get; set; }

    /// <summary>The field the shown items are gathered into groups by, or none.</summary>
    [Parameter] public ExplorerGroupField GroupField { get; set; }

    /// <summary>Raised when the reader changes what the items are grouped by.</summary>
    [Parameter] public EventCallback<ExplorerGroupField> GroupFieldChanged { get; set; }

    /// <summary>Raised when the reader adds or removes a details column.</summary>
    [Parameter] public EventCallback<ExplorerDetailsColumns> DetailsColumnsChanged { get; set; }

    /// <summary>Raised when the reader shows or hides the details column header.</summary>
    [Parameter] public EventCallback<bool> DetailsHeaderVisibleChanged { get; set; }

    [Inject] private IDebounceable SearchDebouncer { get; set; } = default!;

    [Inject] private TimeProvider Clock { get; set; } = default!;

    private readonly CatalogHistory _history = new();

    private readonly ExplorerUploadPanel _uploadPanel = new();

    private readonly ParameterStateTracker _paramStateTracker = new();

    private string? _rejectionMessage;

    private readonly ExplorerPreview _preview = new();

    private readonly ExplorerItemManager _itemManager = new();

    private readonly CatalogSelection _selection = new();

    private const double MinSidebarWidth = 10;

    private const double MaxSidebarWidth = 28;

    private double _sidebarWidth = 18;

    private bool _isResizingSidebar;

    private ExplorerViewMode _viewMode = ExplorerViewMode.Thumbnails;

    private ExplorerDetailsColumns _detailsColumns;

    private bool _showsDetailsHeader = true;

    private ExplorerGroupField _groupField;

    private readonly HashSet<string> _collapsedGroups = [with(StringComparer.Ordinal)];

    private CatalogCollectionView _collection = default!;

    private readonly CatalogProviderItems _providerItems = new();

    private readonly ExplorerQueryParameters _queryParameters = new();

    private IReadOnlyList<ExplorerItemAction>? _actions;

    private CatalogFolderTree? _folderTree;

    private IReadOnlyList<ICatalogFolderItem>? _breadcrumbs;

    /// <summary>The folder list indexed, rebuilt once per parameter set because a host may mutate the list it passed.</summary>
    private CatalogFolderTree FolderTree
        => _folderTree ??= new CatalogFolderTree(Folders);

    private IReadOnlyList<ICatalogFolderItem> Breadcrumbs
        => _breadcrumbs ??= CurrentFolder is { } current ? BuildBreadcrumbs(FolderTree, current) : [];

    private static List<ICatalogFolderItem> BuildBreadcrumbs(CatalogFolderTree tree, ICatalogFolderItem current)
    {
        var path = new List<ICatalogFolderItem>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var item = tree.Find(current.Key) ?? current;

        // A host can supply a cyclic parent chain, so each folder is walked at most once.
        while (item is not null && visited.Add(item.Key))
        {
            path.Insert(0, item);
            item = tree.GetParent(item);
        }

        return path;
    }

    private bool CanGoBack => _history.CanGoBack;

    private bool CanGoForward => _history.CanGoForward;

    private bool CanGoUp => CurrentFolder is not null;

    private string DeleteConfirmMessage
    {
        get
        {
            if (_itemManager.PendingAction is not ExplorerBatchAction<ICatalogItem> { IsActive: true } deletion)
            {
                return string.Empty;
            }

            if (deletion.Targets.Count == 1)
            {
                var name = deletion.Targets[0].Name;

                return L["Are you sure you want to delete \"{Name}\"?", name];
            }

            var count = deletion.Targets.Count;

            return L["[$ plural; one: Are you sure you want to delete # item?; other: Are you sure you want to delete # items?; $]", count];
        }
    }

    private int EffectivePage
        => Math.Clamp(_collection.PageNumber, 1, PageCount);

    private ExplorerRenameAction<ICatalogItem>? PendingRename
        => _itemManager.PendingAction as ExplorerRenameAction<ICatalogItem>;

    private string RenameDialogTitle
        => PendingRename?.Target is ICatalogFolderItem ? L["Rename Folder"].Value : L["Rename Media"].Value;

    private string RenameDialogFieldLabel
        => PendingRename?.Target is ICatalogFolderItem ? L["Folder Name"].Value : L["New Name"].Value;

    private string FolderDeleteConfirmMessage
    {
        get
        {
            if (_itemManager.PendingAction is not ExplorerBatchAction<ICatalogFolderItem> { IsActive: true } deletion)
            {
                return string.Empty;
            }

            if (deletion.Targets.Count == 1)
            {
                var name = deletion.Targets[0].Name;

                return L["Are you sure you want to delete folder \"{Name}\"?", name];
            }

            var count = deletion.Targets.Count;

            return L["[$ plural; one: Are you sure you want to delete # folder?; other: Are you sure you want to delete # folders?; $]", count];
        }
    }

    private bool IsProviderMode
        => ItemsProvider is not null;

    /// <summary>
    /// How many items match the current search and sort, or null when the provider cannot say. Pages are cut in the
    /// provider's own count, so paging reads the reported total; appended slices count what they list.
    /// </summary>
    private int? MatchingCount
    {
        get
        {
            if (!IsProviderMode)
            {
                return _collection.MatchingCount;
            }

            return IsInfiniteScrolling ? _providerItems.ListedTotal : _providerItems.Total;
        }
    }

    private int PageCount
        => MatchingCount is { } total ? Math.Max(1, (int)Math.Ceiling(total / (double)PageSize)) : 1;

    private bool HasMorePages
    {
        get
        {
            if (!IsInfiniteScrolling)
            {
                return EffectivePage < PageCount;
            }

            if (!IsProviderMode)
            {
                return _collection.HasMorePages;
            }

            // The last slice's own fill decides, counted in the provider's items rather than the listed ones.
            return _providerItems.WasFull && (_providerItems.Total is not { } total || _providerItems.FetchedCount < total);
        }
    }

    private bool IsInfiniteScrolling
        => Options.InfiniteScroll != ExplorerInfiniteScrollMode.Off;

    /// <summary>The items on screen.</summary>
    private IReadOnlyList<ICatalogItem> VisibleItems
        => IsProviderMode ? _providerItems.Items : _collection.VisibleItems;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _collection = new CatalogCollectionView(item => item.GetLocalizedLabels(L).KindLabel)
        {
            PageNumber = CurrentPage,
            PageSize = PageSize,
            SortField = SortField,
            SortDirection = SortDirection,
        };

        _viewMode = ViewMode;
        _groupField = GroupField;
        _detailsColumns = Options.DetailsColumns;
        _showsDetailsHeader = Options.ShowDetailsHeader;

        _paramStateTracker.Track(() => Constraints.AcceptedResourceKinds, accepted => _collection.Filter = CatalogResourceFilter.Parse(accepted));
        _paramStateTracker.Track(() => CurrentPage, page => _collection.PageNumber = page);
        _paramStateTracker.Track(() => ViewMode, mode => _viewMode = mode);
        _paramStateTracker.Track(() => GroupField, field =>
        {
            _groupField = field;
            _collapsedGroups.Clear();
        });
        _paramStateTracker.Track(() => Options.DetailsColumns, columns => _detailsColumns = columns);
        _paramStateTracker.Track(() => Options.ShowDetailsHeader, shows => _showsDetailsHeader = shows);
        _paramStateTracker.Track(() => SortField, field => _collection.SortField = field);
        _paramStateTracker.Track(() => SortDirection, direction => _collection.SortDirection = direction);

        // Resynced only when the host supplies a different list, so an unrelated parent render cannot discard the selection.
        _paramStateTracker.Track(() => Selected, AdoptSelection);

        // Seeded once, so leaving selection mode afterwards is the reader's to decide.
        _selection.IsSelectionModeActive = Options.SelectOnOpen;

        _itemManager.OnChanged = () => _ = InvokeAsync(StateHasChanged);
        _itemManager.OnActivate = ActivateItemAsync;
        _itemManager.OnDrop = (target, folder) => RunAsync(() => HandleDropOnFolderAsync(target, folder));
        _itemManager.OnRequestNewFolder = () => RunAsync(() => OpenFolderDialogAsync(CurrentFolder));
        _itemManager.OnRenameConfirmed = (target, newName) => RunAsync(() => ApplyRenameAsync(target, newName));
        _itemManager.OnCreateFolderConfirmed = (parent, name) => RunAsync(() => ApplyCreateFolderAsync(parent, name));
        _itemManager.OnDeleteConfirmed = targets => RunAsync(() => ApplyDeleteAsync(targets));
        _itemManager.OnFolderDeleteConfirmed = folders => RunAsync(() => ApplyFolderDeleteAsync(folders));
        _itemManager.OnToggleUpload = () => Run(_uploadPanel.Toggle);
        _itemManager.SelectedItems = () => _selection.Items;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Items is null == ItemsProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FormaExplorer)} requires exactly one of {nameof(Items)} or {nameof(ItemsProvider)}.");
        }

        _collection.Source = Items ?? [];
        _collection.PageSize = PageSize;
        _collection.IsAppending = IsInfiniteScrolling;
        _collection.Invalidate();
        _folderTree = null;
        _breadcrumbs = null;

        _paramStateTracker.Apply();

        _selection.SelectionLimit = Options.MaxSelection;

        _itemManager.Actions = Actions;
        _itemManager.ItemOptions = ItemOptions;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!IsProviderMode)
        {
            return;
        }

        // The folder is the host's to know, so it is watched for change rather than sent, and a move invalidates the items shown.
        var moved = _queryParameters.Update(ItemsProvider, CurrentFolder?.Key, PageSize);

        if (!moved)
        {
            return;
        }

        _collection.PageNumber = 1;

        await QueryAsync(append: false);
    }

    /// <summary>Fetches a slice from the provider.</summary>
    private Task QueryAsync(bool append, CancellationToken cancellationToken = default)
    {
        if (ItemsProvider is not { } provider)
        {
            return Task.CompletedTask;
        }

        var query = new CatalogQuery(
            append ? _providerItems.FetchedCount : (EffectivePage - 1) * PageSize,
            PageSize,
            string.IsNullOrWhiteSpace(_collection.SearchQuery) ? null : _collection.SearchQuery,
            _collection.SortField,
            _collection.SortDirection);

        // FetchAsync lives in a library that must ConfigureAwait(false), so its continuation is not
        // guaranteed to land back on the render thread; StateHasChanged needs InvokeAsync to get there.
        return _providerItems.FetchAsync(provider, onStateChanged: () => InvokeAsync(StateHasChanged), query, _collection.Filter, append, cancellationToken);
    }

    /// <summary>Reads the items shown again.</summary>
    private Task RefreshProviderAsync()
    {
        if (!IsProviderMode)
        {
            return Task.CompletedTask;
        }

        // An append would stack a second copy of what is already shown, so a refresh always starts the items again.
        if (IsInfiniteScrolling)
        {
            _collection.PageNumber = 1;
        }

        return QueryAsync(append: false);
    }

    private int RootFolderCount
        => FolderTree.GetChildren(null).Count;

    private int KnownItemCount
        => TotalCount ?? Items?.Count ?? MatchingCount ?? 0;

    private int ListedItemCount
        => TotalCount ?? Items?.Count(item => item.Visibility != CatalogItemVisibility.Hidden) ?? MatchingCount ?? 0;

    private long KnownByteCount
        => TotalBytes ?? Items?.Sum(item => item.Resource?.Size ?? 0) ?? 0;

    // A top-level folder counts as one, so the root itself is zero.
    private int GetDepth(ICatalogFolderItem? folder)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var depth = 0;
        var current = folder;

        while (current is not null && visited.Add(current.Key))
        {
            depth++;
            current = FolderTree.GetParent(current);
        }

        return depth;
    }

    // How many levels the folder's own subtree spans, so moving it can be measured before it lands.
    private int GetHeight(ICatalogFolderItem folder)
    {
        var tallest = 0;

        foreach (var child in FolderTree.GetChildren(folder.Key))
        {
            tallest = Math.Max(tallest, GetHeight(child));
        }

        return tallest + 1;
    }

    private async Task RejectAsync(CatalogRejectionReason reason, long? allowed, string message)
    {
        _rejectionMessage = message;

        await OnRejected.InvokeAsync(new CatalogRejection(reason, allowed, message));
    }

    private async Task<bool> EnsureFolderHasRoomAsync(ICatalogFolderItem? folder, int adding)
    {
        if (Constraints.MaxFolderItemCount <= 0 || folder is null || (folder.Count ?? 0) + adding <= Constraints.MaxFolderItemCount)
        {
            return true;
        }

        await RejectAsync(
            CatalogRejectionReason.FolderItemCount,
            Constraints.MaxFolderItemCount,
            L["[$ plural; one: A folder holds at most # item.; other: A folder holds at most # items.; $]", Constraints.MaxFolderItemCount].Value);

        return false;
    }

    private async Task<bool> EnsureExplorerHasRoomAsync(int adding, long bytes)
    {
        if (Constraints.MaxItemCount > 0 && KnownItemCount + adding > Constraints.MaxItemCount)
        {
            await RejectAsync(
                CatalogRejectionReason.ItemCount,
                Constraints.MaxItemCount,
                L["[$ plural; one: This explorer holds at most # item.; other: This explorer holds at most # items.; $]", Constraints.MaxItemCount].Value);

            return false;
        }

        if (Constraints.MaxTotalBytes > 0 && KnownByteCount + bytes > Constraints.MaxTotalBytes)
        {
            await RejectAsync(
                CatalogRejectionReason.TotalBytes,
                Constraints.MaxTotalBytes,
                L["This explorer holds at most {Size}.", ResourceFormatter.FromByteCount(Constraints.MaxTotalBytes)].Value);

            return false;
        }

        return true;
    }

    private async Task<bool> EnsureFolderFitsAsync(ICatalogFolderItem? parent, int height)
    {
        if (Constraints.MaxFolderDepth > 0 && GetDepth(parent) + height > Constraints.MaxFolderDepth)
        {
            await RejectAsync(
                CatalogRejectionReason.FolderDepth,
                Constraints.MaxFolderDepth,
                L["[$ plural; one: Folders cannot nest more than # level deep.; other: Folders cannot nest more than # levels deep.; $]", Constraints.MaxFolderDepth].Value);

            return false;
        }

        if (parent is null && Constraints.MaxFolderRootCount > 0 && RootFolderCount >= Constraints.MaxFolderRootCount)
        {
            await RejectAsync(
                CatalogRejectionReason.FolderRootCount,
                Constraints.MaxFolderRootCount,
                L["[$ plural; one: This explorer holds at most # top-level folder.; other: This explorer holds at most # top-level folders.; $]", Constraints.MaxFolderRootCount].Value);

            return false;
        }

        if (parent is not null && Constraints.MaxSubfolderCount > 0 && FolderTree.GetChildren(parent.Key).Count >= Constraints.MaxSubfolderCount)
        {
            await RejectAsync(
                CatalogRejectionReason.SubfolderCount,
                Constraints.MaxSubfolderCount,
                L["[$ plural; one: A folder holds at most # folder inside it.; other: A folder holds at most # folders inside it.; $]", Constraints.MaxSubfolderCount].Value);

            return false;
        }

        return true;
    }

    private IReadOnlyList<ExplorerItemAction> Actions
        => _actions ??= BuildActions();

    ExplorerOptions IExplorerConfigurationView.Options => Options;

    bool IExplorerConfigurationView.ShowsSidebar => ShowsSidebar;

    ICatalogFolderItem? IExplorerLocationView.CurrentFolder => CurrentFolder;

    IReadOnlyList<ICatalogFolderItem> IExplorerLocationView.Breadcrumbs => Breadcrumbs;

    CatalogFolderTree IExplorerLocationView.FolderTree => FolderTree;

    bool IExplorerLocationView.CanGoBack => CanGoBack;

    bool IExplorerLocationView.CanGoForward => CanGoForward;

    bool IExplorerLocationView.CanGoUp => CanGoUp;

    CatalogSortField IExplorerContentView.SortField => _collection.SortField;

    CatalogSortDirection IExplorerContentView.SortDirection => _collection.SortDirection;

    ExplorerViewMode IExplorerContentView.ViewMode => _viewMode;

    ExplorerDetailsColumns IExplorerContentView.DetailsColumns => _detailsColumns;

    bool IExplorerContentView.ShowsDetailsHeader => _showsDetailsHeader;

    ExplorerGroupField IExplorerContentView.GroupField => _groupField;

    IReadOnlyList<ExplorerItemGroup> IExplorerContentView.VisibleGroups
        => ExplorerGrouping.GroupBy(
            VisibleItems,
            _groupField,
            _collection.SortDirection,
            item => item.GetLocalizedLabels(L).KindLabel,
            Clock.GetLocalNow(),
            CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);

    bool IExplorerContentView.IsCollapsed(ExplorerItemGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        return _collapsedGroups.Contains(group.Key);
    }

    ICatalogSelectionView IExplorerView.Selection => _selection;

    IExplorerItemView IExplorerView.Item => _itemManager;

    IReadOnlyList<ICatalogItem> IExplorerContentView.VisibleItems => VisibleItems;

    bool IExplorerContentView.IsFetching => _providerItems.IsFetching;

    int? IExplorerContentView.MatchingCount => MatchingCount;

    ExplorerPagingInfo IExplorerContentView.Paging => new(PageSize, PageCount, EffectivePage, HasMorePages);

    string IExplorerSearchView.SearchQuery => _collection.SearchQuery;

    string? IExplorerSearchView.SearchPlaceholder => SearchPlaceholder;

    // A cascaded interaction call is received by whichever descendant's own markup wired it, so its
    // implicit render trigger lands there rather than here; the render this component needs is requested
    // explicitly, through the renderer rather than inline, since some callers are still mid-dispatch of
    // their own event (a dropdown menu closing itself, for one) when the call reaches this component.
    private void Run(Action action)
    {
        action();
        _ = InvokeAsync(StateHasChanged);
    }

    private async Task RunAsync(Func<Task> action)
    {
        await action();
        await InvokeAsync(StateHasChanged);
    }

    Task IExplorerSelectionInteraction.ToggleSelectionAsync(ICatalogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.IsDisabled ? Task.CompletedTask : ChangeSelectionAsync(selection => selection.Toggle(item));
    }

    Task IExplorerContentInteraction.SortByAsync(CatalogSortField field)
        => RunAsync(() => HandleSortFieldChangedAsync(field));

    Task IExplorerContentInteraction.ChangeViewModeAsync(ExplorerViewMode mode)
        => RunAsync(() => HandleViewModeChangedAsync(mode));

    Task IExplorerContentInteraction.GroupByAsync(ExplorerGroupField field)
        => RunAsync(() => HandleGroupFieldChangedAsync(field));

    Task IExplorerContentInteraction.ToggleGroupAsync(ExplorerItemGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        if (!_collapsedGroups.Remove(group.Key))
        {
            _collapsedGroups.Add(group.Key);
        }

        return InvokeAsync(StateHasChanged);
    }

    Task IExplorerContentInteraction.ToggleDetailsColumnAsync(ExplorerDetailsColumns column)
        => RunAsync(() => HandleDetailsColumnToggledAsync(column));

    Task IExplorerContentInteraction.SetDetailsHeaderVisibleAsync(bool isVisible)
        => RunAsync(() => HandleDetailsHeaderVisibleChangedAsync(isVisible));

    Task IExplorerContentInteraction.RequestNextPageAsync()
        => RunAsync(HandleNextPageAsync);

    Task IExplorerContentInteraction.ChangePageAsync(int page)
        => RunAsync(() => HandlePageChangedAsync(page));

    Task IExplorerSearchInteraction.SetSearchQueryAsync(string query)
        => RunAsync(() => HandleSearchQueryChangedAsync(query));

    Task IExplorerLocationInteraction.GoBackAsync()
        => RunAsync(HandleBackAsync);

    Task IExplorerLocationInteraction.GoForwardAsync()
        => RunAsync(HandleForwardAsync);

    Task IExplorerLocationInteraction.GoUpAsync()
        => RunAsync(HandleUpAsync);

    Task IExplorerLocationInteraction.SelectFolderAsync(ICatalogFolderItem? folder)
        => RunAsync(() => HandleFolderSelectedAsync(folder));

    Task IExplorerContentInteraction.SetSortDirectionAsync(CatalogSortDirection direction)
        => RunAsync(() => HandleSortDirectionChangedAsync(direction));

    Task IExplorerSelectionInteraction.ToggleSelectionModeAsync()
        => RunAsync(ToggleSelectModeAsync);

    Task IExplorerSelectionInteraction.ClearSelectionAsync()
        => RunAsync(ClearSelectionAsync);

    Task IExplorerContentInteraction.RefreshAsync()
        => RunAsync(HandleRefreshAsync);

    private List<ExplorerItemAction> BuildActions()
        =>
        [
            new()
            {
                Id = "preview",
                Icon = Glyph.Inspect,
                Label = () => L["Preview"].Value,
                Requires = CatalogItemCapabilities.View,
                InvokeAsync = item =>
                {
                    if (item.Resource is { } resource)
                    {
                        OpenPreview(resource);
                        StateHasChanged();
                    }

                    return Task.CompletedTask;
                }
            },
            new()
            {
                Id = "rename",
                Icon = Glyph.Pen,
                Label = () => L["Rename"].Value,
                Requires = CatalogItemCapabilities.Rename,
                InvokeAsync = item => { _itemManager.RequestRename(item); return Task.CompletedTask; }
            },
            new()
            {
                Id = "delete",
                Icon = Glyph.Delete,
                Label = () => L["Delete"].Value,
                Requires = CatalogItemCapabilities.Delete,
                InvokeAsync = item => { _itemManager.RequestDelete(item); return Task.CompletedTask; },
                IsDestructive = true
            }
        ];

    /// <summary>Changes the selection and reports it, so no caller can do the one without the other.</summary>
    private async Task ChangeSelectionAsync(Action<CatalogSelection> change)
    {
        change(_selection);

        await SelectedChanged.InvokeAsync(_selection.Items);

        StateHasChanged();
    }

    // Adopting what the host already supplied must not report it straight back at them.
    private void AdoptSelection(IReadOnlyList<ICatalogItem> selected)
        => _selection.Replace(selected);

    private async Task ToggleSelectModeAsync()
    {
        _selection.IsSelectionModeActive = !_selection.IsSelectionModeActive;

        if (!_selection.IsSelectionModeActive)
        {
            await ClearSelectionAsync();
        }
    }

    private Task ClearSelectionAsync()
        => ChangeSelectionAsync(selection => selection.Clear());

    private string SidebarSizeStyle
        => new CssStyleBuilder().Add("--forma-explorer-sidebar-size", CssLength.FromRem(_sidebarWidth)).ToString();

    private string BodyCssClass
        => _isResizingSidebar ? "forma-explorer-body is-resizing" : "forma-explorer-body";

    private async Task HandleRefreshAsync()
    {
        await OnRefresh.InvokeAsync();
        await RefreshProviderAsync();
    }

    private async Task HandleBackAsync()
    {
        if (CanGoBack)
        {
            _history.GoBack();
            var folder = FolderTree.Find(_history.Current);
            _collection.PageNumber = 1;

            await OnFolderChanged.InvokeAsync(folder);
        }
    }

    private Task ApplyCreateFolderAsync(ICatalogFolderItem? parent, string name)
        => OnFolderCreate.InvokeAsync(new CatalogFolderCreateRequest(parent, name));

    private async Task ApplyDeleteAsync(IReadOnlyList<ICatalogItem> targets)
    {
        await ChangeSelectionAsync(selection =>
        {
            foreach (var target in targets)
            {
                selection.Remove(target);
            }
        });

        await OnDelete.InvokeAsync(new CatalogDeleteRequest(targets));
        await RefreshProviderAsync();
    }

    private async Task ApplyFolderDeleteAsync(IReadOnlyList<ICatalogFolderItem> folders)
    {
        await ClearSelectionAsync();

        foreach (var folder in folders)
        {
            await OnFolderDelete.InvokeAsync(new CatalogFolderDeleteRequest(folder));
        }
    }

    private async Task HandleFolderSelectedAsync(ICatalogFolderItem? folder)
    {
        if (CurrentFolder?.Key == folder?.Key || (folder is not null && !folder.Permits(ItemOptions, CatalogItemCapabilities.Open)))
        {
            return;
        }

        _history.Visit(folder?.Key);
        _collection.PageNumber = 1;

        await OnFolderChanged.InvokeAsync(folder);
    }

    private async Task HandleForwardAsync()
    {
        if (CanGoForward)
        {
            _history.GoForward();
            var folder = FolderTree.Find(_history.Current);
            _collection.PageNumber = 1;

            await OnFolderChanged.InvokeAsync(folder);
        }
    }

    private async Task HandleDropOnFolderAsync(ICatalogItem target, ICatalogFolderItem? targetFolder)
    {
        if (target is ICatalogFolderItem draggedFolder)
        {
            if (CanMoveFolder(draggedFolder, targetFolder)
                && await EnsureFolderFitsAsync(targetFolder, GetHeight(draggedFolder)))
            {
                await OnFolderDrop.InvokeAsync(new CatalogFolderDropRequest(draggedFolder, targetFolder));
                await RefreshProviderAsync();
            }

            return;
        }

        var selected = _selection.Items;
        IReadOnlyList<ICatalogItem> items = _selection.IsSelected(target) && selected.Count > 1
            ? [.. selected.Where(item => item.Permits(ItemOptions, CatalogItemCapabilities.Move))]
            : [target];

        if (!await EnsureFolderHasRoomAsync(targetFolder, items.Count))
        {
            return;
        }

        await OnDrop.InvokeAsync(new CatalogDropRequest(items, targetFolder));

        // A move takes the resources out of the items they were dragged from, which only a re-read will show.
        await ClearSelectionAsync();
        await RefreshProviderAsync();
    }

    private async Task HandleNextPageAsync()
    {
        if (!IsInfiniteScrolling || !HasMorePages)
        {
            return;
        }

        _collection.PageNumber = EffectivePage + 1;

        await QueryAsync(append: true);
        await OnPageChanged.InvokeAsync(_collection.PageNumber);
    }

    private async Task HandlePageChangedAsync(int page)
    {
        _collection.PageNumber = page;

        await QueryAsync(append: false);
        await OnPageChanged.InvokeAsync(page);
    }

    private bool CanMoveFolder(ICatalogFolderItem folder, ICatalogFolderItem? target)
    {
        if (target is null)
        {
            return folder.ParentKey is not null;
        }

        // Moving a folder inside its own subtree would leave the tree cyclic and unrenderable.
        return folder.Key != target.Key && !IsSelfOrDescendant(target, folder);
    }

    private bool IsSelfOrDescendant(ICatalogFolderItem candidate, ICatalogFolderItem ancestor)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = candidate;

        while (current is not null && visited.Add(current.Key))
        {
            if (current.Key == ancestor.Key)
            {
                return true;
            }

            current = FolderTree.GetParent(current);
        }

        return false;
    }

    private void HandlePreviewNavigate(string source)
    {
        var next = VisibleItems.FirstOrDefault(item => item.Resource?.Source == source);

        if (next?.Resource is { } resource)
        {
            _preview.NavigateTo(resource);
        }
    }

    private async Task ApplyRenameAsync(ICatalogItem target, string newName)
    {
        if (target is ICatalogFolderItem folder)
        {
            await OnFolderRename.InvokeAsync(new CatalogFolderRenameRequest(folder, newName));
            return;
        }

        await OnRename.InvokeAsync(new CatalogRenameRequest(target, newName));
        await RefreshProviderAsync();
    }

    private async Task HandleSearchQueryChangedAsync(string query)
    {
        _collection.SearchQuery = query;
        _collection.PageNumber = 1;

        if (!IsProviderMode)
        {
            return;
        }

        // The debouncer resumes off the renderer, so the query is dispatched back onto it.
        await SearchDebouncer.DebounceAsync(token => InvokeAsync(() => QueryAsync(append: false, token)), Options.SearchDebounceDelay);
    }

    private async Task HandleSortDirectionChangedAsync(CatalogSortDirection direction)
    {
        _collection.SortDirection = direction;
        _collection.PageNumber = 1;

        await QueryAsync(append: false);
    }

    private async Task HandleSortFieldChangedAsync(CatalogSortField field)
    {
        if (_collection.SortField == field)
        {
            _collection.SortDirection = _collection.SortDirection == CatalogSortDirection.Ascending
                ? CatalogSortDirection.Descending
                : CatalogSortDirection.Ascending;
        }
        else
        {
            _collection.SortField = field;
            _collection.SortDirection = CatalogSortDirection.Ascending;
        }

        _collection.PageNumber = 1;

        await QueryAsync(append: false);
    }

    private async Task HandleUpAsync()
    {
        if (CurrentFolder is { } current)
        {
            await HandleFolderSelectedAsync(FolderTree.GetParent(current));
        }
    }

    private async Task HandleUploadCompletedAsync(IReadOnlyList<ResourceInfo> uploaded)
    {
        _uploadPanel.Close();

        await OnUploaded.InvokeAsync(uploaded);
        await RefreshProviderAsync();
    }

    private async Task HandleUploadFilesPickedAsync(IReadOnlyList<IBrowserFile> files)
    {
        _rejectionMessage = null;

        if (_collection.Filter is { } filter
            && files.FirstOrDefault(file => !filter.Accepts(file.Name, file.ContentType)) is { } refused)
        {
            await RejectAsync(
                CatalogRejectionReason.ResourceKind,
                0,
                L["\"{Name}\" is not a kind of file this explorer accepts.", refused.Name].Value);

            return;
        }

        if (!await EnsureFolderHasRoomAsync(CurrentFolder, files.Count)
            || !await EnsureExplorerHasRoomAsync(files.Count, files.Sum(file => file.Size)))
        {
            return;
        }

        _uploadPanel.Pick(files);
    }

    private async Task HandleViewModeChangedAsync(ExplorerViewMode mode)
    {
        _viewMode = mode;

        await ViewModeChanged.InvokeAsync(mode);
    }

    private async Task HandleGroupFieldChangedAsync(ExplorerGroupField field)
    {
        if (_groupField == field)
        {
            return;
        }

        _groupField = field;
        _collapsedGroups.Clear();

        await GroupFieldChanged.InvokeAsync(field);
    }

    private async Task HandleDetailsColumnToggledAsync(ExplorerDetailsColumns column)
    {
        _detailsColumns ^= column;

        await DetailsColumnsChanged.InvokeAsync(_detailsColumns);
    }

    private async Task HandleDetailsHeaderVisibleChangedAsync(bool isVisible)
    {
        if (_showsDetailsHeader == isVisible)
        {
            return;
        }

        _showsDetailsHeader = isVisible;

        await DetailsHeaderVisibleChanged.InvokeAsync(isVisible);
    }

    // Every caller names the parent it means, so no folder here is the root rather than wherever the reader happens to be.
    private async Task OpenFolderDialogAsync(ICatalogFolderItem? parent)
    {
        _rejectionMessage = null;

        if (!await EnsureFolderFitsAsync(parent, height: 1))
        {
            return;
        }

        _itemManager.BeginCreateFolder(parent);
    }

    private Task ActivateItemAsync(ICatalogItem item)
    {
        if (_selection.IsSelectionModeActive)
        {
            return ChangeSelectionAsync(selection => selection.Toggle(item));
        }

        if (item.Resource is { } resource && item.Permits(ItemOptions, CatalogItemCapabilities.View))
        {
            OpenPreview(resource);
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private void OpenPreview(ResourceInfo resource)
    {
        // Captured once so the viewer is not handed a new list on every unrelated render.
        IReadOnlyList<string> siblings =
        [
            .. VisibleItems
                .Where(sibling => sibling.Permits(ItemOptions, CatalogItemCapabilities.View))
                .Select(sibling => sibling.Resource?.Source)
                .OfType<string>()
        ];

        _preview.Open(resource, siblings);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder
            .Add("forma-explorer")
            .Add("is-resizable", Options.Resizable)
            .Add("is-dialog", Options.AsDialog);
    }

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        _providerItems.Dispose();
        SearchDebouncer.Cancel();

        return base.DisposeAsyncCore();
    }
}
