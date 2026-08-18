namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Thumbnail grid view for explorer media resources.</summary>
public sealed partial class FormaExplorerThumbnailsView : ComponentBase
{
    /// <summary>The collection of media resources to display.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo> Resources { get; set; } = [];

    [CascadingParameter] private ExplorerContext Context { get; set; } = default!;

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerThumbnailsView)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
