namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.DataView;
using Forma.Framework.Catalog;
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

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Location is null || View is null || Search is null || Content is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerContentArea)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
