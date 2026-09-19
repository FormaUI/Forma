namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Css;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Navigation;
using Forma.Framework.Explorer.Selection;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>Folders tree navigation sidebar with drag-and-drop target support for the explorer.</summary>
public sealed partial class FormaExplorerSidebar : FormaComponentBase, IExplorerSidebarView, IExplorerSidebarInteraction
{
    /// <summary>The currently active folder, or null for every item.</summary>
    [Parameter] public ICatalogFolderItem? CurrentFolder { get; set; }

    /// <summary>List of available explorer folders.</summary>
    [Parameter] public IReadOnlyList<ICatalogItem> Folders { get; set; } = [];

    /// <summary>Every known folder indexed, so a node does not rescan the list to find its children.</summary>
    [Parameter] public CatalogFolderTree FolderTree { get; set; } = CatalogFolderTree.Empty;

    [CascadingParameter] private IExplorerView Explorer { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction ExplorerInteraction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder is selected.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem?> OnFolderSelected { get; set; }

    /// <summary>Raised when a folder is to be created, under the folder given or at the root when none is.</summary>
    [Parameter] public EventCallback<ICatalogFolderItem?> OnFolderCreate { get; set; }

    /// <summary>Which folder operations the sidebar offers.</summary>
    [Parameter] public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <summary>How many items the whole explorer holds.</summary>
    [Parameter] public int TotalCount { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private const double MinTreeHeight = 6;

    private const double MaxTreeHeight = 30;

    private readonly HashSet<string> _collapsedFolderKeys = [];

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

    // Every row is the same height, so the rendered tree can be sized from the folder count without measuring the DOM.
    private double MeasuredTreeHeight
        => Math.Clamp((Folders.Count + 1) * 2, MinTreeHeight, MaxTreeHeight);

    internal static void HandleDragOver(DragEventArgs args)
        => args.DataTransfer.DropEffect = "move";

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

    // Folders start expanded so a nested tree is discoverable without a first click.
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

    Task IExplorerSidebarInteraction.DropOnFolderAsync(ICatalogFolderItem? item)
        => ItemInteraction.DropOnItemAsync(item);

    Task IExplorerSidebarInteraction.BeginFolderDragAsync(ICatalogFolderItem item)
    {
        ItemInteraction.BeginDrag(item);

        return Task.CompletedTask;
    }

    Task IExplorerSidebarInteraction.ToggleFolderSelectAsync(ICatalogFolderItem item)
        => ExplorerInteraction.ToggleSelectionAsync(item);
}
