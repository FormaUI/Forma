namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.DataView;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Css;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The explorer's folder tree sidebar, the data view's tree over the folders, every folder row a drop target for the items and folders dragged onto it.</summary>
public sealed partial class FormaExplorerSidebar : FormaComponentBase
{
    /// <summary>The key the row standing for every item goes by, apart from every folder's.</summary>
    internal const string AllMediaKey = "forma-explorer:all-media";

    /// <summary>The folders the tree shows.</summary>
    [Parameter] public IReadOnlyList<ICatalogItem> Folders { get; set; } = [];

    /// <summary>The folders indexed as a tree.</summary>
    [Parameter] public CatalogFolderTree FolderTree { get; set; } = CatalogFolderTree.Empty;

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder is to be created under the folder given, where null is the top level.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem?> OnFolderCreate { get; set; }

    /// <summary>Which folder operations the sidebar offers.</summary>
    [Parameter] public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <summary>How many items the whole explorer holds.</summary>
    [Parameter] public int TotalCount { get; set; }

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    [CascadingParameter] private IDataView<ICatalogItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<ICatalogItem> Interaction { get; set; } = default!;

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private const double MinTreeHeight = 6;
    private const double MaxTreeHeight = 30;
    private const string DropAction = "explorer.drop";

    // The folder operations a row's menu offers.
    private const CatalogItemCapabilities FolderActions = CatalogItemCapabilities.Create | CatalogItemCapabilities.Rename | CatalogItemCapabilities.Delete;

    // The tree is one drop zone; each row that takes a drop is marked and named by its folder's key, the row standing for every item by none.
    private static readonly DropZoneOptions TreeDropZoneOptions = new() { Targets = "[data-drop-target]", DropEffect = DropEffect.Move };

    private DropZone _dropZone = default!;
    private DotNetObjectReference<FormaExplorerSidebar>? _selfReference;
    private ElementReference _tree;
    private bool _isDropZoneAttached;
    private double? _treeHeight;

    private string TitleId
        => $"forma-explorer-folders-{Context.RuntimeId:N}";

    // The row standing for every item, first among the top-level folders, with the count of the whole explorer.
    private CatalogFolderItem AllMedia
        => new() { Key = AllMediaKey, Name = L["All Media"], Count = TotalCount };

    private List<ICatalogFolderItem> ExpandableFolders
        => [.. FolderTree.GetExpandableFolders()];

    private bool HasExpandableFolders
        => ExpandableFolders.Count > 0;

    private bool IsFullyCollapsed
        => ExpandableFolders.All(folder => !View.IsOpen(folder));

    private string? TreeSizeStyle
        => _treeHeight is { } height
            ? new CssStyleBuilder().Add("block-size", CssLength.FromRem(height)).ToString()
            : null;

    // Every row is the same height, so the tree is sized from the folder count, the row for every item among them.
    private double MeasuredTreeHeight
        => Math.Clamp((Folders.Count + 1) * 2, MinTreeHeight, MaxTreeHeight);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (ItemInteraction is null || View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerSidebar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }

        _dropZone = new DropZone(Context.JS);
    }

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

    /// <summary>Takes a drop on the row the key names, where null is the row standing for every item.</summary>
    /// <param name="key">The folder's key.</param>
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

    private static bool IsFolder(ICatalogItem item)
        => item is ICatalogFolderItem;

    private bool IsCurrent(ICatalogItem item)
        => item.Key == AllMediaKey ? View.CurrentItem is null : View.CurrentItem?.Key == item.Key;

    // The operations the folder's menu offers, none for the row standing for every item.
    private CatalogItemCapabilities GetActions(ICatalogFolderItem folder)
        => folder.Key == AllMediaKey ? CatalogItemCapabilities.None : folder.GetCapabilities(ItemOptions) & FolderActions;

    // Every folder that holds folders opens, or every one closes where some are open.
    private async Task ToggleAllAsync()
    {
        var opens = IsFullyCollapsed;

        foreach (var folder in ExpandableFolders)
        {
            await (opens ? Interaction.OpenNodeAsync(folder) : Interaction.CloseNodeAsync(folder));
        }
    }

    private Task CreateFolderAsync(ICatalogFolderItem? parent)
        => OnFolderCreate.InvokeAsync(parent);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _dropZone.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
