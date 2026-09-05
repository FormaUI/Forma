namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>What leads an item's row: its checkbox while selecting, its actions otherwise.</summary>
public sealed partial class FormaExplorerItemLead : ComponentBase
{
    /// <summary>The item the lead belongs to.</summary>
    [Parameter, EditorRequired] public IExplorerItem Item { get; set; } = default!;

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

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
