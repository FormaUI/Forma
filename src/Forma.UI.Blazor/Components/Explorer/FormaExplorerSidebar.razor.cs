namespace Forma.UI.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>Folders tree navigation sidebar with drag-and-drop target support for a media explorer.</summary>
public sealed partial class FormaExplorerSidebar : ComponentBase, IAsyncDisposable
{
    /// <summary>The currently active folder, or null for all media.</summary>
    [Parameter] public FolderResourceInfo? CurrentFolder { get; set; }

    /// <summary>List of available explorer folders.</summary>
    [Parameter] public IReadOnlyList<FolderResourceInfo> Folders { get; set; } = [];

    [CascadingParameter] private ExplorerContext Context { get; set; } = default!;

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
    [Parameter] public FormaExplorerFileOptions FileOptions { get; set; } = FormaExplorerFileOptions.Default;

    /// <summary>Total count of all resources across the explorer.</summary>
    [Parameter] public int TotalCount { get; set; }

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private const double MinTreeHeight = 6;

    private const double MaxTreeHeight = 30;

    // Splitter deltas arrive in pixels and the tree is sized in rem, so they are converted at the browser default.
    private const double RootFontSize = 16;

    private readonly HashSet<string> _collapsed = [];

    private readonly string _titleId = $"forma-explorer-folders-{Guid.NewGuid():N}";

    private IEnumerable<FolderResourceInfo> ExpandableFolders
        => Context.Folders.Expandable();

    private bool HasExpandableFolders
        => ExpandableFolders.Any();

    private bool IsFullyCollapsed
        => ExpandableFolders.All(folder => _collapsed.Contains(folder.Source));

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
        => _collapsed.Clear();

    private void CollapseAll()
    {
        foreach (var folder in ExpandableFolders)
        {
            _collapsed.Add(folder.Source);
        }
    }

    private double? _treeHeight;

    private double _sashOrigin;

    private double _sashStartHeight;

    private bool _isResizingTree;

    private ElementReference _sashElement;

    private IJSObjectReference? _sashModule;

    private string? TreeSizeStyle
        => _treeHeight is { } height
            ? string.Create(CultureInfo.InvariantCulture, $"block-size: {height}rem")
            : null;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _sashModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Forma.UI/Components/Explorer/FormaExplorer.razor.js");
            }
            catch (JSException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private async Task HandleSashPointerDownAsync(PointerEventArgs args)
    {
        _isResizingTree = true;
        _sashOrigin = args.ClientY;
        _sashStartHeight = _treeHeight ?? MeasuredTreeHeight;

        if (_sashModule is not null)
        {
            try
            {
                await _sashModule.InvokeVoidAsync("capturePointer", _sashElement, args.PointerId);
            }
            catch (JSException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private void HandleSashPointerMove(PointerEventArgs args)
    {
        if (_isResizingTree)
        {
            SetTreeHeight(_sashStartHeight + ((args.ClientY - _sashOrigin) / RootFontSize));
        }
    }

    private async Task HandleSashPointerUpAsync(PointerEventArgs args)
    {
        _isResizingTree = false;

        if (_sashModule is not null)
        {
            try
            {
                await _sashModule.InvokeVoidAsync("releasePointer", _sashElement, args.PointerId);
            }
            catch (JSException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    private void HandleSashKeyDown(KeyboardEventArgs args)
    {
        var current = _treeHeight ?? MeasuredTreeHeight;

        if (args.Key == "ArrowDown")
        {
            SetTreeHeight(current + 1);
        }
        else if (args.Key == "ArrowUp")
        {
            SetTreeHeight(current - 1);
        }
        else if (args.Key == "Home")
        {
            SetTreeHeight(MinTreeHeight);
        }
        else if (args.Key == "End")
        {
            SetTreeHeight(MaxTreeHeight);
        }
    }

    private void SetTreeHeight(double height)
        => _treeHeight = Math.Clamp(height, MinTreeHeight, MaxTreeHeight);

    // Every row is the same height, so the rendered tree can be sized from the folder count without measuring the DOM.
    private double MeasuredTreeHeight
        => Math.Clamp((Folders.Count + 1) * 2, MinTreeHeight, MaxTreeHeight);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_sashModule is not null)
        {
            try
            {
                await _sashModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }

    internal static void HandleDragOver(DragEventArgs e)
        => e.DataTransfer.DropEffect = "move";

    internal IReadOnlyList<FolderResourceInfo> ChildrenOf(string? parent)
        => Context.Folders.ChildrenOf(parent);

    internal bool IsCurrent(FolderResourceInfo folder)
        => CurrentFolder?.Source == folder.Source;

    // Folders start expanded so a nested tree is discoverable without a first click.
    internal bool IsExpanded(string folderSource)
        => !_collapsed.Contains(folderSource);

    internal void ToggleExpanded(string folderSource)
    {
        if (!_collapsed.Remove(folderSource))
        {
            _collapsed.Add(folderSource);
        }

        // The toolbar toggle reflects the whole tree, so the sidebar rerenders for a change made inside a node.
        StateHasChanged();
    }

    internal Task SelectFolderAsync(FolderResourceInfo? folder)
        => OnFolderSelected.InvokeAsync(folder);

    internal Task RenameFolderAsync(FolderResourceInfo folder)
        => OnFolderRename.InvokeAsync(folder);

    internal Task DeleteFolderAsync(FolderResourceInfo folder)
        => OnFolderDelete.InvokeAsync(folder);

    internal Task CreateFolderAsync(FolderResourceInfo? parent)
        => OnFolderCreate.InvokeAsync(parent);

    internal Task HandleDropOnFolderAsync(FolderResourceInfo? folder)
        => OnFolderDrop.InvokeAsync(folder);

    internal Task HandleFolderDragStartAsync(FolderResourceInfo folder)
        => OnFolderDragStart.InvokeAsync(folder);
}
