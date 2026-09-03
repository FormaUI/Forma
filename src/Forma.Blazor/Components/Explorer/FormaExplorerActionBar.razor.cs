namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer;
using Forma.Features.Explorer.Items;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>Renders the explorer's operations for one item as an overflow menu.</summary>
public sealed partial class FormaExplorerActionBar : ComponentBase
{
    /// <summary>The item the actions run against.</summary>
    [Parameter, EditorRequired] public IExplorerItem Item { get; set; } = default!;

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

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
