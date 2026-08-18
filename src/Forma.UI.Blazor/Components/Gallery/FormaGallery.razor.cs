namespace Forma.UI.Blazor.Components.Gallery;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Blazor.Components.Feedback;
using Forma.UI.Style.Glyphs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A server-side media library manager with thumbnail/details/list views, folders, navigation, drag-and-drop organization, upload, preview, rename, and delete.</summary>
public sealed partial class FormaGallery : ComponentBase, IAsyncDisposable
{
    private const int DefaultPageSize = 24;

    // Splitter deltas arrive in pixels and the sidebar is sized in rem, so they are converted at the browser default.
    private const double RootFontSize = 16;

    private static readonly FormaModalOptions DialogModalOptions = new()
    {
        ShowCancelAction = false,
        ShowConfirmAction = false
    };

    /// <summary>The currently active folder, or null for all media.</summary>
    [Parameter] public FolderResourceInfo? CurrentFolder { get; set; }

    /// <summary>The active page, 1-based.</summary>
    [Parameter] public int CurrentPage { get; set; } = 1;

    /// <summary>The media to display, held in full. Supply this or <see cref="ResourcesProvider"/>, not both.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo>? Resources { get; set; }

    /// <summary>Answers for one window of media at a time, for collections too large to hold. Supply this or <see cref="Resources"/>, not both.</summary>
    [Parameter] public IGalleryResourcesProvider? ResourcesProvider { get; set; }

    /// <summary>Available folders for grouping media.</summary>
    [Parameter] public IReadOnlyList<FolderResourceInfo>? Folders { get; set; }

    private bool ShowsSidebar
        => Options.ShowSidebar && Folders is { Count: > 0 };

    /// <summary>How many resources the whole library holds, for hosts that supply one folder at a time; the supplied resources are counted when null.</summary>
    [Parameter] public int? TotalCount { get; set; }

    /// <summary>How many bytes the whole library holds, for hosts that supply one folder at a time; the supplied resources are measured when null.</summary>
    [Parameter] public long? TotalBytes { get; set; }

    /// <summary>The ceilings the gallery keeps, refusing anything that would cross one.</summary>
    [Parameter] public FormaGalleryConstraints Constraints { get; set; } = FormaGalleryConstraints.None;

    /// <summary>Raised when a ceiling stopped an operation, with the reason already written for a reader.</summary>
    [Parameter] public EventCallback<GalleryRejection> OnRejected { get; set; }

    /// <summary>The maximum number of resources that may be selected at once (0 for unlimited).</summary>
    [Parameter] public int MaxSelection { get; set; }

    /// <summary>How the gallery presents itself and moves through the collection.</summary>
    [Parameter] public FormaGalleryOptions Options { get; set; } = FormaGalleryOptions.Default;

    /// <summary>Which operations the gallery offers on the media and folders it lists.</summary>
    [Parameter] public FormaGalleryFileOptions FileOptions { get; set; } = FormaGalleryFileOptions.Default;

    /// <summary>Raised when media resources are deleted.</summary>
    [Parameter] public EventCallback<GalleryDeleteRequest> OnDelete { get; set; }

    /// <summary>Raised when media resources are dropped onto a target folder.</summary>
    [Parameter] public EventCallback<GalleryDropRequest> OnDrop { get; set; }

    /// <summary>Raised when the active folder changes.</summary>
    [Parameter] public EventCallback<FolderResourceInfo?> OnFolderChanged { get; set; }

    /// <summary>Raised when a request is made to create a new folder.</summary>
    [Parameter] public EventCallback<GalleryFolderCreateRequest> OnFolderCreate { get; set; }

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<GalleryFolderDeleteRequest> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder is dropped onto another folder.</summary>
    [Parameter] public EventCallback<GalleryFolderDropRequest> OnFolderDrop { get; set; }

    /// <summary>Raised when a folder rename is confirmed.</summary>
    [Parameter] public EventCallback<GalleryFolderRenameRequest> OnFolderRename { get; set; }

    /// <summary>Raised when the page changes.</summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>Raised when the refresh action is clicked.</summary>
    [Parameter] public EventCallback OnRefresh { get; set; }

    /// <summary>Raised when a resource rename is confirmed.</summary>
    [Parameter] public EventCallback<GalleryRenameRequest> OnRename { get; set; }

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
    [Parameter] public GallerySortDirection SortDirection { get; set; } = GallerySortDirection.Ascending;

