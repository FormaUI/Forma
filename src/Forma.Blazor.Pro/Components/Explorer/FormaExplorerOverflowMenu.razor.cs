namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Navigation;

using Microsoft.AspNetCore.Components;

/// <summary>The menu holding everything the compact header has no row for: navigation, actions, view mode and sorting.</summary>
public sealed partial class FormaExplorerOverflowMenu : FormaComponentBase
{
    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationView Location { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationInteraction LocationInteraction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null || Location is null || LocationInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerOverflowMenu)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
