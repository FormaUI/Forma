namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Localization;
using Forma.Framework.Resources;
using Forma.UI.Features.Explorer;

using Microsoft.AspNetCore.Components;

/// <summary>One folder in the sidebar tree, rendering its own descendants.</summary>
public sealed partial class FormaExplorerFolderNode : ComponentBase
{
    /// <summary>The folder this node represents.</summary>
    [Parameter, EditorRequired] public FolderResourceInfo Folder { get; set; } = default!;

    /// <summary>How many levels below the root this node sits, driving its indent.</summary>
    [Parameter] public int Depth { get; set; }

    [CascadingParameter] private IExplorerSidebarView Sidebar { get; set; } = default!;

    [CascadingParameter] private IExplorerSidebarInteraction Interaction { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Folder);
        ArgumentOutOfRangeException.ThrowIfNegative(Depth);

        if (Sidebar is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerFolderNode)} must be rendered inside a {nameof(FormaExplorerSidebar)}.");
        }
    }
}
