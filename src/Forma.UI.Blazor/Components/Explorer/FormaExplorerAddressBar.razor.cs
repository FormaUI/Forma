namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>Address bar and navigation controls for a media explorer.</summary>
public sealed partial class FormaExplorerAddressBar : ComponentBase
{
    /// <summary>Breadcrumb folder segments leading to the current folder.</summary>
    [Parameter] public IReadOnlyList<FolderResourceInfo> Breadcrumbs { get; set; } = [];

    /// <summary>Whether navigation backward is available in the history stack.</summary>
    [Parameter] public bool CanGoBack { get; set; }

    /// <summary>Whether navigation forward is available in the history stack.</summary>
    [Parameter] public bool CanGoForward { get; set; }

    /// <summary>Whether navigation up to a parent folder is available.</summary>
    [Parameter] public bool CanGoUp { get; set; }

    /// <summary>The currently active folder, or null for root.</summary>
    [Parameter] public FolderResourceInfo? CurrentFolder { get; set; }

    /// <summary>Every known folder, used to list a breadcrumb level's children.</summary>
    [Parameter] public IReadOnlyList<FolderResourceInfo> Folders { get; set; } = [];

    [CascadingParameter] private ExplorerContext Context { get; set; } = default!;

    /// <summary>Raised when the back button is clicked.</summary>
    [Parameter] public EventCallback OnBack { get; set; }

    /// <summary>Raised when dropped resources land on a breadcrumb folder target.</summary>
    [Parameter] public EventCallback<FolderResourceInfo?> OnBreadcrumbDrop { get; set; }

    /// <summary>Raised when a breadcrumb segment folder is clicked.</summary>
    [Parameter] public EventCallback<FolderResourceInfo?> OnFolderSelected { get; set; }

    /// <summary>Raised when the forward button is clicked.</summary>
    [Parameter] public EventCallback OnForward { get; set; }

    /// <summary>Raised when the refresh button is clicked.</summary>
    [Parameter] public EventCallback OnRefresh { get; set; }

    /// <summary>Raised when the search query changes.</summary>
    [Parameter] public EventCallback<string> OnSearchQueryChanged { get; set; }

    /// <summary>Raised when the up button is clicked.</summary>
    [Parameter] public EventCallback OnUp { get; set; }

    /// <summary>Custom placeholder for the search box.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Current active search query filter.</summary>
    [Parameter] public string SearchQuery { get; set; } = string.Empty;

    /// <summary>Whether each breadcrumb offers a menu of that folder's children.</summary>
    [Parameter] public bool ShowFolderMenu { get; set; } = true;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private const string RootLevelKey = "\u0000root";

    private string? _openLevelKey;

    private IReadOnlyList<BreadcrumbLevel> Levels
    {
        get
        {
            var levels = new List<BreadcrumbLevel> { new(RootLevelKey, null, L["All Media"].Value, null) };

            foreach (var segment in Breadcrumbs)
            {
                levels.Add(new BreadcrumbLevel(segment.Source, segment.Source, segment.Name, segment));
            }

            return levels;
        }
    }

    private IReadOnlyList<FolderResourceInfo> ChildrenOf(string? parent)
        => Context.Folders.ChildrenOf(parent);

    private void ToggleLevelMenu(string levelKey)
        => _openLevelKey = _openLevelKey == levelKey ? null : levelKey;

    private void CloseLevelMenu()
        => _openLevelKey = null;

    private async Task SelectFromMenuAsync(FolderResourceInfo folder)
    {
        _openLevelKey = null;

        await OnFolderSelected.InvokeAsync(folder);
    }

    private void HandleMenuKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            _openLevelKey = null;
        }
    }

    private sealed record BreadcrumbLevel(string Key, string? Parent, string Name, FolderResourceInfo? Folder);

    private string SearchPlaceholder
    {
        get
        {
            if (!string.IsNullOrEmpty(Placeholder))
            {
                return Placeholder;
            }

            if (CurrentFolder is not null)
            {
                return L["Search {name}...", ("name", CurrentFolder.Name)].Value;
            }

            return L["Search media..."].Value;
        }
    }

    private static void HandleDragOver(DragEventArgs e)
        => e.DataTransfer.DropEffect = "move";

    private async Task ClearSearch()
    {
        SearchQuery = string.Empty;

        await OnSearchQueryChanged.InvokeAsync(string.Empty);
    }

    private async Task HandleBreadcrumbDrop(FolderResourceInfo? folder)
        => await OnBreadcrumbDrop.InvokeAsync(folder);

    private async Task HandleSearchInput(ChangeEventArgs e)
    {
        SearchQuery = e.Value?.ToString() ?? string.Empty;

        await OnSearchQueryChanged.InvokeAsync(SearchQuery);
    }
}
