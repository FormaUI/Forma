namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Navigation;
using Forma.Features.Explorer;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Address bar and navigation controls for a media explorer.</summary>
public sealed partial class FormaExplorerAddressBar : ComponentBase
{
    [CascadingParameter] private IExplorerConfigurationView Configuration { get; set; } = default!;

    [CascadingParameter] private IExplorerContentInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationView Location { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationInteraction LocationInteraction { get; set; } = default!;

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    [CascadingParameter] private IExplorerSearchInteraction SearchInteraction { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Configuration is null || Interaction is null || Location is null || LocationInteraction is null
            || Search is null || SearchInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerAddressBar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private IReadOnlyList<BreadcrumbItem<FolderResourceInfo>> Items
    {
        get
        {
            var items = new List<BreadcrumbItem<FolderResourceInfo>>
            {
                new(L["All Media"].Value)
                {
                    IsCurrent = Location.CurrentFolder is null,
                    Children = ChildrenAsItems(null)
                }
            };

            foreach (var segment in Location.Breadcrumbs)
            {
                items.Add(new BreadcrumbItem<FolderResourceInfo>(segment.Name)
                {
                    Value = segment,
                    IsCurrent = Location.CurrentFolder?.Source == segment.Source,
                    Children = ChildrenAsItems(segment.Source)
                });
            }

            return items;
        }
    }

    private List<BreadcrumbItem<FolderResourceInfo>>? ChildrenAsItems(string? folderKey)
    {
        if (!Configuration.Options.ShowFolderMenu)
        {
            return null;
        }

        var children = Location.FolderTree.ChildrenOf(folderKey);

        return children.Count == 0
            ? null
            : [.. children.Select(child => new BreadcrumbItem<FolderResourceInfo>(child.Name) { Value = child })];
    }

    private string SearchPlaceholder
    {
        get
        {
            if (!string.IsNullOrEmpty(Search.SearchPlaceholder))
            {
                return Search.SearchPlaceholder;
            }

            if (Location.CurrentFolder is not null)
            {
                return L["Search {Name}...", Location.CurrentFolder.Name].Value;
            }

            return L["Search media..."].Value;
        }
    }

    private Task HandleSearchValueChangedAsync(string query)
        => SearchInteraction.SetSearchQueryAsync(query);
}
