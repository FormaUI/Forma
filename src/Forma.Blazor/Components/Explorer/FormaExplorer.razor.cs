namespace Forma.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Threading;
using Forma.Features.Explorer;
using Forma.Features.Files;
using Forma.Framework.Icons;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A server-side media library manager with thumbnail/details/list views, folders, navigation, drag-and-drop organization, upload, preview, rename, and delete.</summary>
public sealed partial class FormaExplorer : ComponentBase, IDisposable, IExplorerView, IExplorerInteraction
{
    private const int DefaultPageSize = 24;

    private static readonly ModalOptions DialogModalOptions = new()
    {
        ShowCancelAction = false,
        ShowConfirmAction = false
    };

    private static readonly FilePickerOptions UploadPickerOptions = new() { Accept = "image/*,video/*,audio/*" };

    /// <summary>The currently active folder, or null for all media.</summary>
    [Parameter] public FolderResourceInfo? CurrentFolder { get; set; }

    /// <summary>The active page, 1-based.</summary>
    [Parameter] public int CurrentPage { get; set; } = 1;

    /// <summary>The items to display, held in full. Supply this or <see cref="ItemsProvider"/>, not both.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem>? Items { get; set; }

    /// <summary>Answers for one window of items at a time, for collections too large to hold. Supply this or <see cref="Items"/>, not both.</summary>
    [Parameter] public IExplorerItemsProvider? ItemsProvider { get; set; }

    /// <summary>Available folders for grouping media.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem>? Folders { get; set; }

    private bool ShowsSidebar
        => Options.ShowSidebar && Folders is { Count: > 0 };

    /// <summary>How many resources the whole library holds, for hosts that supply one folder at a time; the supplied resources are counted when null.</summary>
    [Parameter] public int? TotalCount { get; set; }

    /// <summary>How many bytes the whole library holds, for hosts that supply one folder at a time; the supplied resources are measured when null.</summary>
    [Parameter] public long? TotalBytes { get; set; }

    /// <summary>The ceilings the explorer keeps, refusing anything that would cross one.</summary>
    [Parameter] public ExplorerConstraints Constraints { get; set; } = ExplorerConstraints.None;

    /// <summary>Raised when a ceiling stopped an operation, with the reason already written for a reader.</summary>
    [Parameter] public EventCallback<ExplorerRejection> OnRejected { get; set; }

    /// <summary>How the explorer presents itself and moves through the collection.</summary>
    [Parameter] public ExplorerOptions Options { get; set; } = ExplorerOptions.Default;

    /// <summary>Which operations the explorer offers on the items it lists.</summary>
    [Parameter] public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <summary>Raised when media resources are deleted.</summary>
    [Parameter] public EventCallback<ExplorerDeleteRequest> OnDelete { get; set; }

    /// <summary>Raised when media resources are dropped onto a target folder.</summary>
    [Parameter] public EventCallback<ExplorerDropRequest> OnDrop { get; set; }

    /// <summary>Raised when the active folder changes.</summary>
    [Parameter] public EventCallback<FolderResourceInfo?> OnFolderChanged { get; set; }

    /// <summary>Raised when a request is made to create a new folder.</summary>
    [Parameter] public EventCallback<ExplorerFolderCreateRequest> OnFolderCreate { get; set; }

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<ExplorerFolderDeleteRequest> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder is dropped onto another folder.</summary>
    [Parameter] public EventCallback<ExplorerFolderDropRequest> OnFolderDrop { get; set; }

    /// <summary>Raised when a folder rename is confirmed.</summary>
    [Parameter] public EventCallback<ExplorerFolderRenameRequest> OnFolderRename { get; set; }

    /// <summary>Raised when the page changes.</summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>Raised when the refresh action is clicked.</summary>
    [Parameter] public EventCallback OnRefresh { get; set; }

    /// <summary>Raised when a resource rename is confirmed.</summary>
    [Parameter] public EventCallback<ExplorerRenameRequest> OnRename { get; set; }

