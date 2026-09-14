namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Navigation;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Navigation;

using Microsoft.AspNetCore.Components;

/// <summary>The explorer's folder path as a breadcrumb, from the root through the current folder, each crumb opening a menu of its subfolders where the options allow.</summary>
public sealed partial class FormaExplorerBreadcrumb : FormaComponentBase
{
    [CascadingParameter] private IExplorerConfigurationView Configuration { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationView Location { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationInteraction LocationInteraction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Configuration is null || ItemInteraction is null || Location is null || LocationInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerBreadcrumb)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private IReadOnlyList<BreadcrumbItem<IExplorerFolderItem>> Items
    {
        get
        {
            var items = new List<BreadcrumbItem<IExplorerFolderItem>>
            {
                new(L["All Media"].Value)
                {
                    IsCurrent = Location.CurrentFolder is null,
                    Children = GetChildItems(null)
                }
            };

            foreach (var segment in Location.Breadcrumbs)
            {
                items.Add(new BreadcrumbItem<IExplorerFolderItem>(segment.Name)
                {
                    Value = segment,
                    IsCurrent = Location.CurrentFolder?.Key == segment.Key,
                    Children = GetChildItems(segment.Key)
                });
            }

            return items;
        }
    }

    private List<BreadcrumbItem<IExplorerFolderItem>>? GetChildItems(string? folderKey)
    {
        if (!Configuration.Options.ShowFolderMenu)
        {
            return null;
        }

        var children = Location.FolderTree.GetListedChildren(folderKey);

        return children.Count == 0
            ? null
            : [.. children.Select(child => new BreadcrumbItem<IExplorerFolderItem>(child.Name) { Value = child })];
    }
}
