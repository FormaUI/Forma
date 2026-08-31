namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Renders the explorer's operations for one media resource as an overflow menu.</summary>
public sealed partial class FormaExplorerActionBar : ComponentBase
{
    /// <summary>The media resource the actions run against.</summary>
    [Parameter, EditorRequired] public ResourceInfo Resource { get; set; } = default!;

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Resource);

        if (View is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerActionBar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
