namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Css;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Navigation;
using Forma.Framework.Explorer.Selection;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The explorer's folder tree sidebar, every folder row a drop target for the items and folders dragged onto it.</summary>
public sealed partial class FormaExplorerSidebar : FormaComponentBase, IExplorerSidebarView, IExplorerSidebarInteraction
{
    /// <summary>The currently active folder, or null for every item.</summary>
    [Parameter] public ICatalogFolderItem? CurrentFolder { get; set; }

    /// <summary>The folders the tree shows.</summary>
    [Parameter] public IReadOnlyList<ICatalogItem> Folders { get; set; } = [];

    /// <summary>The folders indexed as a tree.</summary>
    [Parameter] public CatalogFolderTree FolderTree { get; set; } = CatalogFolderTree.Empty;

    [CascadingParameter] private IExplorerView Explorer { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction ExplorerInteraction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder is selected.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem?> OnFolderSelected { get; set; }

    /// <summary>Raised when a folder is to be created under the folder given, where null is the top level.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem?> OnFolderCreate { get; set; }

    /// <summary>Which folder operations the sidebar offers.</summary>
    [Parameter] public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <summary>How many items the whole explorer holds.</summary>
    [Parameter] public int TotalCount { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private const double MinTreeHeight = 6;

    private const double MaxTreeHeight = 30;

    private const string DropAction = "explorer.drop";

    // The tree is one drop zone; each row that takes a drop is marked and named by its folder's key, the root's row by none.
    private static readonly DropZoneOptions TreeDropZoneOptions = new() { Targets = "[data-drop-target]", DropEffect = DropEffect.Move };

    private readonly HashSet<string> _collapsedFolderKeys = [];

    private DropZone _dropZone = default!;

    private DotNetObjectReference<FormaExplorerSidebar>? _selfReference;

    private ElementReference _tree;

    private bool _isDropZoneAttached;

    private string TitleId
        => $"forma-explorer-folders-{Context.RuntimeId:N}";

    private IEnumerable<string> ExpandableFolderKeys
        => FolderTree.GetExpandableFolders().Select(item => item.Key);

    private bool HasExpandableFolders
        => ExpandableFolderKeys.Any();

    private bool IsFullyCollapsed
        => ExpandableFolderKeys.All(_collapsedFolderKeys.Contains);

    private void ToggleAll()
    {
        if (IsFullyCollapsed)
        {
            ExpandAll();
            return;
        }

        CollapseAll();
    }

    private void ExpandAll()
        => _collapsedFolderKeys.Clear();

    private void CollapseAll()
    {
        foreach (var key in ExpandableFolderKeys)
        {
            _collapsedFolderKeys.Add(key);
        }
    }

    private double? _treeHeight;

    private string? TreeSizeStyle
        => _treeHeight is { } height
            ? new CssStyleBuilder().Add("block-size", CssLength.FromRem(height)).ToString()
            : null;

    // Every row is the same height, so the tree is sized from the folder count.
    private double MeasuredTreeHeight
        => Math.Clamp((Folders.Count + 1) * 2, MinTreeHeight, MaxTreeHeight);

    /// <inheritdoc />
    protected override void OnInitialized()
        => _dropZone = new DropZone(Context.JS);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isDropZoneAttached)
        {
            return;
        }

        _selfReference ??= DotNetObjectReference.Create(this);

        // Claimed before the await, so a render arriving mid-call attaches no second drop zone.
        _isDropZoneAttached = true;

        // The drop is a route the script declares itself, so the host's policy reaches the router before it does.
        await InputRouter.ConfigureAsync();
        _isDropZoneAttached = await _dropZone.AttachAsync(_tree, DropAction, _selfReference, TreeDropZoneOptions);
    }

    /// <summary>Takes a drop on the row the key names, where null is the root's row.</summary>
    [JSInvokable]
    public Task HandleDropAsync(string? key)
    {
        if (key is null)
        {
            return ItemInteraction.DropOnItemAsync(null);
        }

        if (FolderTree.Find(key) is { } folder)
        {
            return ItemInteraction.DropOnItemAsync(folder);
        }

        ItemInteraction.EndDrag();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _dropZone.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }

    private IReadOnlyList<ICatalogFolderItem> GetChildren(string? parent)
        => FolderTree.GetListedChildren(parent);

    private Task SelectFolderAsync(ICatalogFolderItem? folder)
        => OnFolderSelected.InvokeAsync(folder);

    private Task CreateFolderAsync(ICatalogFolderItem? parent)
        => OnFolderCreate.InvokeAsync(parent);

    ICatalogSelectionView IExplorerSidebarView.Selection => Explorer.Selection;

    IReadOnlyList<ICatalogFolderItem> IExplorerSidebarView.GetChildren(string? parent)
        => GetChildren(parent);

    bool IExplorerSidebarView.IsCurrent(ICatalogFolderItem item)
        => CurrentFolder?.Key == item.Key;

    // Folders start expanded.
    bool IExplorerSidebarView.IsExpanded(string folderKey)
        => !_collapsedFolderKeys.Contains(folderKey);

    void IExplorerSidebarInteraction.ToggleExpanded(string folderKey)
    {
        if (!_collapsedFolderKeys.Remove(folderKey))
        {
            _collapsedFolderKeys.Add(folderKey);
        }

        // The toolbar toggle reflects the whole tree, so the sidebar rerenders for a change made inside a node.
        StateHasChanged();
    }

    Task IExplorerSidebarInteraction.SelectFolderAsync(ICatalogFolderItem? item)
        => SelectFolderAsync(item);

    Task IExplorerSidebarInteraction.RenameFolderAsync(ICatalogFolderItem item)
    {
        ItemInteraction.RequestRename(item);

        return Task.CompletedTask;
    }

    Task IExplorerSidebarInteraction.DeleteFolderAsync(ICatalogFolderItem item)
        => OnFolderDelete.InvokeAsync(item);

    Task IExplorerSidebarInteraction.CreateFolderAsync(ICatalogFolderItem? parent)
        => CreateFolderAsync(parent);

    Task IExplorerSidebarInteraction.BeginFolderDragAsync(ICatalogFolderItem item)
    {
        ItemInteraction.BeginDrag(item);

        return Task.CompletedTask;
    }

    Task IExplorerSidebarInteraction.ToggleFolderSelectAsync(ICatalogFolderItem item)
        => ExplorerInteraction.ToggleSelectionAsync(item);
}