    /// <summary>Raised when new media has been uploaded.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> OnUploaded { get; set; }

    /// <summary>The number of media resources shown per page.</summary>
    [Parameter] public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>Custom search placeholder text.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>The currently selected items.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem> Selected { get; set; } = [];

    /// <summary>Raised when the selected items change.</summary>
    [Parameter] public EventCallback<IReadOnlyList<IExplorerItem>> SelectedChanged { get; set; }

    /// <summary>Active sort direction.</summary>
    [Parameter] public ExplorerSortDirection SortDirection { get; set; } = ExplorerSortDirection.Ascending;

    /// <summary>Active sort field.</summary>
    [Parameter] public ExplorerSortField SortField { get; set; } = ExplorerSortField.Name;

    /// <summary>File store used to persist uploads.</summary>
    [Parameter] public IFileStore? Store { get; set; }

    /// <summary>Active view presentation mode.</summary>
    [Parameter] public ExplorerViewMode ViewMode { get; set; } = ExplorerViewMode.Thumbnails;

    /// <summary>Raised when the view mode changes.</summary>
    [Parameter] public EventCallback<ExplorerViewMode> ViewModeChanged { get; set; }

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private readonly ExplorerHistory _history = new();

    private readonly ExplorerUploadPanel _uploadPanel = new();

    private readonly ParameterState<string?> _acceptParameter = new();

    private string? _rejectionMessage;

    private readonly ExplorerPreview _preview = new();

    private readonly ExplorerItemManager _itemManager = new();

    private readonly ExplorerSelection _selection = new();

    private const double MinSidebarWidth = 10;

    private const double MaxSidebarWidth = 28;

    private double _sidebarWidth = 18;

    private bool _isResizingSidebar;

    private readonly ParameterState<IReadOnlyList<IExplorerItem>?> _selectedParameter = new();

    private readonly ParameterState<ExplorerViewMode> _viewModeParameter = new();

    private readonly ParameterState<ExplorerSortField> _sortFieldParameter = new();

    private readonly ParameterState<ExplorerSortDirection> _sortDirectionParameter = new();

    private readonly ParameterState<int> _currentPageParameter = new();

    private ExplorerViewMode _viewMode = ExplorerViewMode.Thumbnails;

    private ExplorerCollectionView _collection = default!;

    private readonly ExplorerProviderWindow _providerWindow = new();

    private readonly Debouncer _searchDebouncer = new();

    private readonly ExplorerQueryParameters _queryParameters = new();

    private ExplorerItemOptions? _actionOptions;

    private IReadOnlyList<ExplorerItemAction> _actions = [];

    private ExplorerFolderTree? _folderTree;

    private IReadOnlyList<IExplorerFolderItem>? _breadcrumbs;

    /// <summary>The folder list indexed, rebuilt once per parameter set because a host may mutate the list it passed.</summary>
    private ExplorerFolderTree FolderTree
        => _folderTree ??= new ExplorerFolderTree(Folders);

    private IReadOnlyList<IExplorerFolderItem> Breadcrumbs
        => _breadcrumbs ??= CurrentFolder is null ? [] : BuildBreadcrumbs(FolderTree, CurrentFolder);

    private FolderResourceInfo? FolderAt(string? source)
        => FolderTree.Find(source)?.Resource;

