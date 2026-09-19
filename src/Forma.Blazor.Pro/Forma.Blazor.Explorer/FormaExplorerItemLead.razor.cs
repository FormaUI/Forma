namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;

using Microsoft.AspNetCore.Components;

/// <summary>What leads an item's row: its checkbox while selecting, its actions otherwise.</summary>
public sealed partial class FormaExplorerItemLead : FormaComponentBase
{
    /// <summary>The item the lead belongs to.</summary>
    [Parameter, EditorRequired] public ICatalogItem Item { get; set; } = default!;

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
            throw new InvalidOperationException($"{nameof(FormaExplorerItemLead)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Item);
}
