namespace Forma.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Blazor.Components.DataView;
using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Files;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Catalog.Requests;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Css;
using Forma.Framework.Events;
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

    /// <summary>The items to display, held in full.</summary>
    [Parameter] public IReadOnlyList<ICatalogItem>? Items { get; set; }

    /// <summary>The provider the items are read from, a slice at a time.</summary>
    [Parameter] public ICatalogProvider? ItemsProvider { get; set; }

    /// <summary>The folders the sidebar's tree shows and items are moved between.</summary>
    [Parameter] public IReadOnlyList<ICatalogItem>? Folders { get; set; }

    private bool ShowsSidebar
        => Options.ShowSidebar && Folders is { Count: > 0 };

    /// <summary>The total count of items the whole library holds, where null counts the supplied items.</summary>
    [Parameter] public int? TotalCount { get; set; }

    /// <summary>The total bytes the whole library holds, where null measures the supplied items.</summary>
    [Parameter] public long? TotalBytes { get; set; }

    /// <summary>The constraints the explorer keeps.</summary>
    [Parameter] public CatalogConstraints Constraints { get; set; } = CatalogConstraints.None;

    /// <summary>Raised when a constraint stops an operation.</summary>
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

    /// <summary>The search box's placeholder text.</summary>
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

    /// <summary>The field the shown items are grouped by.</summary>
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

    // A dialog's name box is a field of the dialog's own form, outside any form the explorer stands in.
    private readonly EditContext _dialogForm = new(new object());

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

    // The kinds an upload may add, where null is the kinds the explorer lists.
    private CatalogResourceFilter? _uploadFilter;

    // The groups of the browsing sequence under way, defined against the moment it started.
    private CatalogGrouping? _grouping;

    // The grouped field, direction and culture the groups were defined for.
    private (ExplorerGroupField Field, CatalogSortDirection Direction, CultureInfo UICulture)? _groupingBasis;

    // The key an item goes by across reloads, the same delegate on every render.
    private static readonly Func<ICatalogItem, object> ItemKeyOf = static item => item.Key;

    // The name an item goes by in the data view's views and their accessible names, the same delegate on every render.
    private static readonly Func<ICatalogItem, string> ItemNameOf = static item => item.Name;

    // A disabled item is drawn muted and cannot be chosen, the same delegate on every render.
    private static readonly Func<ICatalogItem, bool> IsItemDisabled = static item => item.IsDisabled;

    // The classes an item's row carries for its state.
    private static readonly Func<ICatalogItem, string?> GetItemClass = static item => item.GetStateClasses();

    // Whether an item is a folder holding folders, the children the sidebar's tree opens on, made once and reading the folders in force.
    private Func<ICatalogItem, bool> _holdsFolders = default!;

    // The folders that start open in the sidebar, every one holding folders, made again for a new set of folders.
    private (CatalogFolderTree Tree, IReadOnlyCollection<object> Keys)? _openFolderKeys;

    // What the data view reads the content from, and the provider it is handed, made again when the items, the provider or the folders
    // the explorer is given change, which the data view reads as a new source.
    private ExplorerDataViewSource _source = default!;

    private DataViewItemsProvider<ICatalogItem> _contentProvider = default!;

    private (IReadOnlyList<ICatalogItem>? Items, ICatalogProvider? Provider, IReadOnlyList<ICatalogItem>? Folders) _contentBasis;

    // The files of the items the explorer holds, kept while the items are.
    private CatalogListProvider? _heldProvider;

    private IReadOnlyList<ICatalogItem>? _heldItems;

    private FormaDataView<ICatalogItem>? _dataView;

    // Whether the move under way steps through the history rather than visiting, and whether it reached the folder it stepped to.
    private bool _isHistoryMove;

    private bool _hasHistoryMoveLanded;

    // The grouping, the sort and the kinds listed the content was last asked in, which the host can change as well as the reader.
    private (ExplorerGroupField GroupField, CatalogSortField SortField, CatalogSortDirection Direction, CatalogResourceFilter? Filter)? _queriedBasis;

    private IReadOnlyList<ExplorerItemAction>? _actions;

    private CatalogFolderTree? _folderTree;

    private IReadOnlyList<ICatalogFolderItem>? _breadcrumbs;

    /// <summary>The folder list indexed as a tree.</summary>
    private CatalogFolderTree FolderTree
        => _folderTree ??= new CatalogFolderTree(Folders);

    // Every folder holding folders starts open, as the explorer's tree always has.
    private IReadOnlyCollection<object> OpenFolderKeys
    {
        get
        {
            if (_openFolderKeys is not { } open || !ReferenceEquals(open.Tree, FolderTree))
            {
                open = (FolderTree, [.. FolderTree.GetExpandableFolders().Select(folder => (object)folder.Key)]);
                _openFolderKeys = open;
            }

            return open.Keys;
        }
    }

    // A folder's row is dragged where the folder may be moved and takes a drop where items may land in it, named by its key; the row
    // standing for every item takes a drop on the root and names none.
    private Dictionary<string, object>? GetItemAttributes(ICatalogItem item)
    {
        if (item.Key == FormaExplorerSidebar.AllMediaKey)
        {
            return new Dictionary<string, object>(StringComparer.Ordinal) { ["data-drop-target"] = "true" };
        }

        if (item is not ICatalogFolderItem folder)
        {
            return null;
        }

        var allowed = folder.GetCapabilities(ItemOptions);
        Dictionary<string, object> attributes = new(StringComparer.Ordinal)
        {
            ["draggable"] = allowed.HasFlag(CatalogItemCapabilities.Drag) ? "true" : "false",
            ["data-drop-key"] = folder.Key,
            ["ondragstart"] = EventCallback.Factory.Create(this, () => _itemManager.BeginDrag(folder)),
        };

        if (allowed.HasFlag(CatalogItemCapabilities.Drop))
        {
            attributes["data-drop-target"] = "true";
        }

        return attributes;
    }

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

    /// <summary>How many items match the current search and sort, where null means the provider cannot say.</summary>
    /// <remarks>
    /// Pages are cut in the store's own positions, so paging reads the provider's reported total; appended slices
    /// are counted against how many items the whole query lists, which only a provider that guarantees it gives.
    /// </remarks>
    private int? MatchingCount
        => ContentView?.TotalItemCount;

    // The content the data view holds, which it reads from the source once it has drawn.
    private IDataView<ICatalogItem>? ContentView
        => _dataView;

    private DataViewAppendMode AppendMode
        => Options.InfiniteScroll switch
        {
            ExplorerInfiniteScrollMode.Automatic => DataViewAppendMode.Automatic,
            ExplorerInfiniteScrollMode.Manual => DataViewAppendMode.Manual,
            _ => DataViewAppendMode.Off,
        };

    /// <summary>The items on screen.</summary>
    private IReadOnlyList<ICatalogItem> VisibleItems
        => ContentView?.Items ?? [];

    /// <summary>The groups of the browsing sequence under way, defined again when the grouped field, the direction or the culture changes.</summary>
    private CatalogGrouping? Grouping
    {
        get
        {
            if (_groupingBasis != (_groupField, _collection.SortDirection, Context.CultureState.UICulture))
            {
                StartSequence();
            }

            return _grouping;
        }
    }

    // A browsing sequence defines its groups once, so a slice read after midnight lands in the groups of the first.
    private void StartSequence()
    {
        _grouping = ExplorerGrouping.Define(
            _groupField,
            _collection.SortDirection,
            GetKindLabel,
            Context.CultureState.UICulture,
            Clock.GetUtcNow(),
            Clock.LocalTimeZone,
            Context.CultureState.Culture.DateTimeFormat.FirstDayOfWeek);
        _groupingBasis = (_groupField, _collection.SortDirection, Context.CultureState.UICulture);
    }

    private string GetKindLabel(CatalogItemKind kind)
        => ExplorerItemLocalization.GetKindLabel(L, kind);

    // Kind groups stand in the order of their labels in the reader's culture, so the content is asked for again in the new order.
    private Task HandleCultureChangedAsync()
        => IsDisposed || _groupField != ExplorerGroupField.Kind
            ? Task.CompletedTask
            : InvokeAsync(RestartContentAsync);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Context.CultureState.Subscribe(HandleCultureChangedAsync);
        _source = new ExplorerDataViewSource(() => FolderTree, GetFileProvider, CreateContentQuery, () => _collection.Filter);
        _holdsFolders = item => item is ICatalogFolderItem folder && FolderTree.GetListedChildren(folder.Key).Count > 0;

        _collection = new CatalogCollectionView(item => GetKindLabel(item.GetKind()), () => Context.CultureState.UICulture)
        {
            PageNumber = CurrentPage,
            SortField = SortField,
            SortDirection = SortDirection,
        };

        _viewMode = ViewMode;
        _groupField = GroupField;
        _detailsColumns = Options.DetailsColumns;
        _showsDetailsHeader = Options.ShowDetailsHeader;

        _paramStateTracker.Track(() => Constraints.AcceptedResourceKinds, accepted => _collection.Filter = CatalogResourceFilter.Parse(accepted));
        _paramStateTracker.Track(() => Constraints.AcceptedUploadKinds, accepted => _uploadFilter = CatalogResourceFilter.Parse(accepted));
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
        _paramStateTracker.Track(() => Items, _ => StartSequence());

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

        _folderTree = null;
        _breadcrumbs = null;

        _paramStateTracker.Apply();

        // A new list, provider or set of folders is a new source, which the data view asks again, keeping what the reader did.
        if (_contentProvider is null || !ReferenceEquals(_contentBasis.Items, Items) || !ReferenceEquals(_contentBasis.Provider, ItemsProvider) || !ReferenceEquals(_contentBasis.Folders, Folders))
        {
            _contentBasis = (Items, ItemsProvider, Folders);
            _contentProvider = _source.ProvideAsync;
        }

        _selection.SelectionLimit = Options.MaxSelection;

        _itemManager.Actions = Actions;
        _itemManager.ItemOptions = ItemOptions;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        // A grouping, a sort or kinds listed that the host sets order or narrow the store's answer anew, so the content starts again; the
        // first parameters are the ones the data view first asks in.
        if (_queriedBasis is null)
        {
            _queriedBasis = QueryBasis;
        }
        else if (_queriedBasis != QueryBasis)
        {
            await RestartContentAsync();
        }
    }

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        // The data view announces what it holds as it changes, which the explorer's own parts draw from.
        if (firstRender && _dataView is IDataView<ICatalogItem> content)
        {
            content.Subscribe(HandleContentChangedAsync);
        }
    }

    private Task HandleContentChangedAsync()
        => InvokeAsync(StateHasChanged);

    // The files of the location, the host's provider's or the items the explorer holds.
    private ICatalogProvider GetFileProvider()
    {
        if (ItemsProvider is { } provider)
        {
            return provider;
        }

        var items = Items ?? [];

        if (_heldProvider is null || !ReferenceEquals(_heldItems, items))
        {
            _heldItems = items;
            _heldProvider = new CatalogListProvider(items, item => GetKindLabel(item.GetKind()), () => Context.CultureState.UICulture);
        }

        return _heldProvider;
    }

    // The query for a location's content, read as the data view asks, so it carries the search, the sort and the groups in force.
    private CatalogQuery CreateContentQuery(string? folderKey, int skip, int take)
        => new(folderKey, skip, take, string.IsNullOrWhiteSpace(_collection.SearchQuery) ? null : _collection.SearchQuery, _collection.SortField, _collection.SortDirection)
        {
            Filter = _collection.Filter,
            Grouping = Grouping,
            IncludesGroupSizes = Grouping is not null,
        };

    private (ExplorerGroupField GroupField, CatalogSortField SortField, CatalogSortDirection Direction, CatalogResourceFilter? Filter) QueryBasis
        => (_groupField, _collection.SortField, _collection.SortDirection, _collection.Filter);

    /// <summary>Reads the items shown again.</summary>
    private Task RefreshProviderAsync()
    {
        StartSequence();

        return _dataView?.RefreshDataAsync() ?? Task.CompletedTask;
    }

    // A new search, sort or grouping starts a browsing sequence of its own on the first page; a move back to the first page reads it
    // there, and a data view on that page already reads it again.
    private Task RestartContentAsync()
    {
        _queriedBasis = QueryBasis;
        StartSequence();

        if (_collection.PageNumber != 1)
        {
            _collection.PageNumber = 1;
            StateHasChanged();

            return Task.CompletedTask;
        }

        return _dataView?.RefreshDataAsync() ?? Task.CompletedTask;
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

    // How many levels the folder's own subtree spans, the folder itself counting as one.
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
                L["This explorer holds at most {Size}.", ResourceFormatter.FromByteCount(Constraints.MaxTotalBytes, Context.CultureState.Culture)].Value);

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
        => ExplorerGrouping.GroupBy(VisibleItems, Grouping, GetKindLabel, _source.GroupSizes);

    bool IExplorerContentView.IsCollapsed(ExplorerItemGroup group)
    {
        ArgumentNullException.ThrowIfNull(group);

        return _collapsedGroups.Contains(group.Key);
    }

    ICatalogSelectionView IExplorerView.Selection => _selection;

    IExplorerItemView IExplorerView.Item => _itemManager;

    IReadOnlyList<ICatalogItem> IExplorerContentView.VisibleItems => VisibleItems;

    bool IExplorerContentView.IsFetching => ContentView?.IsBusy ?? true;

    int? IExplorerContentView.MatchingCount => MatchingCount;

    string IExplorerSearchView.SearchQuery => _collection.SearchQuery;

    string? IExplorerSearchView.SearchPlaceholder => SearchPlaceholder;

    // A cascaded call's implicit render lands on the descendant that wired it, so this component's render is
    // requested explicitly, through the renderer, since a caller can still be dispatching its own event.
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

    /// <summary>Changes the selection and reports it.</summary>
    private async Task ChangeSelectionAsync(Action<CatalogSelection> change)
    {
        change(_selection);

        await SelectedChanged.InvokeAsync(_selection.Items);

        StateHasChanged();
    }

    // The host's own selection is adopted without being reported back.
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

            if (!await MoveThroughHistoryAsync())
            {
                _history.GoForward();
            }
        }
    }

    // A step back or forward moves into the folder at the history's cursor without visiting it, and reports whether it got there.
    private async Task<bool> MoveThroughHistoryAsync()
    {
        _isHistoryMove = true;
        _hasHistoryMoveLanded = false;

        try
        {
            await MoveToFolderAsync(FolderTree.Find(_history.Current));
        }
        finally
        {
            _isHistoryMove = false;
        }

        return _hasHistoryMoveLanded;
    }

    // Every move goes through the data view's change of location, which the explorer accepts or refuses before anything changes.
    private Task MoveToFolderAsync(ICatalogFolderItem? folder)
        => _dataView is { } view ? view.MakeCurrentAsync(folder) : Task.CompletedTask;

    // A file, or a folder the reader may not open, is refused before the location changes.
    private void HandleCurrentItemChanging(DataViewCurrentItemChange<ICatalogItem> change)
    {
        if (change.Item is { } item && (item is not ICatalogFolderItem folder || !folder.Permits(ItemOptions, CatalogItemCapabilities.Open)))
        {
            change.Refuse();
        }
    }

    // A move that succeeded clears the selection, records the visit unless it stepped through the history, and starts the location's sequence.
    private async Task HandleCurrentItemChangedAsync(ICatalogItem? item)
    {
        var folder = item as ICatalogFolderItem;

        if (_isHistoryMove)
        {
            _hasHistoryMoveLanded = true;
        }
        else
        {
            _history.Visit(folder?.Key);
        }

        _collection.PageNumber = 1;
        StartSequence();
        await ClearSelectionAsync();
        await OnFolderChanged.InvokeAsync(folder);
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

    private Task HandleFolderSelectedAsync(ICatalogFolderItem? folder)
        => MoveToFolderAsync(folder);

    private async Task HandleForwardAsync()
    {
        if (CanGoForward)
        {
            _history.GoForward();

            if (!await MoveThroughHistoryAsync())
            {
                _history.GoBack();
            }
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

    private async Task HandlePageChangedAsync(int page)
    {
        _collection.PageNumber = page;

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

        // Held items are filtered at once; a provider is asked once the reader pauses, the debouncer resuming off the renderer.
        if (!IsProviderMode)
        {
            await RestartContentAsync();
            return;
        }

        await SearchDebouncer.DebounceAsync(_ => InvokeAsync(RestartContentAsync), Options.SearchDebounceDelay);
    }

    private Task HandleSortDirectionChangedAsync(CatalogSortDirection direction)
    {
        _collection.SortDirection = direction;

        return RestartContentAsync();
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

        await RestartContentAsync();
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

        if ((_uploadFilter ?? _collection.Filter) is { } filter
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

        // The store orders by the groups, so the content is asked for again, once, whether or not the host binds the field back.
        await RestartContentAsync();
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

    // A null parent is the top level.
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

        if (item is ICatalogFolderItem folder)
        {
            return MoveToFolderAsync(folder);
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
        Context.CultureState.Unsubscribe(HandleCultureChangedAsync);
        (_dataView as IDataView<ICatalogItem>)?.Unsubscribe(HandleContentChangedAsync);
        SearchDebouncer.Cancel();

        return base.DisposeAsyncCore();
    }
}
