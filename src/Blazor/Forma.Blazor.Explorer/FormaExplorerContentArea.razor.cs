namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.DataView;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Css;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Navigation;

using Microsoft.AspNetCore.Components;

/// <summary>The view switch over the items an explorer shows, with the data view's action for the next page and its footer.</summary>
public sealed partial class FormaExplorerContentArea : FormaComponentBase
{
    [CascadingParameter] private IExplorerLocationView Location { get; set; } = default!;

    [CascadingParameter] private IExplorerContentView View { get; set; } = default!;

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    [CascadingParameter] private IDataView<ICatalogItem> Content { get; set; } = default!;

    [CascadingParameter] private IExplorerView Explorer { get; set; } = default!;

    // The width a thumbnail packs at.
    private static readonly CssLength ThumbnailSize = CssLength.FromRem(8.125);

    // What a cell shows for a value the item does not have.
    private const string Placeholder = "—";

    private ICatalogSelectionView Selection
        => Explorer.Selection;

    private bool Shows(ExplorerDetailsColumns column)
        => View.DetailsColumns.HasFlag(column);

    private static string GetSortName(CatalogSortField field)
        => ExplorerDataViewSorts.GetName(field);

    private static DataViewSort<ICatalogItem> GetSort(CatalogSortField field)
        => ExplorerDataViewSorts.GetSort(field);

    private string FormatDate(DateTimeOffset? value)
        => value?.LocalDateTime.ToString("g", Context.CultureState.Culture) ?? Placeholder;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Location is null || View is null || Search is null || Content is null || Explorer is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerContentArea)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
