namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>One folder in the sidebar tree, rendering its own descendants.</summary>
public sealed partial class FormaExplorerFolderNode : ComponentBase
{
    /// <summary>The folder this node represents.</summary>
    [Parameter, EditorRequired] public FolderResourceInfo Folder { get; set; } = default!;

    /// <summary>How many levels below the root this node sits, driving its indent.</summary>
    [Parameter] public int Depth { get; set; }

    [CascadingParameter] private FormaExplorerSidebar Sidebar { get; set; } = default!;

    [CascadingParameter] private ExplorerContext Context { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Folder);
        ArgumentOutOfRangeException.ThrowIfNegative(Depth);

        if (Sidebar is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerFolderNode)} must be rendered inside a {nameof(FormaExplorerSidebar)}.");
        }
    }
}
