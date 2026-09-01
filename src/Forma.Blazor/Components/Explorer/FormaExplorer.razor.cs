namespace Forma.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Interop.Behaviours;
using Forma.Features.Explorer;
using Forma.Features.Files;
using Forma.Framework.Icons;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A server-side media library manager with thumbnail/details/list views, folders, navigation, drag-and-drop organization, upload, preview, rename, and delete.</summary>
public sealed partial class FormaExplorer : ComponentBase, IAsyncDisposable, IExplorerView, IExplorerInteraction, IResourceKindLabels
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

    /// <summary>The media to display, held in full. Supply this or <see cref="ResourcesProvider"/>, not both.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo>? Resources { get; set; }

    /// <summary>Answers for one window of media at a time, for collections too large to hold. Supply this or <see cref="Resources"/>, not both.</summary>
    [Parameter] public IExplorerResourcesProvider? ResourcesProvider { get; set; }

    /// <summary>Available folders for grouping media.</summary>
    [Parameter] public IReadOnlyList<FolderResourceInfo>? Folders { get; set; }

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

    /// <summary>Which operations the explorer offers on the media and folders it lists.</summary>
    [Parameter] public ExplorerFileOptions FileOptions { get; set; } = ExplorerFileOptions.Default;

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

    /// <summary>The currently selected resources.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo> Selected { get; set; } = [];

    /// <summary>Raised when the selected resources change.</summary>
    [Parameter] public EventCallback<IReadOnlyList<ResourceInfo>> SelectedChanged { get; set; }

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

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private readonly ExplorerDeletion _deletion = new();

    private FolderResourceInfo? _draggedFolder;

    private ResourceInfo? _draggedResource;

    private readonly ExplorerHistory _history = new();

    private readonly FolderDraft _folderDraft = new();

    private bool _isUploadOpen;

    private IReadOnlyList<IBrowserFile>? _pickedUploadFiles;

    private readonly ParameterState<string?> _acceptParameter = new();

    private string? _rejectionMessage;

    private ResourceInfo? _previewResource;

    private IReadOnlyList<string> _previewSiblings = [];

    private readonly ExplorerRename _rename = new();

    private readonly ExplorerSelection _selection = new();

    private const double MinSidebarWidth = 10;

    private const double MaxSidebarWidth = 28;

    private double _sidebarWidth = 18;

    private bool _isResizingSidebar;

    private InfiniteScroll _infiniteScroll = default!;

    private ElementReference _sentinelElement;

    private ElementReference _scrollElement;

    private DotNetObjectReference<FormaExplorer>? _selfReference;

    private bool _isSentinelObserved;

    private bool _isSentinelStale;

    private bool _isLoadingPage;

    private readonly ParameterState<IReadOnlyList<ResourceInfo>?> _selectedParameter = new();

    private readonly ParameterState<ExplorerViewMode> _viewModeParameter = new();

    private readonly ParameterState<ExplorerSortField> _sortFieldParameter = new();

    private readonly ParameterState<ExplorerSortDirection> _sortDirectionParameter = new();

    private readonly ParameterState<int> _currentPageParameter = new();

    private ExplorerViewMode _viewMode = ExplorerViewMode.Thumbnails;

    private ExplorerCollectionView _collection = default!;

    private List<ResourceInfo> _providerWindow = [];

    private int? _providerTotal;

    // A store that cannot count still has to say whether the end has been reached, and a full window is the only signal left.
    private bool _lastWindowWasFull;

    private bool _isQuerying;

    private CancellationTokenSource? _queryCancellation;

    private CancellationTokenSource? _searchCancellation;

    private readonly ParameterState<IExplorerResourcesProvider?> _providerParameter = new();

    private readonly ParameterState<string?> _folderSourceParameter = new();

    private readonly ParameterState<int> _pageSizeParameter = new();

    private ExplorerFileOptions? _actionOptions;

    private IReadOnlyList<ExplorerAction> _actions = [];

    private ExplorerFolderTree? _folderTree;

    private IReadOnlyList<FolderResourceInfo>? _breadcrumbs;

    /// <summary>The folder list indexed, rebuilt once per parameter set because a host may mutate the list it passed.</summary>
    private ExplorerFolderTree FolderTree
        => _folderTree ??= new ExplorerFolderTree(Folders);

    private IReadOnlyList<FolderResourceInfo> Breadcrumbs
        => _breadcrumbs ??= CurrentFolder is null ? [] : BuildBreadcrumbs(FolderTree, CurrentFolder);

    private static List<FolderResourceInfo> BuildBreadcrumbs(ExplorerFolderTree tree, FolderResourceInfo? current)
    {
        var path = new List<FolderResourceInfo>();
        var visited = new HashSet<string>(StringComparer.Ordinal);

        // A host can supply a cyclic parent chain, so each folder is walked at most once.
        while (current is not null && visited.Add(current.Source))
        {
            path.Insert(0, current);
            current = tree.Find(current.Parent);
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
            if (_deletion.Resources is not { Count: > 0 })
            {
                return string.Empty;
            }

            if (_deletion.Resources.Count == 1)
            {
                var name = _deletion.Resources[0].Name;

                return L["Are you sure you want to delete \"{Name}\"?", name];
            }

            var count = _deletion.Resources.Count;

            return L["[$ plural; one: Are you sure you want to delete # item?; other: Are you sure you want to delete # items?; $]", count];
        }
    }

    private int EffectivePage
        => Math.Clamp(_collection.Page, 1, PageCount);

    private string FolderDeleteConfirmMessage
    {
        get
        {
            if (_deletion.Folders is not { Count: > 0 })
            {
                return string.Empty;
            }

            if (_deletion.Folders.Count == 1)
            {
                var name = _deletion.Folders[0].Name;

                return L["Are you sure you want to delete folder \"{Name}\"?", name];
            }

            var count = _deletion.Folders.Count;

            return L["[$ plural; one: Are you sure you want to delete # folder?; other: Are you sure you want to delete # folders?; $]", count];
        }
    }

    private bool IsProviderMode
        => ResourcesProvider is not null;

    /// <summary>How many resources match the current search and sort, or null when the provider cannot say.</summary>
    private int? MatchingCount
        => IsProviderMode ? _providerTotal : _collection.MatchingCount;

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

            return MatchingCount is { } total ? VisibleResources.Count < total : _lastWindowWasFull;
        }
    }

    private bool IsInfiniteScrolling
        => Options.InfiniteScroll != ExplorerInfiniteScrollMode.Off;

    /// <summary>The resources on screen; a provider supplies its own window, and a held collection is sliced into one.</summary>
    private IReadOnlyList<ResourceInfo> VisibleResources
        => IsProviderMode ? _providerWindow : _collection.Visible;

    private IReadOnlyList<ResourceInfo> SelectedResources
        => _selection.Resolve();

    private IReadOnlyList<FolderResourceInfo> SelectedFolders
        => _selection.ResolveFolders();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _infiniteScroll = new InfiniteScroll(JS);

        _collection = new ExplorerCollectionView(this)
        {
            Page = CurrentPage,
            PageSize = PageSize,
            SortField = SortField,
            SortDirection = SortDirection,
        };

        _viewMode = ViewMode;

        // Seeded once, so leaving selection mode afterwards is the reader's to decide.
        _selection.IsSelectMode = Options.SelectOnOpen;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Resources is null == ResourcesProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FormaExplorer)} requires exactly one of {nameof(Resources)} or {nameof(ResourcesProvider)}.");
        }

        _collection.Source = Resources ?? [];
        _collection.PageSize = PageSize;
        _collection.IsAppending = IsInfiniteScrolling;
        _collection.Invalidate();
        _folderTree = null;
        _breadcrumbs = null;

        if (_acceptParameter.Update(Constraints.AcceptedResourceKinds))
        {
            _collection.Accept = ExplorerResourceFilter.Parse(Constraints.AcceptedResourceKinds);
        }

        if (_currentPageParameter.Update(CurrentPage))
        {
            _collection.Page = CurrentPage;
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

        _selection.MaxSelection = Options.MaxSelection;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        if (!IsProviderMode)
        {
            return;
        }

        // The folder is the host's to know, so it is watched for change rather than sent, and a move invalidates the window.
        var moved = _providerParameter.Update(ResourcesProvider)
            | _folderSourceParameter.Update(CurrentFolder?.Source)
            | _pageSizeParameter.Update(PageSize);

        if (!moved)
        {
            return;
        }

        _collection.Page = 1;

        await QueryAsync(append: false);
    }

    /// <summary>Fetches a window from the provider, replacing what is shown or extending it while infinite scrolling.</summary>
    private async Task QueryAsync(bool append)
    {
        if (ResourcesProvider is not { } provider)
        {
            return;
        }

        // A newer window makes an older one irrelevant rather than merely late, so the earlier request is abandoned.
        await CancelAsync(_queryCancellation);
        _queryCancellation = new CancellationTokenSource();
        var token = _queryCancellation.Token;

        var query = new ExplorerResourceQuery(
            append ? _providerWindow.Count : (EffectivePage - 1) * PageSize,
            PageSize,
            string.IsNullOrWhiteSpace(_collection.SearchQuery) ? null : _collection.SearchQuery,
            _collection.SortField,
            _collection.SortDirection);

        _isQuerying = true;
        StateHasChanged();

        try
        {
            var page = await provider.GetResourcesAsync(query, token);

            if (token.IsCancellationRequested)
            {
                return;
            }

            // A provider is expected to apply the same filter itself; this only keeps a stray resource off the screen.
            var window = _collection.Accept is { } accepted
                ? page.Resources.Where(accepted.Accepts)
                : page.Resources;

            // The views hold the window they were handed, so a longer one replaces it rather than growing it underneath them.
            _providerWindow = append ? [.. _providerWindow, .. window] : [.. window];
            _providerTotal = page.TotalCount;
            _lastWindowWasFull = page.Resources.Count >= PageSize;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            // A superseded query leaves the flag to the one that replaced it; anything else, including a failure, clears it.
            if (!token.IsCancellationRequested)
            {
                _isQuerying = false;
            }
        }

        StateHasChanged();
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
            _collection.Page = 1;
        }

        return QueryAsync(append: false);
    }

    private int RootFolderCount
        => Folders?.Count(folder => folder.Parent is null) ?? 0;

    private int KnownResourceCount
        => TotalCount ?? Resources?.Count ?? MatchingCount ?? 0;

    private long KnownByteCount
        => TotalBytes ?? Resources?.Sum(resource => resource.Size ?? 0) ?? 0;

    // A top-level folder counts as one, so the root itself is zero.
    private int DepthOf(FolderResourceInfo? folder)
    {
        var tree = FolderTree;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var depth = 0;
        var current = folder;

        while (current is not null && visited.Add(current.Source))
        {
            depth++;
            current = tree.Find(current.Parent);
        }

        return depth;
    }

    // How many levels the folder's own subtree spans, so moving it can be measured before it lands.
    private int HeightOf(FolderResourceInfo folder)
    {
        var tallest = 0;

        foreach (var child in FolderTree.ChildrenOf(folder.Source))
        {
            tallest = Math.Max(tallest, HeightOf(child));
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

    private static async Task CancelAsync(CancellationTokenSource? source)
    {
        if (source is null)
        {
            return;
        }

        await source.CancelAsync();
        source.Dispose();
    }

    // Only the options decide which actions exist, so the list and its closures survive an unrelated render.
    private IReadOnlyList<ExplorerAction> Actions
    {
        get
        {
            if (!ReferenceEquals(_actionOptions, FileOptions))
            {
                _actionOptions = FileOptions;
                _actions = BuildActions();
            }

            return _actions;
        }
    }

    ExplorerOptions IExplorerView.Options => Options;

    ExplorerFolderTree IExplorerView.FolderTree => FolderTree;

    ExplorerSortField IExplorerView.SortField => _collection.SortField;

    ExplorerSortDirection IExplorerView.SortDirection => _collection.SortDirection;

    string IResourceKindLabels.LabelOf(ResourceInfo resource) => resource.KindLabel(L);

    IExplorerSelectionView IExplorerView.Selection => _selection;

    IReadOnlyList<ExplorerAction> IExplorerView.Actions => Actions;

    Task IExplorerInteraction.ActivateAsync(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (_selection.IsSelectMode)
        {
            return ChangeSelectionAsync(selection => selection.Toggle(resource));
        }

        OpenPreview(resource);
        StateHasChanged();

        return Task.CompletedTask;
    }

    Task IExplorerInteraction.ToggleSelectAsync(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return ChangeSelectionAsync(selection => selection.Toggle(resource));
    }

    Task IExplorerInteraction.ToggleFolderSelectAsync(FolderResourceInfo folder)
        => ToggleFolderSelectAsync(folder);

    Task IExplorerInteraction.SortByAsync(ExplorerSortField field)
        => HandleSortRequestedAsync(field);

    void IExplorerInteraction.BeginDrag(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        _draggedResource = resource;
    }

    // The sidebar re-exposes this to its folder nodes, which never reach past it to the explorer.
    internal Task ToggleFolderSelectAsync(FolderResourceInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return ChangeSelectionAsync(selection => selection.ToggleFolder(folder));
    }

    private List<ExplorerAction> BuildActions()
    {
        var actions = new List<ExplorerAction>
        {
            new()
            {
                Id = "preview",
                Icon = Glyph.Inspect,
                Text = () => L["Preview"].Value,
                Invoke = resource => { OpenPreview(resource); StateHasChanged(); return Task.CompletedTask; }
            }
        };

        if (FileOptions.AllowRename)
        {
            actions.Add(new ExplorerAction
            {
                Id = "rename",
                Icon = Glyph.Pen,
                Text = () => L["Rename"].Value,
                Invoke = resource => { OpenRenameDialog(resource); StateHasChanged(); return Task.CompletedTask; }
            });
        }

        if (FileOptions.AllowDelete)
        {
            actions.Add(new ExplorerAction
            {
                Id = "delete",
                Icon = Glyph.Delete,
                Text = () => L["Delete"].Value,
                Invoke = resource => { OpenDeleteDialog(resource); StateHasChanged(); return Task.CompletedTask; },
                IsDestructive = true
            });
        }

        return actions;
    }

    /// <summary>Changes the selection and reports it, so no caller can do the one without the other.</summary>
    private async Task ChangeSelectionAsync(Action<ExplorerSelection> change)
    {
        change(_selection);

        await SelectedChanged.InvokeAsync(SelectedResources);

        StateHasChanged();
    }

    // Adopting what the host already supplied must not report it straight back at them.
    private void AdoptSelection(IReadOnlyList<ResourceInfo> selected)
        => _selection.Replace(selected);

    private async Task HandleSortRequestedAsync(ExplorerSortField field)
    {
        await HandleSortFieldChangedAsync(field);

        StateHasChanged();
    }

    private async Task ToggleSelectModeAsync()
    {
        _selection.IsSelectMode = !_selection.IsSelectMode;

        if (!_selection.IsSelectMode)
        {
            await ClearSelectionAsync();
        }
    }

    private Task ClearSelectionAsync()
        => ChangeSelectionAsync(selection => selection.Clear());

    private string SidebarSizeStyle
        => string.Create(CultureInfo.InvariantCulture, $"--forma-explorer-sidebar-size: {_sidebarWidth}rem");

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
        => await SyncSentinelAsync();

    private async Task SyncSentinelAsync()
    {
        // Only automatic scrolling watches the sentinel; the manual mode waits for the button to be pressed.
        var wantsSentinel = Options.InfiniteScroll == ExplorerInfiniteScrollMode.Automatic && HasMorePages;

        if (wantsSentinel == _isSentinelObserved && !_isSentinelStale)
        {
            return;
        }

        if (wantsSentinel)
        {
            _selfReference ??= DotNetObjectReference.Create(this);

            _isSentinelObserved = await _infiniteScroll.AttachAsync(_sentinelElement, _selfReference, _scrollElement);
        }
        else
        {
            await _infiniteScroll.DetachAsync();
            _isSentinelObserved = false;
        }

        _isSentinelStale = false;
    }

    /// <summary>Appends the next page of media, as the reader scrolls toward the end of the current one.</summary>
    [JSInvokable]
    public async Task HandleReachedAsync()
    {
        if (_isLoadingPage || !IsInfiniteScrolling || !HasMorePages)
        {
            return;
        }

        _collection.Page = EffectivePage + 1;

        // An observer reports crossings, not a standing state, so it is re-armed to keep filling a viewport the new page did not.
        _isSentinelStale = true;
        _isLoadingPage = true;

        StateHasChanged();

        try
        {
            await QueryAsync(append: true);
            await OnPageChanged.InvokeAsync(_collection.Page);
        }
        finally
        {
            _isLoadingPage = false;
        }

        StateHasChanged();
    }

    private async Task HandleRefreshAsync()
    {
        await OnRefresh.InvokeAsync();
        await RefreshProviderAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _infiniteScroll.DisposeAsync();

        _selfReference?.Dispose();
        _queryCancellation?.Dispose();
        _searchCancellation?.Dispose();
    }

    private void CloseDeleteDialog()
        => _deletion.Clear();

    private void CloseFolderDeleteDialog()
        => _deletion.Clear();

    private void CloseFolderDialog()
        => _folderDraft.Close();

    private void CloseFolderRenameDialog()
        => _rename.Close();

    private void ClosePreview()
        => _previewResource = null;

    private void CloseRenameDialog()
        => _rename.Close();

    private void CloseUpload()
    {
        _isUploadOpen = false;
        _pickedUploadFiles = null;
    }

    private async Task HandleBackAsync()
    {
        if (CanGoBack)
        {
            _history.GoBack();
            var folder = Folders?.FirstOrDefault(f => f.Source == _history.Current);
            _collection.Page = 1;

            await OnFolderChanged.InvokeAsync(folder);
        }
    }

    private async Task HandleCreateFolderAsync()
    {
        if (!_folderDraft.CanCreate)
        {
            return;
        }

        var request = new ExplorerFolderCreateRequest(_folderDraft.Parent, _folderDraft.ConfirmedName);
        _folderDraft.Close();

        await OnFolderCreate.InvokeAsync(request);
    }
    private async Task HandleDeleteConfirmedAsync()
    {
        if (_deletion.Resources is { Count: > 0 } targets)
        {
            _deletion.Clear();

            await ChangeSelectionAsync(selection =>
            {
                foreach (var target in targets)
                {
                    selection.Remove(target);
                }
            });

            await OnDelete.InvokeAsync(new ExplorerDeleteRequest(targets));
            await RefreshProviderAsync();
        }
    }

    private async Task HandleFolderDeleteConfirmedAsync()
    {
        if (_deletion.Folders is { Count: > 0 } folders)
        {
            _deletion.Clear();

            await ClearSelectionAsync();

            foreach (var folder in folders)
            {
                await OnFolderDelete.InvokeAsync(new ExplorerFolderDeleteRequest(folder));
            }
        }
    }

    private async Task HandleFolderRenameConfirmedAsync()
    {
        if (_rename.Folder is { } folder && _rename.CanConfirm)
        {
            var newName = _rename.ConfirmedName;
            _rename.Close();

            await OnFolderRename.InvokeAsync(new ExplorerFolderRenameRequest(folder, newName));
        }
    }

    private async Task HandleFolderSelectedAsync(FolderResourceInfo? folder)
    {
        if (CurrentFolder?.Source == folder?.Source)
        {
            return;
        }

        _history.Visit(folder?.Source);
        _collection.Page = 1;

        await OnFolderChanged.InvokeAsync(folder);
    }

    private async Task HandleForwardAsync()
    {
        if (CanGoForward)
        {
            _history.GoForward();
            var folder = Folders?.FirstOrDefault(f => f.Source == _history.Current);
            _collection.Page = 1;

            await OnFolderChanged.InvokeAsync(folder);
        }
    }

    private async Task HandleDropOnFolderAsync(FolderResourceInfo? targetFolder)
    {
        // The drag ends whatever the drop lands on, so a rejected target cannot leave a stale resource to move later.
        var draggedFolder = _draggedFolder;
        var draggedResource = _draggedResource;
        _draggedFolder = null;
        _draggedResource = null;

        if (draggedFolder is not null)
        {
            if (CanMoveFolder(draggedFolder, targetFolder)
                && await EnsureFolderFitsAsync(targetFolder, HeightOf(draggedFolder)))
            {
                await OnFolderDrop.InvokeAsync(new ExplorerFolderDropRequest(draggedFolder, targetFolder));
                await RefreshProviderAsync();
            }

            return;
        }

        if (draggedResource is not null)
        {
            var resources = _selection.IsSelected(draggedResource) && SelectedResources.Count > 1
                ? SelectedResources
                : [draggedResource];

            if (!await EnsureFolderHasRoomAsync(targetFolder, resources.Count))
            {
                return;
            }

            await OnDrop.InvokeAsync(new ExplorerDropRequest(resources, targetFolder));

            // A move takes the resources out of the window they were dragged from, which only a re-read will show.
            await ClearSelectionAsync();
            await RefreshProviderAsync();
        }
    }

    private async Task HandlePageChangedAsync(int page)
    {
        _collection.Page = page;

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
        var tree = FolderTree;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = candidate;

        while (current is not null && visited.Add(current.Source))
        {
            if (current.Source == ancestor.Source)
            {
                return true;
            }

            current = tree.Find(current.Parent);
        }

        return false;
    }

    private void HandlePreviewNavigate(string source)
    {
        var next = VisibleResources.FirstOrDefault(resource => resource.Source == source);

        if (next is not null)
        {
            _previewResource = next;
        }
    }

    private async Task HandleRenameConfirmedAsync()
    {
        if (_rename.Resource is not { } resource || !_rename.CanConfirm)
        {
            return;
        }

        var newName = _rename.ConfirmedName;
        _rename.Close();

        await OnRename.InvokeAsync(new ExplorerRenameRequest(resource, newName));
        await RefreshProviderAsync();
    }

    private async Task HandleSearchQueryChangedAsync(string query)
    {
        _collection.SearchQuery = query;
        _collection.Page = 1;

        if (!IsProviderMode)
        {
            return;
        }

        // A held collection filters as fast as it can render, but a provider would otherwise be asked once per keystroke.
        await CancelAsync(_searchCancellation);
        _searchCancellation = new CancellationTokenSource();

        try
        {
            await Task.Delay(Options.SearchDebounce, _searchCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await QueryAsync(append: false);
    }

    private async Task HandleSortDirectionChangedAsync(ExplorerSortDirection direction)
    {
        _collection.SortDirection = direction;
        _collection.Page = 1;

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

        _collection.Page = 1;

        await QueryAsync(append: false);
    }

    private async Task HandleUpAsync()
    {
        if (CurrentFolder is not null)
        {
            var parent = CurrentFolder.Parent is { } parentSource
                ? Folders?.FirstOrDefault(f => f.Source == parentSource)
                : null;

            await HandleFolderSelectedAsync(parent);
        }
    }

    private async Task HandleUploadCompletedAsync(IReadOnlyList<ResourceInfo> uploaded)
    {
        _pickedUploadFiles = null;
        _isUploadOpen = false;

        await OnUploaded.InvokeAsync(uploaded);
        await RefreshProviderAsync();
    }

    private async Task HandleUploadFilesPickedAsync(IReadOnlyList<IBrowserFile> files)
    {
        _rejectionMessage = null;

        if (_collection.Accept is { } filter
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

        _pickedUploadFiles = files;
    }

    private async Task HandleViewModeChangedAsync(ExplorerViewMode mode)
    {
        _viewMode = mode;

        await ViewModeChanged.InvokeAsync(mode);
    }

    private void OpenDeleteDialog(ResourceInfo? single = null)
    {
        if (single is not null)
        {
            _deletion.Request([single]);
        }
        else if (_selection.HasFolders)
        {
            _deletion.Request(SelectedFolders);
        }
        else if (_selection.HasResources)
        {
            _deletion.Request(SelectedResources);
        }
    }

    private void OpenFolderDeleteDialog(FolderResourceInfo folder)
        => _deletion.Request([folder]);

    // Every caller names the parent it means, so no folder here is the root rather than wherever the reader happens to be.
    private async Task OpenFolderDialogAsync(FolderResourceInfo? parent)
    {
        _rejectionMessage = null;

        if (!await EnsureFolderFitsAsync(parent, height: 1))
        {
            return;
        }

        _folderDraft.Open(parent);
    }

    private void OpenFolderRenameDialog(FolderResourceInfo folder)
        => _rename.Begin(folder);

    private void OpenPreview(ResourceInfo resource)
    {
        // Captured once so the viewer is not handed a new list on every unrelated render.
        _previewSiblings = [.. VisibleResources.Select(sibling => sibling.Source)];
        _previewResource = resource;
    }

    private void OpenRenameDialog(ResourceInfo resource)
        => _rename.Begin(resource);

    private void ToggleUpload()
        => _isUploadOpen = !_isUploadOpen;
}
