namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer.Items;
using Forma.Features.Explorer.Navigation;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>One folder in the sidebar tree, rendering its own descendants.</summary>
public sealed partial class FormaExplorerFolderNode : ComponentBase
{
    /// <summary>The folder item this node represents.</summary>
    [Parameter, EditorRequired] public IExplorerFolderItem Item { get; set; } = default!;

    /// <summary>How many levels below the root this node sits, driving its indent.</summary>
    [Parameter] public int Depth { get; set; }

    [CascadingParameter] private IExplorerSidebarView Sidebar { get; set; } = default!;

    [CascadingParameter] private IExplorerSidebarInteraction Interaction { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Item);
        ArgumentOutOfRangeException.ThrowIfNegative(Depth);

        if (Sidebar is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerFolderNode)} must be rendered inside a {nameof(FormaExplorerSidebar)}.");
        }
    }
}
