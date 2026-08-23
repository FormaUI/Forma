namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Renders the explorer's operations for one media resource as an overflow menu.</summary>
public sealed partial class FormaExplorerActionBar : ComponentBase
{
    /// <summary>The media resource the actions run against.</summary>
    [Parameter, EditorRequired] public ResourceInfo Resource { get; set; } = default!;

    [CascadingParameter] private ExplorerContext Context { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Resource);

        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerActionBar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