    private static List<IExplorerFolderItem> BuildBreadcrumbs(ExplorerFolderTree tree, FolderResourceInfo? current)
    {
        var path = new List<IExplorerFolderItem>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var item = current is null ? null : tree.Find(current.Source) ?? ExplorerItem.ForFolder(current);

        // A host can supply a cyclic parent chain, so each folder is walked at most once.
        while (item is not null && visited.Add(item.Key))
        {
            path.Insert(0, item);
            item = item.Parent ?? tree.Find(item.Resource?.Parent);
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
            if (_itemManager.PendingAction is not ExplorerBatchAction<ResourceInfo> { IsActive: true } deletion)
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

    private ExplorerRenameAction<ResourceInfo>? PendingRename
        => _itemManager.PendingAction as ExplorerRenameAction<ResourceInfo>;

    private string RenameDialogTitle
        => PendingRename?.Target is FolderResourceInfo ? L["Rename Folder"].Value : L["Rename Media"].Value;

    private string RenameDialogFieldLabel
        => PendingRename?.Target is FolderResourceInfo ? L["Folder Name"].Value : L["New Name"].Value;

    private string FolderDeleteConfirmMessage
    {
        get
        {
            if (_itemManager.PendingAction is not ExplorerBatchAction<FolderResourceInfo> { IsActive: true } deletion)
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

    /// <summary>How many resources match the current search and sort, or null when the provider cannot say.</summary>
    private int? MatchingCount
        => IsProviderMode ? _providerWindow.Total : _collection.MatchingCount;

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

            return MatchingCount is { } total ? VisibleItems.Count < total : _providerWindow.WasFull;
        }
    }

    private bool IsInfiniteScrolling
        => Options.InfiniteScroll != ExplorerInfiniteScrollMode.Off;

    /// <summary>The items on screen; a provider supplies its own window, and a held collection is sliced into one.</summary>
    private IReadOnlyList<IExplorerItem> VisibleItems
        => IsProviderMode ? _providerWindow.Items : _collection.VisibleItems;

    private IReadOnlyList<ResourceInfo> SelectedResources
        => _selection.ResolveResources();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _collection = new ExplorerCollectionView(item => item.GetLocalizedLabels(L).KindLabel)
        {
            PageNumber = CurrentPage,
            PageSize = PageSize,
            SortField = SortField,
            SortDirection = SortDirection,
        };

        _viewMode = ViewMode;

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
        _itemManager.SelectedItems = _selection.ResolveItems;
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

        if (_acceptParameter.Update(Constraints.AcceptedResourceKinds))
        {
            _collection.Filter = ExplorerResourceFilter.Parse(Constraints.AcceptedResourceKinds);
        }

        if (_currentPageParameter.Update(CurrentPage))
        {
            _collection.PageNumber = CurrentPage;
        }

        if (_viewModeParameter.Update(ViewMode))
        {
            _viewMode = ViewMode;
        }

        if (_sortFieldParameter.Update(SortField))
        {
            _collection.SortField = SortField;
        }

        if (_sortDirectionParameter.Update(SortDirection))
        {
            _collection.SortDirection = SortDirection;
        }

        // Resync only when the host supplies a different list, so an unrelated parent render cannot discard the selection.
        if (_selectedParameter.Update(Selected))
        {
            AdoptSelection(Selected);
        }

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

        // The folder is the host's to know, so it is watched for change rather than sent, and a move invalidates the window.
        var moved = _queryParameters.Update(ItemsProvider, CurrentFolder?.Source, PageSize);

        if (!moved)
        {
            return;
        }

        _collection.PageNumber = 1;

        await QueryAsync(append: false);
    }

    /// <summary>Fetches a window from the provider, replacing what is shown or extending it while infinite scrolling.</summary>
    private Task QueryAsync(bool append)
    {
        if (ItemsProvider is not { } provider)
        {
            return Task.CompletedTask;
        }

        var query = new ExplorerItemQuery(
            append ? _providerWindow.Items.Count : (EffectivePage - 1) * PageSize,
            PageSize,
            string.IsNullOrWhiteSpace(_collection.SearchQuery) ? null : _collection.SearchQuery,
            _collection.SortField,
            _collection.SortDirection);

        // FetchAsync lives in a library that must ConfigureAwait(false), so its continuation is not
        // guaranteed to land back on the render thread; StateHasChanged needs InvokeAsync to get there.
        return _providerWindow.FetchAsync(provider, onStateChanged: () => InvokeAsync(StateHasChanged), query, _collection.Filter, appendToWindow: append);
    }

    /// <summary>Re-reads the current window, after something the explorer does not own has changed the collection.</summary>
    private Task RefreshProviderAsync()
    {
        if (!IsProviderMode)
        {
            return Task.CompletedTask;
        }

        // An append would stack a second copy of what is already shown, so a refresh always starts the window again.
        if (IsInfiniteScrolling)
        {
            _collection.PageNumber = 1;
        }

        return QueryAsync(append: false);
    }

    private int RootFolderCount
        => Folders?.Count(item => item is IExplorerFolderItem { Resource.Parent: null }) ?? 0;

    private int KnownResourceCount
        => TotalCount ?? Items?.Count ?? MatchingCount ?? 0;

    private long KnownByteCount
        => TotalBytes ?? Items?.Sum(item => item.Resource?.Size ?? 0) ?? 0;

    // A top-level folder counts as one, so the root itself is zero.
    private int DepthOf(FolderResourceInfo? folder)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var depth = 0;
        var current = folder;

        while (current is not null && visited.Add(current.Source))
        {
            depth++;
            current = FolderAt(current.Parent);
        }

        return depth;
    }

