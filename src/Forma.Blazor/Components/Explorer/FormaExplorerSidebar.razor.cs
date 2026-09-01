namespace Forma.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Features.Explorer;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>Folders tree navigation sidebar with drag-and-drop target support for a media explorer.</summary>
public sealed partial class FormaExplorerSidebar : ComponentBase, IExplorerSidebarView, IExplorerSidebarInteraction
{
    /// <summary>The currently active folder, or null for all media.</summary>
    [Parameter] public FolderResourceInfo? CurrentFolder { get; set; }

    /// <summary>List of available explorer folders.</summary>
    [Parameter] public IReadOnlyList<FolderResourceInfo> Folders { get; set; } = [];

    /// <summary>Every known folder indexed, so a node does not rescan the list to find its children.</summary>
    [Parameter] public ExplorerFolderTree FolderTree { get; set; } = ExplorerFolderTree.Empty;

    [CascadingParameter] private IExplorerView Explorer { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction ExplorerInteraction { get; set; } = default!;

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<FolderResourceInfo> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder starts being dragged.</summary>
    [Parameter] public EventCallback<FolderResourceInfo> OnFolderDragStart { get; set; }

    /// <summary>Raised when dropped resources land on a folder target.</summary>
    [Parameter] public EventCallback<FolderResourceInfo?> OnFolderDrop { get; set; }

    /// <summary>Raised when a folder rename is requested.</summary>
    [Parameter] public EventCallback<FolderResourceInfo> OnFolderRename { get; set; }

    /// <summary>Raised when a folder is selected.</summary>
    [Parameter] public EventCallback<FolderResourceInfo?> OnFolderSelected { get; set; }

    /// <summary>Raised when a folder is to be created, under the folder given or at the root when none is.</summary>
    [Parameter] public EventCallback<FolderResourceInfo?> OnFolderCreate { get; set; }

    /// <summary>Which folder operations the sidebar offers.</summary>
    [Parameter] public ExplorerFileOptions FileOptions { get; set; } = ExplorerFileOptions.Default;

    /// <summary>Total count of all resources across the explorer.</summary>
    [Parameter] public int TotalCount { get; set; }

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private const double MinTreeHeight = 6;

    private const double MaxTreeHeight = 30;

    private readonly HashSet<string> _collapsedFolderSources = [];

    private readonly string _titleId = $"forma-explorer-folders-{Guid.NewGuid():N}";

    private IEnumerable<FolderResourceInfo> ExpandableFolders
        => FolderTree.Expandable();

    private bool HasExpandableFolders
        => ExpandableFolders.Any();

    private bool IsFullyCollapsed
        => ExpandableFolders.All(folder => _collapsedFolderSources.Contains(folder.Source));

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
        => _collapsedFolderSources.Clear();

    private void CollapseAll()
    {
        foreach (var folder in ExpandableFolders)
        {
            _collapsedFolderSources.Add(folder.Source);
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

    private IReadOnlyList<FolderResourceInfo> ChildrenOf(string? parent)
        => FolderTree.ChildrenOf(parent);

    private Task SelectFolderAsync(FolderResourceInfo? folder)
        => OnFolderSelected.InvokeAsync(folder);

    private Task CreateFolderAsync(FolderResourceInfo? parent)
        => OnFolderCreate.InvokeAsync(parent);

    private Task DropOnFolderAsync(FolderResourceInfo? folder)
        => OnFolderDrop.InvokeAsync(folder);

    IExplorerSelectionView IExplorerSidebarView.Selection => Explorer.Selection;

    IReadOnlyList<FolderResourceInfo> IExplorerSidebarView.ChildrenOf(string? parent)
        => ChildrenOf(parent);

    bool IExplorerSidebarView.IsCurrent(FolderResourceInfo folder)
        => CurrentFolder?.Source == folder.Source;

    // Folders start expanded so a nested tree is discoverable without a first click.
    bool IExplorerSidebarView.IsExpanded(string folderSource)
        => !_collapsedFolderSources.Contains(folderSource);

    void IExplorerSidebarInteraction.ToggleExpanded(string folderSource)
    {
        if (!_collapsedFolderSources.Remove(folderSource))
        {
            _collapsedFolderSources.Add(folderSource);
        }

        // The toolbar toggle reflects the whole tree, so the sidebar rerenders for a change made inside a node.
        StateHasChanged();
    }

    Task IExplorerSidebarInteraction.SelectFolderAsync(FolderResourceInfo? folder)
        => SelectFolderAsync(folder);

    Task IExplorerSidebarInteraction.RenameFolderAsync(FolderResourceInfo folder)
        => OnFolderRename.InvokeAsync(folder);

    Task IExplorerSidebarInteraction.DeleteFolderAsync(FolderResourceInfo folder)
        => OnFolderDelete.InvokeAsync(folder);

    Task IExplorerSidebarInteraction.CreateFolderAsync(FolderResourceInfo? parent)
        => CreateFolderAsync(parent);

    Task IExplorerSidebarInteraction.DropOnFolderAsync(FolderResourceInfo? folder)
        => DropOnFolderAsync(folder);

    Task IExplorerSidebarInteraction.BeginFolderDragAsync(FolderResourceInfo folder)
        => OnFolderDragStart.InvokeAsync(folder);

    Task IExplorerSidebarInteraction.ToggleFolderSelectAsync(FolderResourceInfo folder)
        => ExplorerInteraction.ToggleFolderSelectAsync(folder);
}
