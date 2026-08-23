namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Blazor.Components.Navigation;

using Microsoft.AspNetCore.Components;

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

    private IReadOnlyList<BreadcrumbItem<FolderResourceInfo>> Items
    {
        get
        {
            var items = new List<BreadcrumbItem<FolderResourceInfo>>
            {
                new(L["All Media"].Value)
                {
                    IsCurrent = CurrentFolder is null,
                    Children = ChildrenAsItems(null)
                }
            };

            foreach (var segment in Breadcrumbs)
            {
                items.Add(new BreadcrumbItem<FolderResourceInfo>(segment.Name)
                {
                    Value = segment,
                    IsCurrent = CurrentFolder?.Source == segment.Source,
                    Children = ChildrenAsItems(segment.Source)
                });
            }

            return items;
        }
    }

    private List<BreadcrumbItem<FolderResourceInfo>>? ChildrenAsItems(string? folderKey)
    {
        if (!ShowFolderMenu)
        {
            return null;
        }

        var children = ChildrenOf(folderKey);

        return children.Count == 0
            ? null
            : [.. children.Select(child => new BreadcrumbItem<FolderResourceInfo>(child.Name) { Value = child })];
    }

    private IReadOnlyList<FolderResourceInfo> ChildrenOf(string? parent)
        => Context.Folders.ChildrenOf(parent);

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
                return L["Search {Name}...", ("Name", CurrentFolder.Name)].Value;
            }

            return L["Search media..."].Value;
        }
    }

    private async Task HandleSearchValueChangedAsync(string query)
    {
        SearchQuery = query;

        await OnSearchQueryChanged.InvokeAsync(SearchQuery);
    }
}