    // How many levels the folder's own subtree spans, so moving it can be measured before it lands.
    private int HeightOf(FolderResourceInfo folder)
    {
        var tallest = 0;

        foreach (var child in FolderTree.ChildrenOf(folder.Source))
        {
            if (child.Resource is { } childFolder)
            {
                tallest = Math.Max(tallest, HeightOf(childFolder));
            }
        }

        return tallest + 1;
    }

    private async Task RejectAsync(ExplorerRejectionReason reason, long? allowed, string message)
    {
        _rejectionMessage = message;

        await OnRejected.InvokeAsync(new ExplorerRejection(reason, allowed, message));
    }

    private async Task<bool> EnsureFolderHasRoomAsync(FolderResourceInfo? folder, int adding)
    {
        if (Constraints.MaxFolderResourceCount <= 0 || folder is null || (folder.Count ?? 0) + adding <= Constraints.MaxFolderResourceCount)
        {
            return true;
        }

        await RejectAsync(
            ExplorerRejectionReason.FolderResourceCount,
            Constraints.MaxFolderResourceCount,
            L["[$ plural; one: A folder holds at most # item.; other: A folder holds at most # items.; $]", Constraints.MaxFolderResourceCount].Value);

        return false;
    }

    private async Task<bool> EnsureExplorerHasRoomAsync(int adding, long bytes)
    {
        if (Constraints.MaxResourceCount > 0 && KnownResourceCount + adding > Constraints.MaxResourceCount)
        {
            await RejectAsync(
                ExplorerRejectionReason.ResourceCount,
                Constraints.MaxResourceCount,
                L["[$ plural; one: This explorer holds at most # item.; other: This explorer holds at most # items.; $]", Constraints.MaxResourceCount].Value);

            return false;
        }

        if (Constraints.MaxTotalBytes > 0 && KnownByteCount + bytes > Constraints.MaxTotalBytes)
        {
            await RejectAsync(
                ExplorerRejectionReason.TotalBytes,
                Constraints.MaxTotalBytes,
                L["This explorer holds at most {Size}.", ResourceFormatter.FromByteCount(Constraints.MaxTotalBytes)].Value);

            return false;
        }

        return true;
    }

