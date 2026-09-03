namespace Forma.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Features.Explorer;
using Forma.Features.Explorer.Items;
using Forma.Features.Explorer.Navigation;
using Forma.Features.Explorer.Selection;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>Folders tree navigation sidebar with drag-and-drop target support for a media explorer.</summary>
public sealed partial class FormaExplorerSidebar : ComponentBase, IExplorerSidebarView, IExplorerSidebarInteraction
{
    /// <summary>The currently active folder, or null for all media.</summary>
    [Parameter] public IExplorerFolderItem? CurrentFolder { get; set; }

    /// <summary>List of available explorer folders.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem> Folders { get; set; } = [];

    /// <summary>Every known folder indexed, so a node does not rescan the list to find its children.</summary>
    [Parameter] public ExplorerFolderTree FolderTree { get; set; } = ExplorerFolderTree.Empty;

    [CascadingParameter] private IExplorerView Explorer { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction ExplorerInteraction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<IExplorerFolderItem> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder is selected.</summary>
    [Parameter] public EventCallback<IExplorerFolderItem?> OnFolderSelected { get; set; }

    /// <summary>Raised when a folder is to be created, under the folder given or at the root when none is.</summary>
    [Parameter] public EventCallback<IExplorerFolderItem?> OnFolderCreate { get; set; }

    /// <summary>Which folder operations the sidebar offers.</summary>
    [Parameter] public ExplorerItemOptions ItemOptions { get; set; } = ExplorerItemOptions.Default;

    /// <summary>Total count of all resources across the explorer.</summary>
    [Parameter] public int TotalCount { get; set; }

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private const double MinTreeHeight = 6;

    private const double MaxTreeHeight = 30;

    private readonly HashSet<string> _collapsedFolderKeys = [];

    private readonly string _titleId = $"forma-explorer-folders-{Guid.NewGuid():N}";

    private IEnumerable<string> ExpandableFolderKeys
        => FolderTree.Expandable().Select(item => item.Key);

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
            ? string.Create(CultureInfo.InvariantCulture, $"block-size: {height}rem")
            : null;

    // Every row is the same height, so the rendered tree can be sized from the folder count without measuring the DOM.
    private double MeasuredTreeHeight
        => Math.Clamp((Folders.Count + 1) * 2, MinTreeHeight, MaxTreeHeight);

    internal static void HandleDragOver(DragEventArgs args)
        => args.DataTransfer.DropEffect = "move";

    private IReadOnlyList<IExplorerFolderItem> ChildrenOf(string? parent)
        => FolderTree.ListedChildrenOf(parent);

    private Task SelectFolderAsync(IExplorerFolderItem? folder)
        => OnFolderSelected.InvokeAsync(folder);

    private Task CreateFolderAsync(IExplorerFolderItem? parent)
        => OnFolderCreate.InvokeAsync(parent);

    IExplorerSelectionView IExplorerSidebarView.Selection => Explorer.Selection;

    IReadOnlyList<IExplorerFolderItem> IExplorerSidebarView.ChildrenOf(string? parent)
        => ChildrenOf(parent);

    bool IExplorerSidebarView.IsCurrent(IExplorerFolderItem item)
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

    Task IExplorerSidebarInteraction.SelectFolderAsync(IExplorerFolderItem? item)
        => SelectFolderAsync(item);

    Task IExplorerSidebarInteraction.RenameFolderAsync(IExplorerFolderItem item)
    {
        ItemInteraction.RequestRename(item);

        return Task.CompletedTask;
    }

    Task IExplorerSidebarInteraction.DeleteFolderAsync(IExplorerFolderItem item)
        => OnFolderDelete.InvokeAsync(item);

    Task IExplorerSidebarInteraction.CreateFolderAsync(IExplorerFolderItem? parent)
        => CreateFolderAsync(parent);

    Task IExplorerSidebarInteraction.DropOnFolderAsync(IExplorerFolderItem? item)
        => ItemInteraction.DropOnItemAsync(item);

    Task IExplorerSidebarInteraction.BeginFolderDragAsync(IExplorerFolderItem item)
    {
        ItemInteraction.BeginDrag(item);

        return Task.CompletedTask;
    }

    Task IExplorerSidebarInteraction.ToggleFolderSelectAsync(IExplorerFolderItem item)
        => ExplorerInteraction.ToggleSelectionAsync(item);
}
