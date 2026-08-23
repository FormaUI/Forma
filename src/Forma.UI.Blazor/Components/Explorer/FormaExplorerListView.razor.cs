namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Compact list presentation view for explorer media resources.</summary>
public sealed partial class FormaExplorerListView : ComponentBase
{
    /// <summary>The collection of media resources to display.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo> Resources { get; set; } = [];

    [CascadingParameter] private ExplorerContext Context { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerListView)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