    private async Task<bool> EnsureFolderFitsAsync(FolderResourceInfo? parent, int height)
    {
        if (Constraints.MaxFolderDepth > 0 && DepthOf(parent) + height > Constraints.MaxFolderDepth)
        {
            await RejectAsync(
                ExplorerRejectionReason.FolderDepth,
                Constraints.MaxFolderDepth,
                L["[$ plural; one: Folders cannot nest more than # level deep.; other: Folders cannot nest more than # levels deep.; $]", Constraints.MaxFolderDepth].Value);

            return false;
        }

        if (parent is null && Constraints.MaxFolderRootCount > 0 && RootFolderCount >= Constraints.MaxFolderRootCount)
        {
            await RejectAsync(
                ExplorerRejectionReason.FolderRootCount,
                Constraints.MaxFolderRootCount,
                L["[$ plural; one: This explorer holds at most # top-level folder.; other: This explorer holds at most # top-level folders.; $]", Constraints.MaxFolderRootCount].Value);

            return false;
        }

        if (parent is not null && Constraints.MaxSubfolderCount > 0 && FolderTree.ChildrenOf(parent.Source).Count >= Constraints.MaxSubfolderCount)
        {
            await RejectAsync(
                ExplorerRejectionReason.SubfolderCount,
                Constraints.MaxSubfolderCount,
                L["[$ plural; one: A folder holds at most # folder inside it.; other: A folder holds at most # folders inside it.; $]", Constraints.MaxSubfolderCount].Value);

            return false;
        }

        return true;
    }

    // Only the options decide which actions exist, so the list and its closures survive an unrelated render.
    private IReadOnlyList<ExplorerItemAction> Actions
    {
        get
        {
            if (!ReferenceEquals(_actionOptions, ItemOptions))
            {
                _actionOptions = ItemOptions;
                _actions = BuildActions();
            }

            return _actions;
        }
    }

    ExplorerOptions IExplorerConfigurationView.Options => Options;

    bool IExplorerConfigurationView.ShowsSidebar => ShowsSidebar;

    FolderResourceInfo? IExplorerLocationView.CurrentFolder => CurrentFolder;

    IReadOnlyList<IExplorerFolderItem> IExplorerLocationView.Breadcrumbs => Breadcrumbs;

    ExplorerFolderTree IExplorerLocationView.FolderTree => FolderTree;

    bool IExplorerLocationView.CanGoBack => CanGoBack;

    bool IExplorerLocationView.CanGoForward => CanGoForward;

    bool IExplorerLocationView.CanGoUp => CanGoUp;

    ExplorerSortField IExplorerContentView.SortField => _collection.SortField;

    ExplorerSortDirection IExplorerContentView.SortDirection => _collection.SortDirection;

    ExplorerViewMode IExplorerContentView.ViewMode => _viewMode;

    IExplorerSelectionView IExplorerView.Selection => _selection;

    IExplorerItemView IExplorerView.Item => _itemManager;

    IReadOnlyList<IExplorerItem> IExplorerContentView.VisibleItems => VisibleItems;

    bool IExplorerContentView.IsFetching => _providerWindow.IsFetching;

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

    Task IExplorerSelectionInteraction.ToggleSelectionAsync(IExplorerItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return ChangeSelectionAsync(selection => selection.Toggle(item));
    }

    Task IExplorerContentInteraction.SortByAsync(ExplorerSortField field)
        => RunAsync(() => HandleSortFieldChangedAsync(field));

    Task IExplorerContentInteraction.ChangeViewModeAsync(ExplorerViewMode mode)
        => RunAsync(() => HandleViewModeChangedAsync(mode));

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

    Task IExplorerLocationInteraction.SelectFolderAsync(FolderResourceInfo? folder)
        => RunAsync(() => HandleFolderSelectedAsync(folder));

    Task IExplorerContentInteraction.SetSortDirectionAsync(ExplorerSortDirection direction)
        => RunAsync(() => HandleSortDirectionChangedAsync(direction));

    Task IExplorerSelectionInteraction.ToggleSelectionModeAsync()
        => RunAsync(ToggleSelectModeAsync);

    Task IExplorerSelectionInteraction.ClearSelectionAsync()
        => RunAsync(ClearSelectionAsync);

    Task IExplorerContentInteraction.RefreshAsync()
        => RunAsync(HandleRefreshAsync);