    /// <summary>Active sort field.</summary>
    [Parameter] public GallerySortField SortField { get; set; } = GallerySortField.Name;

    /// <summary>File store used to persist uploads.</summary>
    [Parameter] public IFileStore? Store { get; set; }

    /// <summary>Active view presentation mode.</summary>
    [Parameter] public GalleryViewMode ViewMode { get; set; } = GalleryViewMode.Thumbnails;

    /// <summary>Raised when the view mode changes.</summary>
    [Parameter] public EventCallback<GalleryViewMode> ViewModeChanged { get; set; }

    [Inject] private IStringLocale<FormaGallery> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private List<FolderResourceInfo>? _deletingFolders;

    private List<ResourceInfo>? _deletingResources;

    private FolderResourceInfo? _draggedFolder;

    private ResourceInfo? _draggedResource;

    private readonly List<string?> _history = [null];

    private int _historyIndex;

    private bool _isNewFolderOpen;

    private bool _isUploadOpen;

    private string _newFolderName = string.Empty;

    private FolderResourceInfo? _newFolderParent;

    private int _page = 1;

    private IReadOnlyList<IBrowserFile>? _pickedUploadFiles;

    private GalleryResourceFilter? _acceptFilter;

    private string? _syncedAccept;

    private string? _rejectionMessage;

    private ResourceInfo? _previewResource;

    private IReadOnlyList<string> _previewSiblings = [];

    private string _renameFolderName = string.Empty;

    private string _renameNewName = string.Empty;

    private FolderResourceInfo? _renamingFolder;

    private ResourceInfo? _renamingResource;

    private string _searchQuery = string.Empty;

    private GalleryContext _context = default!;

    private const double MinSidebarWidth = 10;

    private const double MaxSidebarWidth = 28;

    private double _sidebarWidth = 14;

    private double _splitterOrigin;

    private double _splitterStartWidth;

    private bool _isResizingSidebar;

    private ElementReference _splitterElement;

    private IJSObjectReference? _splitterModule;

    private readonly string _sentinelId = $"forma-gallery-sentinel-{Guid.NewGuid():N}";

    private readonly string _scrollId = $"forma-gallery-scroll-{Guid.NewGuid():N}";

    private DotNetObjectReference<FormaGallery>? _selfReference;

    private bool _isSentinelObserved;

    private bool _isSentinelStale;

    private bool _isLoadingPage;

    private IReadOnlyList<ResourceInfo>? _syncedSelected;

    private GalleryViewMode _syncedViewMode;

    private GallerySortField _syncedSortField;

    private GallerySortDirection _syncedSortDirection;

    private int _syncedCurrentPage;

    private GallerySortDirection _sortDirection = GallerySortDirection.Ascending;

    private GallerySortField _sortField = GallerySortField.Name;

    private GalleryViewMode _viewMode = GalleryViewMode.Thumbnails;

    // Several members read the ordered list while rendering, so it is built once and dropped whenever its inputs move.
    private List<ResourceInfo>? _filteredAndSorted;

    private List<ResourceInfo>? _pagedResources;

    private List<ResourceInfo>? _pagedSource;

    private int _pagedSkip;

    private int _pagedTake;

    private List<ResourceInfo> _providerWindow = [];

    private int? _providerTotal;

    // A store that cannot count still has to say whether the end has been reached, and a full window is the only signal left.
    private bool _lastWindowWasFull;

    private bool _isQuerying;

    private CancellationTokenSource? _queryCancellation;

    private CancellationTokenSource? _searchCancellation;

    private IGalleryResourcesProvider? _syncedProvider;

    private string? _syncedFolderSource;

    private int _syncedPageSize;

    private FormaGalleryFileOptions? _actionOptions;

    private GalleryFolderTree? _folderTree;

    private IReadOnlyList<FolderResourceInfo>? _breadcrumbs;

    /// <summary>The folder list indexed, rebuilt once per parameter set because a host may mutate the list it passed.</summary>
    private GalleryFolderTree FolderTree
        => _folderTree ??= new GalleryFolderTree(Folders);

    private IReadOnlyList<FolderResourceInfo> Breadcrumbs
        => _breadcrumbs ??= CurrentFolder is null ? [] : BuildBreadcrumbs(FolderTree, CurrentFolder);

