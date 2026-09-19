namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;

using Microsoft.AspNetCore.Components;

/// <summary>Renders the explorer's operations for one item as an overflow menu.</summary>
public sealed partial class FormaExplorerActionBar : FormaComponentBase
{
    /// <summary>The item the actions run against.</summary>
    [Parameter, EditorRequired] public ICatalogItem Item { get; set; } = default!;

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    private IReadOnlyList<ExplorerItemAction> AvailableActions
    {
        get
        {
            var capabilities = Item.GetCapabilities(View.Item.ItemOptions);

            return [.. View.Item.Actions.Where(action => (capabilities & action.Requires) == action.Requires)];
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Item);

        if (View is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerActionBar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