    private List<ExplorerItemAction> BuildActions()
    {
        var actions = new List<ExplorerItemAction>
        {
            new()
            {
                Id = "preview",
                Icon = Glyph.Inspect,
                Label = () => L["Preview"].Value,
                Requires = ExplorerItemCapabilities.View,
                InvokeAsync = item =>
                {
                    if (item.Resource is { } resource)
                    {
                        OpenPreview(resource);
                        StateHasChanged();
                    }

                    return Task.CompletedTask;
                }
            }
        };

        if (ItemOptions.AllowedCapabilities.HasFlag(ExplorerItemCapabilities.Rename))
        {
            actions.Add(new ExplorerItemAction
            {
                Id = "rename",
                Icon = Glyph.Pen,
                Label = () => L["Rename"].Value,
                Requires = ExplorerItemCapabilities.Rename,
                InvokeAsync = item => { _itemManager.RequestRename(item); return Task.CompletedTask; }
            });
        }

        if (ItemOptions.AllowedCapabilities.HasFlag(ExplorerItemCapabilities.Delete))
        {
            actions.Add(new ExplorerItemAction
            {
                Id = "delete",
                Icon = Glyph.Delete,
                Label = () => L["Delete"].Value,
                Requires = ExplorerItemCapabilities.Delete,
                InvokeAsync = item => { _itemManager.RequestDelete(item); return Task.CompletedTask; },
                IsDestructive = true
            });
        }

        return actions;
    }

    /// <summary>Changes the selection and reports it, so no caller can do the one without the other.</summary>
    private async Task ChangeSelectionAsync(Action<ExplorerSelection> change)
    {
        change(_selection);

        await SelectedChanged.InvokeAsync(_selection.ResolveItems());

        StateHasChanged();
    }

    // Adopting what the host already supplied must not report it straight back at them.
    private void AdoptSelection(IReadOnlyList<IExplorerItem> selected)
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
        => string.Create(CultureInfo.InvariantCulture, $"--forma-explorer-sidebar-size: {_sidebarWidth}rem");

    private async Task HandleRefreshAsync()
    {
        await OnRefresh.InvokeAsync();
        await RefreshProviderAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _providerWindow.Dispose();
        _searchDebouncer.Dispose();
    }

    private async Task HandleBackAsync()
    {
        if (CanGoBack)
        {
            _history.GoBack();
            var folder = FolderAt(_history.Current);
            _collection.PageNumber = 1;

            await OnFolderChanged.InvokeAsync(folder);
        }
    }

    private Task ApplyCreateFolderAsync(FolderResourceInfo? parent, string name)
        => OnFolderCreate.InvokeAsync(new ExplorerFolderCreateRequest(parent, name));

    private async Task ApplyDeleteAsync(IReadOnlyList<ResourceInfo> targets)
    {
        await ChangeSelectionAsync(selection =>
        {
            foreach (var target in targets)
            {
                selection.Remove(ExplorerItem.For(target));
            }
        });

        await OnDelete.InvokeAsync(new ExplorerDeleteRequest(targets));
        await RefreshProviderAsync();
    }

    private async Task ApplyFolderDeleteAsync(IReadOnlyList<FolderResourceInfo> folders)
    {
        await ClearSelectionAsync();

        foreach (var folder in folders)
        {
            await OnFolderDelete.InvokeAsync(new ExplorerFolderDeleteRequest(folder));
        }
    }

    private async Task HandleFolderSelectedAsync(FolderResourceInfo? folder)
    {
        if (CurrentFolder?.Source == folder?.Source)
        {
            return;
        }

        _history.Visit(folder?.Source);
        _collection.PageNumber = 1;

        await OnFolderChanged.InvokeAsync(folder);
    }

    private async Task HandleForwardAsync()
    {
        if (CanGoForward)
        {
            _history.GoForward();
            var folder = FolderAt(_history.Current);
            _collection.PageNumber = 1;

            await OnFolderChanged.InvokeAsync(folder);
        }
    }

