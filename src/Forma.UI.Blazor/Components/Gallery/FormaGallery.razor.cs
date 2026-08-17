namespace Forma.UI.Components.Gallery;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Components.Feedback;

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

    /// <summary>Whether the gallery is presented in dialog presentation mode.</summary>
    [Parameter] public bool AsDialog { get; set; }

    /// <summary>The currently active folder, or null for all media.</summary>
    [Parameter] public GalleryFolderInfo? CurrentFolder { get; set; }

    /// <summary>The active page, 1-based.</summary>
    [Parameter] public int CurrentPage { get; set; } = 1;

    /// <summary>Whether infinite scroll dynamic chunk loading is enabled.</summary>
    [Parameter] public bool EnableInfiniteScroll { get; set; }

    /// <summary>The media entries stored on the server to display in the gallery.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<ResourceInfo> Entries { get; set; } = [];

    /// <summary>Available folders for grouping media.</summary>
    [Parameter] public IReadOnlyList<GalleryFolderInfo>? Folders { get; set; }

    /// <summary>Whether the folder tree sidebar is shown.</summary>
    [Parameter] public bool ShowSidebar { get; set; } = true;

    private bool ShowsSidebar
        => ShowSidebar && Folders is { Count: > 0 };

    /// <summary>Whether each breadcrumb offers a menu of that folder's children.</summary>
    [Parameter] public bool ShowFolderMenu { get; set; } = true;

    /// <summary>How many items the whole library holds, for hosts that supply one folder at a time; the supplied entries are counted when null.</summary>
    [Parameter] public int? TotalCount { get; set; }

    /// <summary>The maximum number of items that may be selected at once (0 for unlimited).</summary>
    [Parameter] public int MaxSelection { get; set; }

    /// <summary>Which operations the gallery offers.</summary>
    [Parameter] public FormaGalleryOptions Options { get; set; } = FormaGalleryOptions.Default;

    /// <summary>Raised when media items are deleted.</summary>
    [Parameter] public EventCallback<GalleryDeleteRequest> OnDelete { get; set; }

    /// <summary>Raised when media items are dropped onto a target folder.</summary>
    [Parameter] public EventCallback<GalleryDropRequest> OnDrop { get; set; }

    /// <summary>Raised when the active folder changes.</summary>
    [Parameter] public EventCallback<GalleryFolderInfo?> OnFolderChanged { get; set; }

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

    /// <summary>The number of media entries shown per page.</summary>
    [Parameter] public int PageSize { get; set; } = DefaultPageSize;

    /// <summary>Whether the gallery container is resizable by the user.</summary>
    [Parameter] public bool Resizable { get; set; }

    /// <summary>Custom search placeholder text.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>The currently selected resources.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo> Selected { get; set; } = [];

    /// <summary>Raised when the selected items change.</summary>
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

    private List<GalleryFolderInfo>? _deletingFolders;

    private List<ResourceInfo>? _deletingResources;

    private GalleryFolderInfo? _draggedFolder;

    private ResourceInfo? _draggedItem;

    private readonly List<string?> _history = [null];

    private int _historyIndex;

    private bool _isNewFolderOpen;

    private bool _isUploadOpen;

    private string _newFolderName = string.Empty;

    private GalleryFolderInfo? _newFolderParent;

    private int _page = 1;

    private IReadOnlyList<IBrowserFile>? _pickedUploadFiles;

    private ResourceInfo? _previewResource;

    private IReadOnlyList<string> _previewSiblings = [];

    private string _renameFolderName = string.Empty;

    private string _renameNewName = string.Empty;

    private GalleryFolderInfo? _renamingFolder;

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

    private FormaGalleryOptions? _actionOptions;

    private Dictionary<string, GalleryFolderInfo> FolderLookup
    {
        get
        {
            var lookup = new Dictionary<string, GalleryFolderInfo>(StringComparer.Ordinal);

            foreach (var folder in Folders ?? [])
            {
                lookup.TryAdd(folder.Id, folder);
            }

            return lookup;
        }
    }

    private IReadOnlyList<GalleryFolderInfo> Breadcrumbs
    {
        get
        {
            if (CurrentFolder is null || Folders is not { Count: > 0 })
            {
                return [];
            }

            var path = new List<GalleryFolderInfo>();
            var lookup = FolderLookup;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var current = CurrentFolder;

            // A host can supply a cyclic parent chain, so each folder is walked at most once.
            while (current is not null && visited.Add(current.Id))
            {
                path.Insert(0, current);
                current = current.ParentId is { } parentId && lookup.TryGetValue(parentId, out var parent)
                    ? parent
                    : null;
            }

            return path;
        }
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

    private List<ResourceInfo> FilteredAndSortedEntries
    {
        get
        {
            if (_filteredAndSorted is not null)
            {
                return _filteredAndSorted;
            }

            IEnumerable<ResourceInfo> query = Entries;

            if (!string.IsNullOrWhiteSpace(_searchQuery))
            {
                query = query.Where(entry => entry.Name.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            query = (_sortField, _sortDirection) switch
            {
                (GallerySortField.Name, GallerySortDirection.Ascending) => query.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (GallerySortField.Name, GallerySortDirection.Descending) => query.OrderByDescending(e => e.Name, StringComparer.OrdinalIgnoreCase),
                (GallerySortField.Size, GallerySortDirection.Ascending) => query.OrderBy(e => e.Size ?? 0),
                (GallerySortField.Size, GallerySortDirection.Descending) => query.OrderByDescending(e => e.Size ?? 0),
                (GallerySortField.Kind, GallerySortDirection.Ascending) => query.OrderBy(e => e.Kind.ToString()),
                (GallerySortField.Kind, GallerySortDirection.Descending) => query.OrderByDescending(e => e.Kind.ToString()),
                (GallerySortField.DateAdded, GallerySortDirection.Ascending) => query.OrderBy(e => e.Added() ?? DateTimeOffset.MinValue),
                (GallerySortField.DateAdded, GallerySortDirection.Descending) => query.OrderByDescending(e => e.Added() ?? DateTimeOffset.MinValue),
                (GallerySortField.DateModified, GallerySortDirection.Ascending) => query.OrderBy(e => e.Modified() ?? DateTimeOffset.MinValue),
                (GallerySortField.DateModified, GallerySortDirection.Descending) => query.OrderByDescending(e => e.Modified() ?? DateTimeOffset.MinValue),
                _ => query
            };

            _filteredAndSorted = [.. query];

            return _filteredAndSorted;
        }
    }

    private int PageCount
        => Math.Max(1, (int)Math.Ceiling(FilteredAndSortedEntries.Count / (double)PageSize));

    private List<ResourceInfo> PagedEntries
    {
        get
        {
            if (EnableInfiniteScroll)
            {
                return [.. FilteredAndSortedEntries.Take(EffectivePage * PageSize)];
            }

            return [.. FilteredAndSortedEntries.Skip((EffectivePage - 1) * PageSize).Take(PageSize)];
        }
    }

    private List<ResourceInfo> SelectedResources
        => [.. _context.Selection.Resolve(Entries)];

    private IReadOnlyList<GalleryFolderInfo> SelectedFolders
        => _context.Selection.ResolveFolders(Folders ?? []);

    protected override void OnInitialized()
    {
        _context = new GalleryContext(
            entry => { OpenPreview(entry); StateHasChanged(); return Task.CompletedTask; },
            NotifySelectionChangedAsync,
            HandleSortRequestedAsync,
            entry => _draggedItem = entry);

        _context.SelectLabel = entry => _context.Selection.IsSelected(entry)
            ? L["Deselect {name}", ("name", entry.Name)].Value
            : L["Select {name}", ("name", entry.Name)].Value;
        _context.ActivateLabel = entry => _context.Selection.IsSelectMode
            ? _context.SelectLabel(entry)
            : L["Preview {name}", ("name", entry.Name)].Value;

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
        _filteredAndSorted = null;

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
        _context.Selection.MaxSelection = MaxSelection;
        _context.SortField = _sortField;
        _context.SortDirection = _sortDirection;

        // Only the options decide which actions exist, so the list and its closures survive an unrelated render.
        if (!ReferenceEquals(_actionOptions, Options))
        {
            _actionOptions = Options;
            _context.ItemActions = BuildItemActions();
        }
    }

    private List<GalleryAction> BuildItemActions()
    {
        var actions = new List<GalleryAction>
        {
            new()
            {
                Id = "preview",
                Icon = "fa-eye",
                Label = entry => L["Preview {name}", ("name", entry.Name)].Value,
                Invoke = entry => { OpenPreview(entry); StateHasChanged(); return Task.CompletedTask; }
            }
        };

        if (Options.AllowRename)
        {
            actions.Add(new GalleryAction
            {
                Id = "rename",
                Icon = "fa-pen",
                Label = entry => L["Rename {name}", ("name", entry.Name)].Value,
                Invoke = entry => { OpenRenameDialog(entry); StateHasChanged(); return Task.CompletedTask; }
            });
        }

        if (Options.AllowDelete)
        {
            actions.Add(new GalleryAction
            {
                Id = "delete",
                Icon = "fa-trash",
                Label = entry => L["Delete {name}", ("name", entry.Name)].Value,
                Invoke = entry => { OpenDeleteDialog(entry); StateHasChanged(); return Task.CompletedTask; },
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

    private Task HandleSortRequestedAsync(GallerySortField field)
    {
        HandleSortFieldChanged(field);
        StateHasChanged();

        return Task.CompletedTask;
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
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_splitterModule is not null)
        {
            try
            {
                await _splitterModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
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
            var folderId = _history[_historyIndex];
            var folder = Folders?.FirstOrDefault(f => f.Id == folderId);
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

    private async Task HandleFolderSelectedAsync(GalleryFolderInfo? folder)
    {
        if (CurrentFolder?.Id == folder?.Id)
        {
            return;
        }

        if (_historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - (_historyIndex + 1));
        }

        _history.Add(folder?.Id);
        _historyIndex = _history.Count - 1;
        _page = 1;

        await OnFolderChanged.InvokeAsync(folder);
    }

    private async Task HandleForwardAsync()
    {
        if (CanGoForward)
        {
            _historyIndex++;
            var folderId = _history[_historyIndex];
            var folder = Folders?.FirstOrDefault(f => f.Id == folderId);
            _page = 1;

            await OnFolderChanged.InvokeAsync(folder);
        }
    }

    private async Task HandleItemDropOnFolderAsync(GalleryFolderInfo? targetFolder)
    {
        // The drag ends whatever the drop lands on, so a rejected target cannot leave a stale item to move later.
        var draggedFolder = _draggedFolder;
        var draggedItem = _draggedItem;
        _draggedFolder = null;
        _draggedItem = null;

        if (draggedFolder is not null)
        {
            if (CanMoveFolder(draggedFolder, targetFolder))
            {
                await OnFolderDrop.InvokeAsync(new GalleryFolderDropRequest(draggedFolder, targetFolder));
            }

            return;
        }

        if (draggedItem is not null)
        {
            var items = _context.Selection.IsSelected(draggedItem) && SelectedResources.Count > 1
                ? SelectedResources
                : [draggedItem];

            await OnDrop.InvokeAsync(new GalleryDropRequest(items, targetFolder));
        }
    }

    private async Task HandlePageChangedAsync(int page)
    {
        _page = page;

        await OnPageChanged.InvokeAsync(page);
    }

    private bool CanMoveFolder(GalleryFolderInfo folder, GalleryFolderInfo? target)
    {
        if (target is null)
        {
            return folder.ParentId is not null;
        }

        // Moving a folder inside its own subtree would leave the tree cyclic and unrenderable.
        return folder.Id != target.Id && !IsSelfOrDescendant(target, folder);
    }

    private bool IsSelfOrDescendant(GalleryFolderInfo candidate, GalleryFolderInfo ancestor)
    {
        var lookup = FolderLookup;
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = candidate;

        while (current is not null && visited.Add(current.Id))
        {
            if (current.Id == ancestor.Id)
            {
                return true;
            }

            current = current.ParentId is { } parentId && lookup.TryGetValue(parentId, out var parent)
                ? parent
                : null;
        }

        return false;
    }

    private void HandlePreviewNavigate(string source)
    {
        var next = FilteredAndSortedEntries.FirstOrDefault(entry => entry.Source == source);

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
    }

    private void HandleSearchQueryChanged(string query)
    {
        _searchQuery = query;
        _page = 1;
        _filteredAndSorted = null;
    }

    private void HandleSortDirectionChanged(GallerySortDirection direction)
    {
        _sortDirection = direction;
        _page = 1;
        _filteredAndSorted = null;
        SyncContext();
    }

    private void HandleSortFieldChanged(GallerySortField field)
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
    }

    private async Task HandleUpAsync()
    {
        if (CurrentFolder is not null)
        {
            var parent = CurrentFolder.ParentId is { } parentId
                ? Folders?.FirstOrDefault(f => f.Id == parentId)
                : null;

            await HandleFolderSelectedAsync(parent);
        }
    }

    private async Task HandleUploadCompletedAsync(IReadOnlyList<ResourceInfo> uploaded)
    {
        _pickedUploadFiles = null;
        _isUploadOpen = false;

        await OnUploaded.InvokeAsync(uploaded);
    }

    private void HandleUploadFilesPicked(IReadOnlyList<IBrowserFile> files)
        => _pickedUploadFiles = files;

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
        else if (_context.Selection.HasItems)
        {
            _deletingResources = SelectedResources;
        }
    }

    private void OpenFolderDeleteDialog(GalleryFolderInfo folder)
        => _deletingFolders = [folder];

    private void OpenFolderDialog(GalleryFolderInfo? parent = null)
    {
        _newFolderParent = parent ?? CurrentFolder;
        _newFolderName = string.Empty;
        _isNewFolderOpen = true;
    }

    private void OpenFolderRenameDialog(GalleryFolderInfo folder)
    {
        _renamingFolder = folder;
        _renameFolderName = folder.Name;
    }

    private void OpenPreview(ResourceInfo entry)
    {
        // Captured once so the viewer is not handed a new list on every unrelated render.
        _previewSiblings = [.. FilteredAndSortedEntries.Select(sibling => sibling.Source)];
        _previewResource = entry;
    }

    private void OpenRenameDialog(ResourceInfo entry)
    {
        _renamingResource = entry;
        _renameNewName = entry.Name;
    }

    private void ToggleUpload()
        => _isUploadOpen = !_isUploadOpen;
}
