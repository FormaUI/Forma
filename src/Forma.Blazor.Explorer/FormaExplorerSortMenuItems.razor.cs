namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content.Queries;

using Microsoft.AspNetCore.Components;

/// <summary>The items of a sort menu: the sort fields, the sort directions and the groupings, each with the current one checked.</summary>
public sealed partial class FormaExplorerSortMenuItems : FormaComponentBase
{
    private static readonly CatalogSortField[] SortFields = Enum.GetValues<CatalogSortField>();

    private static readonly CatalogSortDirection[] SortDirections = Enum.GetValues<CatalogSortDirection>();

    private static readonly ExplorerGroupField[] GroupFields = Enum.GetValues<ExplorerGroupField>();

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerSortMenuItems)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