    private async Task HandleDropOnFolderAsync(ResourceInfo target, FolderResourceInfo? targetFolder)
    {
        if (target is FolderResourceInfo draggedFolder)
        {
            if (CanMoveFolder(draggedFolder, targetFolder)
                && await EnsureFolderFitsAsync(targetFolder, HeightOf(draggedFolder)))
            {
                await OnFolderDrop.InvokeAsync(new ExplorerFolderDropRequest(draggedFolder, targetFolder));
                await RefreshProviderAsync();
            }

            return;
        }

        var resources = _selection.IsSelected(ExplorerItem.For(target)) && SelectedResources.Count > 1
            ? SelectedResources
            : [target];

        if (!await EnsureFolderHasRoomAsync(targetFolder, resources.Count))
        {
            return;
        }

        await OnDrop.InvokeAsync(new ExplorerDropRequest(resources, targetFolder));

        // A move takes the resources out of the window they were dragged from, which only a re-read will show.
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

    private bool CanMoveFolder(FolderResourceInfo folder, FolderResourceInfo? target)
    {
        if (target is null)
        {
            return folder.Parent is not null;
        }

        // Moving a folder inside its own subtree would leave the tree cyclic and unrenderable.
        return folder.Source != target.Source && !IsSelfOrDescendant(target, folder);
    }

    private bool IsSelfOrDescendant(FolderResourceInfo candidate, FolderResourceInfo ancestor)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = candidate;

        while (current is not null && visited.Add(current.Source))
        {
            if (current.Source == ancestor.Source)
            {
                return true;
            }

            current = FolderAt(current.Parent);
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

    private async Task ApplyRenameAsync(ResourceInfo target, string newName)
    {
        if (target is FolderResourceInfo folder)
        {
            await OnFolderRename.InvokeAsync(new ExplorerFolderRenameRequest(folder, newName));
            return;
        }

        await OnRename.InvokeAsync(new ExplorerRenameRequest(target, newName));
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

        // A held collection filters as fast as it can render, but a provider would otherwise be asked once per keystroke.
        if (await _searchDebouncer.WaitAsync(Options.SearchDebounce))
        {
            await QueryAsync(append: false);
        }
    }

    private async Task HandleSortDirectionChangedAsync(ExplorerSortDirection direction)
    {
        _collection.SortDirection = direction;
        _collection.PageNumber = 1;

        await QueryAsync(append: false);
    }

    private async Task HandleSortFieldChangedAsync(ExplorerSortField field)
    {
        if (_collection.SortField == field)
        {
            _collection.SortDirection = _collection.SortDirection == ExplorerSortDirection.Ascending
                ? ExplorerSortDirection.Descending
                : ExplorerSortDirection.Ascending;
        }
        else
        {
            _collection.SortField = field;
            _collection.SortDirection = ExplorerSortDirection.Ascending;
        }

        _collection.PageNumber = 1;

        await QueryAsync(append: false);
    }

    private async Task HandleUpAsync()
    {
        if (CurrentFolder is not null)
        {
            var parent = CurrentFolder.Parent is { } parentSource
                ? FolderAt(parentSource)
                : null;

            await HandleFolderSelectedAsync(parent);
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
                ExplorerRejectionReason.ResourceKind,
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

    // Every caller names the parent it means, so no folder here is the root rather than wherever the reader happens to be.
    private async Task OpenFolderDialogAsync(FolderResourceInfo? parent)
    {
        _rejectionMessage = null;

        if (!await EnsureFolderFitsAsync(parent, height: 1))
        {
            return;
        }

        _itemManager.BeginCreateFolder(parent);
    }

    private Task ActivateItemAsync(IExplorerItem item)
    {
        if (_selection.IsSelectionModeActive)
        {
            return ChangeSelectionAsync(selection => selection.Toggle(item));
        }

        if (item.Resource is { } resource)
        {
            OpenPreview(resource);
            StateHasChanged();
        }

        return Task.CompletedTask;
    }

    private void OpenPreview(ResourceInfo resource)
    {
        // Captured once so the viewer is not handed a new list on every unrelated render.
        IReadOnlyList<string> siblings = [.. VisibleItems.Select(sibling => sibling.Resource?.Source).OfType<string>()];
        _preview.Open(resource, siblings);
    }
}
