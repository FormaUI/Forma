namespace Forma.UI.Components.Gallery;

using System.Globalization;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>Folders tree navigation sidebar with drag-and-drop target support for a media gallery.</summary>
public sealed partial class FormaGallerySidebar : ComponentBase, IAsyncDisposable
{
    /// <summary>The currently active folder, or null for all media.</summary>
    [Parameter] public GalleryFolderInfo? CurrentFolder { get; set; }

    /// <summary>List of available gallery folders.</summary>
    [Parameter] public IReadOnlyList<GalleryFolderInfo> Folders { get; set; } = [];

    /// <summary>Raised when a folder is deleted.</summary>
    [Parameter] public EventCallback<GalleryFolderInfo> OnFolderDelete { get; set; }

    /// <summary>Raised when a folder starts being dragged.</summary>
    [Parameter] public EventCallback<GalleryFolderInfo> OnFolderDragStart { get; set; }

    /// <summary>Raised when dropped items land on a folder target.</summary>
    [Parameter] public EventCallback<GalleryFolderInfo?> OnFolderDrop { get; set; }

    /// <summary>Raised when a folder rename is requested.</summary>
    [Parameter] public EventCallback<GalleryFolderInfo> OnFolderRename { get; set; }

    /// <summary>Raised when a folder is selected.</summary>
    [Parameter] public EventCallback<GalleryFolderInfo?> OnFolderSelected { get; set; }

    /// <summary>Raised when new folder creation is requested.</summary>
    [Parameter] public EventCallback OnNewFolder { get; set; }

    /// <summary>Which folder operations the sidebar offers.</summary>
    [Parameter] public FormaGalleryOptions Options { get; set; } = FormaGalleryOptions.Default;

    /// <summary>Total count of all items across the gallery.</summary>
    [Parameter] public int TotalCount { get; set; }

    [Inject] private IStringLocale<FormaGallery> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private const double MinTreeHeight = 6;

    private const double MaxTreeHeight = 30;

    // Splitter deltas arrive in pixels and the tree is sized in rem, so they are converted at the browser default.
    private const double RootFontSize = 16;

    private readonly HashSet<string> _collapsed = [];

    private readonly string _titleId = $"forma-gallery-folders-{Guid.NewGuid():N}";

    private IEnumerable<GalleryFolderInfo> ExpandableFolders
        => Folders.Where(folder => Folders.Any(child => child.ParentId == folder.Id));

    private bool HasExpandableFolders
        => ExpandableFolders.Any();

    private bool IsFullyCollapsed
        => ExpandableFolders.All(folder => _collapsed.Contains(folder.Id));

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
            _collapsed.Add(folder.Id);
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
                _sashModule = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Forma.UI/Components/Gallery/FormaGallery.razor.js");
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

    internal IReadOnlyList<GalleryFolderInfo> ChildrenOf(string? parentId)
        => [.. Folders.Where(folder => folder.ParentId == parentId)];

    internal bool IsCurrent(GalleryFolderInfo folder)
        => CurrentFolder?.Id == folder.Id;

    // Folders start expanded so a nested tree is discoverable without a first click.
    internal bool IsExpanded(string folderId)
        => !_collapsed.Contains(folderId);

    internal void ToggleExpanded(string folderId)
    {
        if (!_collapsed.Remove(folderId))
        {
            _collapsed.Add(folderId);
        }

        // The toolbar toggle reflects the whole tree, so the sidebar rerenders for a change made inside a node.
        StateHasChanged();
    }

    internal Task SelectFolderAsync(GalleryFolderInfo? folder)
        => OnFolderSelected.InvokeAsync(folder);

    internal Task RenameFolderAsync(GalleryFolderInfo folder)
        => OnFolderRename.InvokeAsync(folder);

    internal Task DeleteFolderAsync(GalleryFolderInfo folder)
        => OnFolderDelete.InvokeAsync(folder);

    internal Task HandleDropOnFolderAsync(GalleryFolderInfo? folder)
        => OnFolderDrop.InvokeAsync(folder);

    internal Task HandleFolderDragStartAsync(GalleryFolderInfo folder)
        => OnFolderDragStart.InvokeAsync(folder);
}