    private static List<FolderResourceInfo> BuildBreadcrumbs(GalleryFolderTree tree, FolderResourceInfo? current)
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

    private bool CanGoBack => _historyIndex > 0;

    private bool CanGoForward => _historyIndex < _history.Count - 1;

    private bool CanGoUp => CurrentFolder is not null;

    private string DeleteConfirmMessage
    {
        get
        {
            if (_deletingResources is not { Count: > 0 })
            {
                return string.Empty;
            }

            if (_deletingResources.Count == 1)
            {
                var name = _deletingResources[0].Name;

                return L[$"Are you sure you want to delete \"{name}\"?"];
            }

            var count = _deletingResources.Count;

            return L["[$ plural; one: Are you sure you want to delete # item?; other: Are you sure you want to delete # items?; $]", count];
        }
    }

    private int EffectivePage
        => Math.Clamp(_page, 1, PageCount);

    private string FolderDeleteConfirmMessage
    {
        get
        {
            if (_deletingFolders is not { Count: > 0 })
            {
                return string.Empty;
            }

            if (_deletingFolders.Count == 1)
            {
                var name = _deletingFolders[0].Name;

                return L[$"Are you sure you want to delete folder \"{name}\"?"];
            }

            var count = _deletingFolders.Count;

            return L["[$ plural; one: Are you sure you want to delete # folder?; other: Are you sure you want to delete # folders?; $]", count];
        }
    }

