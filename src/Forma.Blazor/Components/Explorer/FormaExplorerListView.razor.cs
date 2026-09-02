namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Compact list presentation view for explorer media resources.</summary>
public sealed partial class FormaExplorerListView : ComponentBase
{
    /// <summary>The collection of items to display.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem> Items { get; set; } = [];

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerListView)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
