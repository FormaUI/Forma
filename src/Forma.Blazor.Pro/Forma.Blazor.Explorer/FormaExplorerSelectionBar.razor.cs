namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;

using Microsoft.AspNetCore.Components;

/// <summary>The bar shown while items are selected: delete and clear on a command bar's track, the count and the way out of selection mode at its end.</summary>
public sealed partial class FormaExplorerSelectionBar : FormaComponentBase
{
    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerSelectionBar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