    private List<ResourceInfo> FilteredAndSortedResources
    {
        get
        {
            if (_filteredAndSorted is not null)
            {
                return _filteredAndSorted;
            }

            IEnumerable<ResourceInfo> query = Resources ?? [];

            if (_acceptFilter is { } accepted)
            {
                query = query.Where(accepted.Accepts);
            }

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                query = query.Where(resource => resource.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            query = (_sortField, _sortDirection) switch
            {
                (GallerySortField.Name, GallerySortDirection.Ascending) => query.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (GallerySortField.Name, GallerySortDirection.Descending) => query.OrderByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (GallerySortField.Size, GallerySortDirection.Ascending) => query.OrderBy(e => e.Size ?? 0),
                (GallerySortField.Size, GallerySortDirection.Descending) => query.OrderByDescending(e => e.Size ?? 0),
                (GallerySortField.Kind, GallerySortDirection.Ascending) => query.OrderBy(e => e.KindLabel(L), StringComparer.CurrentCulture),
                (GallerySortField.Kind, GallerySortDirection.Descending) => query.OrderByDescending(e => e.KindLabel(L), StringComparer.CurrentCulture),
                (GallerySortField.DateAdded, GallerySortDirection.Ascending) => query.OrderBy(e => e.CreatedAt ?? DateTimeOffset.MinValue),
                (GallerySortField.DateAdded, GallerySortDirection.Descending) => query.OrderByDescending(e => e.CreatedAt ?? DateTimeOffset.MinValue),
                (GallerySortField.DateModified, GallerySortDirection.Ascending) => query.OrderBy(e => e.ModifiedAt ?? DateTimeOffset.MinValue),
                (GallerySortField.DateModified, GallerySortDirection.Descending) => query.OrderByDescending(e => e.ModifiedAt ?? DateTimeOffset.MinValue),
                _ => query
            };

            _filteredAndSorted = [.. query];

            return _filteredAndSorted;
        }
    }

    private bool IsProviderMode
        => ResourcesProvider is not null;

    /// <summary>How many resources match the current search and sort, or null when the provider cannot say.</summary>
    private int? MatchingCount
        => IsProviderMode ? _providerTotal : FilteredAndSortedResources.Count;

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
        => Options.InfiniteScroll != GalleryInfiniteScrollMode.Off;

    /// <summary>The resources on screen; a provider supplies its own window, and a held collection is sliced into one.</summary>
    private List<ResourceInfo> VisibleResources
    {
        get
        {
            if (IsProviderMode)
            {
                return _providerWindow;
            }

            var source = FilteredAndSortedResources;
            var take = IsInfiniteScrolling ? EffectivePage * PageSize : PageSize;
            var skip = IsInfiniteScrolling ? 0 : (EffectivePage - 1) * PageSize;

            // Infinite scrolling grows this window to thousands, and rendering reads it several times, so it is rebuilt only when it moves.
            if (_pagedResources is not null && ReferenceEquals(_pagedSource, source) && _pagedSkip == skip && _pagedTake == take)
            {
                return _pagedResources;
            }

            _pagedSource = source;
            _pagedSkip = skip;
            _pagedTake = take;
            _pagedResources = [.. source.Skip(skip).Take(take)];

            return _pagedResources;
        }
    }

    private IReadOnlyList<ResourceInfo> SelectedResources
        => _context.Selection.Resolve();

    private IReadOnlyList<FolderResourceInfo> SelectedFolders
        => _context.Selection.ResolveFolders();

    protected override void OnInitialized()
    {
        _context = new GalleryContext(
            resource => { OpenPreview(resource); StateHasChanged(); return Task.CompletedTask; },
            NotifySelectionChangedAsync,
            HandleSortRequestedAsync,
            resource => _draggedResource = resource);

        _context.SelectLabel = resource => _context.Selection.IsSelected(resource)
            ? L["Deselect {name}", ("name", resource.Name)].Value
            : L["Select {name}", ("name", resource.Name)].Value;
        _context.ActivateLabel = resource => _context.Selection.IsSelectMode
            ? _context.SelectLabel(resource)
            : L["Preview {name}", ("name", resource.Name)].Value;

        _context.ActionsLabel = resource => L["Actions for {name}", ("name", resource.Name)].Value;

        _page = CurrentPage;
        _viewMode = ViewMode;
        _sortField = SortField;
        _sortDirection = SortDirection;

        _syncedCurrentPage = CurrentPage;
        _syncedViewMode = ViewMode;
        _syncedSortField = SortField;
        _syncedSortDirection = SortDirection;

        SyncContext();
    }

    protected override void OnParametersSet()
    {
        if (Resources is null == ResourcesProvider is null)
        {
            throw new InvalidOperationException(
                $"{nameof(FormaGallery)} requires exactly one of {nameof(Resources)} or {nameof(ResourcesProvider)}.");
        }

        _filteredAndSorted = null;
        _folderTree = null;
        _breadcrumbs = null;

        if (!string.Equals(_syncedAccept, Constraints.AcceptedResourceKinds, StringComparison.Ordinal))
        {
            _syncedAccept = Constraints.AcceptedResourceKinds;
            _acceptFilter = GalleryResourceFilter.Parse(Constraints.AcceptedResourceKinds);
        }

        Adopt(CurrentPage, ref _syncedCurrentPage, ref _page);
        Adopt(ViewMode, ref _syncedViewMode, ref _viewMode);
        Adopt(SortField, ref _syncedSortField, ref _sortField);
        Adopt(SortDirection, ref _syncedSortDirection, ref _sortDirection);

        // Resync only when the host supplies a different list, so an unrelated parent render cannot discard the selection.
        if (!ReferenceEquals(_syncedSelected, Selected))
        {
            _syncedSelected = Selected;
            _context.Selection.Replace(Selected);
        }

        SyncContext();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (!IsProviderMode)
        {
            return;
        }

        // The folder is the host's to know, so it is watched for change rather than sent, and a move invalidates the window.
        var moved = !ReferenceEquals(_syncedProvider, ResourcesProvider)
            || _syncedFolderSource != CurrentFolder?.Source
            || _syncedPageSize != PageSize;

        if (!moved)
        {
            return;
        }

        _syncedProvider = ResourcesProvider;
        _syncedFolderSource = CurrentFolder?.Source;
        _syncedPageSize = PageSize;
        _page = 1;

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

        var query = new GalleryResourceQuery(
            append ? _providerWindow.Count : (EffectivePage - 1) * PageSize,
            PageSize,
            string.IsNullOrWhiteSpace(_searchQuery) ? null : _searchQuery,
            _sortField,
            _sortDirection);

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
            var window = _acceptFilter is { } accepted
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

    /// <summary>Re-reads the current window, after something the gallery does not own has changed the collection.</summary>
    private Task RefreshProviderAsync()
    {
        if (!IsProviderMode)
        {
            return Task.CompletedTask;
        }

        // An append would stack a second copy of what is already shown, so a refresh always starts the window again.
        _page = IsInfiniteScrolling ? 1 : _page;

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

    private async Task RejectAsync(GalleryRejectionReason reason, long? allowed, string message)
    {
        _rejectionMessage = message;

        await OnRejected.InvokeAsync(new GalleryRejection(reason, allowed, message));
    }

    private async Task<bool> EnsureFolderHasRoomAsync(FolderResourceInfo? folder, int adding)
    {
        if (Constraints.MaxFolderResourceCount <= 0 || folder is null || (folder.Count ?? 0) + adding <= Constraints.MaxFolderResourceCount)
        {
            return true;
        }

        await RejectAsync(
            GalleryRejectionReason.FolderResourceCount,
            Constraints.MaxFolderResourceCount,
            L["[$ plural; one: A folder holds at most # item.; other: A folder holds at most # items.; $]", Constraints.MaxFolderResourceCount].Value);

        return false;
    }

    private async Task<bool> EnsureGalleryHasRoomAsync(int adding, long bytes)
    {
        if (Constraints.MaxResourceCount > 0 && KnownResourceCount + adding > Constraints.MaxResourceCount)
        {
            await RejectAsync(
                GalleryRejectionReason.ResourceCount,
                Constraints.MaxResourceCount,
                L["[$ plural; one: This gallery holds at most # item.; other: This gallery holds at most # items.; $]", Constraints.MaxResourceCount].Value);

            return false;
        }

        if (Constraints.MaxTotalBytes > 0 && KnownByteCount + bytes > Constraints.MaxTotalBytes)
        {
            await RejectAsync(
                GalleryRejectionReason.TotalBytes,
                Constraints.MaxTotalBytes,
                L["This gallery holds at most {size}.", ("size", ResourceFormatter.FromByteCount(Constraints.MaxTotalBytes))].Value);

            return false;
        }

        return true;
    }

    private async Task<bool> EnsureFolderFitsAsync(FolderResourceInfo? parent, int height)
    {
        if (Constraints.MaxFolderDepth > 0 && DepthOf(parent) + height > Constraints.MaxFolderDepth)
        {
            await RejectAsync(
                GalleryRejectionReason.FolderDepth,
                Constraints.MaxFolderDepth,
                L["[$ plural; one: Folders cannot nest more than # level deep.; other: Folders cannot nest more than # levels deep.; $]", Constraints.MaxFolderDepth].Value);

            return false;
        }

        if (parent is null && Constraints.MaxFolderRootCount > 0 && RootFolderCount >= Constraints.MaxFolderRootCount)
        {
            await RejectAsync(
                GalleryRejectionReason.FolderRootCount,
                Constraints.MaxFolderRootCount,
                L["[$ plural; one: This gallery holds at most # top-level folder.; other: This gallery holds at most # top-level folders.; $]", Constraints.MaxFolderRootCount].Value);

            return false;
        }

        if (parent is not null && Constraints.MaxSubfolderCount > 0 && FolderTree.ChildrenOf(parent.Source).Count >= Constraints.MaxSubfolderCount)
        {
            await RejectAsync(
                GalleryRejectionReason.SubfolderCount,
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

    // A parameter is taken only when the host changes it, so an unrelated parent render leaves what the user chose alone.
    private static void Adopt<T>(T parameter, ref T synced, ref T local)
        where T : struct
    {
        if (!EqualityComparer<T>.Default.Equals(parameter, synced))
        {
            synced = parameter;
            local = parameter;
        }
    }

    private void SyncContext()
    {
        _context.Options = Options;
        _context.FileOptions = FileOptions;
        _context.Folders = FolderTree;
        _context.Selection.MaxSelection = MaxSelection;
        _context.SortField = _sortField;
        _context.SortDirection = _sortDirection;

        // Only the options decide which actions exist, so the list and its closures survive an unrelated render.
        if (!ReferenceEquals(_actionOptions, FileOptions))
        {
            _actionOptions = FileOptions;
            _context.Actions = BuildActions();
        }
    }

    private List<GalleryAction> BuildActions()
    {
        var actions = new List<GalleryAction>
        {
            new()
            {
                Id = "preview",
                Icon = Glyph.Eye,
                Label = resource => L["Preview {name}", ("name", resource.Name)].Value,
                Invoke = resource => { OpenPreview(resource); StateHasChanged(); return Task.CompletedTask; }
            }
        };

        if (FileOptions.AllowRename)
        {
            actions.Add(new GalleryAction
            {
                Id = "rename",
                Icon = Glyph.Pen,
                Label = resource => L["Rename {name}", ("name", resource.Name)].Value,
                Invoke = resource => { OpenRenameDialog(resource); StateHasChanged(); return Task.CompletedTask; }
            });
        }

        if (FileOptions.AllowDelete)
        {
            actions.Add(new GalleryAction
            {
                Id = "delete",
                Icon = Glyph.Trash,
                Label = resource => L["Delete {name}", ("name", resource.Name)].Value,
                Invoke = resource => { OpenDeleteDialog(resource); StateHasChanged(); return Task.CompletedTask; },
                IsDestructive = true
            });
        }

        return actions;
    }

    private async Task NotifySelectionChangedAsync()
    {
        await SelectedChanged.InvokeAsync(SelectedResources);

        StateHasChanged();
    }

    private async Task HandleSortRequestedAsync(GallerySortField field)
    {
        await HandleSortFieldChangedAsync(field);

        StateHasChanged();
    }

    private async Task ToggleSelectModeAsync()
    {
        _context.Selection.IsSelectMode = !_context.Selection.IsSelectMode;

        if (!_context.Selection.IsSelectMode)
        {
            await ClearSelectionAsync();
        }
    }

    private async Task ClearSelectionAsync()
    {
        _context.Selection.Clear();

        await SelectedChanged.InvokeAsync([]);
    }

    private async Task HandleSplitterPointerDownAsync(PointerEventArgs args)
    {
        _isResizingSidebar = true;
        _splitterOrigin = args.ClientX;
        _splitterStartWidth = _sidebarWidth;

        if (_splitterModule is not null)
        {
            try
            {
                await _splitterModule.InvokeVoidAsync("capturePointer", _splitterElement, args.PointerId);
            }
            catch (JSException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private void HandleSplitterPointerMove(PointerEventArgs args)
    {
        if (!_isResizingSidebar)
        {
            return;
        }

        // The sidebar sits at the inline start, so a right-to-left culture grows it as the pointer moves the other way.
        var delta = (args.ClientX - _splitterOrigin) / RootFontSize;
        var direction = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? -1 : 1;

        SetSidebarWidth(_splitterStartWidth + (delta * direction));
    }

    private async Task HandleSplitterPointerUpAsync(PointerEventArgs args)
    {
        _isResizingSidebar = false;

        if (_splitterModule is not null)
        {
            try
            {
                await _splitterModule.InvokeVoidAsync("releasePointer", _splitterElement, args.PointerId);
            }
            catch (JSException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private void HandleSplitterKeyDown(KeyboardEventArgs args)
    {
        var forward = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? "ArrowLeft" : "ArrowRight";
        var backward = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft ? "ArrowRight" : "ArrowLeft";

        if (args.Key == forward)
        {
            SetSidebarWidth(_sidebarWidth + 1);
        }
        else if (args.Key == backward)
        {
            SetSidebarWidth(_sidebarWidth - 1);
        }
        else if (args.Key == "Home")
        {
            SetSidebarWidth(MinSidebarWidth);
        }
        else if (args.Key == "End")
        {
            SetSidebarWidth(MaxSidebarWidth);
        }
    }

    private void SetSidebarWidth(double width)
        => _sidebarWidth = Math.Clamp(width, MinSidebarWidth, MaxSidebarWidth);

    private string SidebarSizeStyle
        => string.Create(CultureInfo.InvariantCulture, $"--forma-gallery-sidebar-size: {_sidebarWidth}rem");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _splitterModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Forma.UI/Components/Gallery/FormaGallery.razor.js");
            }
            catch (JSException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
        }

        await SyncSentinelAsync();
    }

    private async Task SyncSentinelAsync()
    {
        if (_splitterModule is null)
        {
            return;
        }

        // Only automatic scrolling watches the sentinel; the manual mode waits for the button to be pressed.
        var wantsSentinel = Options.InfiniteScroll == GalleryInfiniteScrollMode.Automatic && HasMorePages;

        if (wantsSentinel == _isSentinelObserved && !_isSentinelStale)
        {
            return;
        }

        try
        {
            if (wantsSentinel)
            {
                _selfReference ??= DotNetObjectReference.Create(this);

                await _splitterModule.InvokeVoidAsync("observeSentinel", _sentinelId, _scrollId, _selfReference);
            }
            else
            {
                await _splitterModule.InvokeVoidAsync("disconnectSentinel", _sentinelId);
            }

            _isSentinelObserved = wantsSentinel;
            _isSentinelStale = false;
        }
        catch (JSException)
        {
        }
        catch (JSDisconnectedException)
        {
        }
    }

    /// <summary>Appends the next page of media, as the reader scrolls toward the end of the current one.</summary>
    [JSInvokable]
    public async Task LoadNextPageAsync()
    {
        if (_isLoadingPage || !IsInfiniteScrolling || !HasMorePages)
        {
            return;
        }

        _page = EffectivePage + 1;

        // An observer reports crossings, not a standing state, so it is re-armed to keep filling a viewport the new page did not.
        _isSentinelStale = true;
        _isLoadingPage = true;

        StateHasChanged();

        try
        {
            await QueryAsync(append: true);
            await OnPageChanged.InvokeAsync(_page);
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
        if (_splitterModule is not null)
        {
            try
            {
                if (_isSentinelObserved)
                {
                    await _splitterModule.InvokeVoidAsync("disconnectSentinel", _sentinelId);
                }

                await _splitterModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        _selfReference?.Dispose();
        _queryCancellation?.Dispose();
        _searchCancellation?.Dispose();
    }

    private void CloseDeleteDialog()
        => _deletingResources = null;

    private void CloseFolderDeleteDialog()
        => _deletingFolders = null;

    private void CloseFolderDialog()
        => _isNewFolderOpen = false;

    private void CloseFolderRenameDialog()
        => _renamingFolder = null;

    private void ClosePreview()
        => _previewResource = null;

    private void CloseRenameDialog()
        => _renamingResource = null;

    private void CloseUpload()
    {
        _isUploadOpen = false;
        _pickedUploadFiles = null;
    }

    private async Task HandleBackAsync()
    {
        if (CanGoBack)
        {
            _historyIndex--;
            var folderSource = _history[_historyIndex];
            var folder = Folders?.FirstOrDefault(f => f.Source == folderSource);
            _page = 1;

            await OnFolderChanged.InvokeAsync(folder);
        }
    }

    private async Task HandleCreateFolderAsync()
    {
        if (string.IsNullOrWhiteSpace(_newFolderName))
        {
            return;
        }

        var request = new GalleryFolderCreateRequest(_newFolderParent, _newFolderName.Trim());
        _isNewFolderOpen = false;
        _newFolderName = string.Empty;

        await OnFolderCreate.InvokeAsync(request);
    }
    private async Task HandleDeleteConfirmedAsync()
    {
        if (_deletingResources is { Count: > 0 } targets)
        {
            _deletingResources = null;

            foreach (var target in targets)
            {
                _context.Selection.Remove(target);
            }

            await SelectedChanged.InvokeAsync(SelectedResources);
            await OnDelete.InvokeAsync(new GalleryDeleteRequest(targets));
            await RefreshProviderAsync();
        }
    }

    private async Task HandleFolderDeleteConfirmedAsync()
    {
        if (_deletingFolders is { Count: > 0 } folders)
        {
            _deletingFolders = null;
            _context.Selection.Clear();

            foreach (var folder in folders)
            {
                await OnFolderDelete.InvokeAsync(new GalleryFolderDeleteRequest(folder));
            }
        }
    }

    private async Task HandleFolderRenameConfirmedAsync()
    {
        if (_renamingFolder is not null && !string.IsNullOrWhiteSpace(_renameFolderName))
        {
            var folder = _renamingFolder;
            var newName = _renameFolderName.Trim();
            _renamingFolder = null;

            await OnFolderRename.InvokeAsync(new GalleryFolderRenameRequest(folder, newName));
        }
    }

    private async Task HandleFolderSelectedAsync(FolderResourceInfo? folder)
    {
        if (CurrentFolder?.Source == folder?.Source)
        {
            return;
        }

        if (_historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
        }

        _history.Add(folder?.Source);
        _historyIndex = _history.Count - 1;
        _page = 1;

        await OnFolderChanged.InvokeAsync(folder);
    }

    private async Task HandleForwardAsync()
    {
        if (CanGoForward)
        {
            _historyIndex++;
            var folderSource = _history[_historyIndex];
            var folder = Folders?.FirstOrDefault(f => f.Source == folderSource);
            _page = 1;

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
                await OnFolderDrop.InvokeAsync(new GalleryFolderDropRequest(draggedFolder, targetFolder));
                await RefreshProviderAsync();
            }

            return;
        }

        if (draggedResource is not null)
        {
            var resources = _context.Selection.IsSelected(draggedResource) && SelectedResources.Count > 1
                ? SelectedResources
                : [draggedResource];

            if (!await EnsureFolderHasRoomAsync(targetFolder, resources.Count))
            {
                return;
            }

            await OnDrop.InvokeAsync(new GalleryDropRequest(resources, targetFolder));

            // A move takes the resources out of the window they were dragged from, which only a re-read will show.
            _context.Selection.Clear();
            await NotifySelectionChangedAsync();
            await RefreshProviderAsync();
        }
    }

    private async Task HandlePageChangedAsync(int page)
    {
        _page = page;

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
        if (_renamingResource is null || string.IsNullOrWhiteSpace(_renameNewName))
        {
            return;
        }

        var resource = _renamingResource;
        var newName = _renameNewName.Trim();
        _renamingResource = null;

        await OnRename.InvokeAsync(new GalleryRenameRequest(resource, newName));
        await RefreshProviderAsync();
    }

    private async Task HandleSearchQueryChangedAsync(string query)
    {
        _searchQuery = query;
        _page = 1;
        _filteredAndSorted = null;

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

    private async Task HandleSortDirectionChangedAsync(GallerySortDirection direction)
    {
        _sortDirection = direction;
        _page = 1;
        _filteredAndSorted = null;
        SyncContext();

        await QueryAsync(append: false);
    }

    private async Task HandleSortFieldChangedAsync(GallerySortField field)
    {
        if (_sortField == field)
        {
            _sortDirection = _sortDirection == GallerySortDirection.Ascending
                ? GallerySortDirection.Descending
                : GallerySortDirection.Ascending;
        }
        else
        {
            _sortField = field;
            _sortDirection = GallerySortDirection.Ascending;
        }

        _page = 1;
        _filteredAndSorted = null;
        SyncContext();

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

        if (_acceptFilter is { } filter
            && files.FirstOrDefault(file => !filter.Accepts(file.Name, file.ContentType)) is { } refused)
        {
            await RejectAsync(
                GalleryRejectionReason.ResourceKind,
                0,
                L["\"{name}\" is not a kind of file this gallery accepts.", ("name", refused.Name)].Value);

            return;
        }

        if (!await EnsureFolderHasRoomAsync(CurrentFolder, files.Count)
            || !await EnsureGalleryHasRoomAsync(files.Count, files.Sum(file => file.Size)))
        {
            return;
        }

        _pickedUploadFiles = files;
    }

    private async Task HandleViewModeChangedAsync(GalleryViewMode mode)
    {
        _viewMode = mode;

        await ViewModeChanged.InvokeAsync(mode);
    }

    private void OpenDeleteDialog(ResourceInfo? single = null)
    {
        if (single is not null)
        {
            _deletingResources = [single];
        }
        else if (_context.Selection.HasFolders)
        {
            _deletingFolders = [.. SelectedFolders];
        }
        else if (_context.Selection.HasResources)
        {
            _deletingResources = [.. SelectedResources];
        }
    }

    private void OpenFolderDeleteDialog(FolderResourceInfo folder)
        => _deletingFolders = [folder];

    // Every caller names the parent it means, so no folder here is the root rather than wherever the reader happens to be.
    private async Task OpenFolderDialogAsync(FolderResourceInfo? parent)
    {
        _rejectionMessage = null;

        if (!await EnsureFolderFitsAsync(parent, height: 1))
        {
            return;
        }

        _newFolderParent = parent;
        _newFolderName = string.Empty;
        _isNewFolderOpen = true;
    }

    private void OpenFolderRenameDialog(FolderResourceInfo folder)
    {
        _renamingFolder = folder;
        _renameFolderName = folder.Name;
    }

    private void OpenPreview(ResourceInfo resource)
    {
        // Captured once so the viewer is not handed a new list on every unrelated render.
        _previewSiblings = [.. VisibleResources.Select(sibling => sibling.Source)];
        _previewResource = resource;
    }

    private void OpenRenameDialog(ResourceInfo resource)
    {
        _renamingResource = resource;
        _renameNewName = resource.Name;
    }

    private void ToggleUpload()
        => _isUploadOpen = !_isUploadOpen;
}
