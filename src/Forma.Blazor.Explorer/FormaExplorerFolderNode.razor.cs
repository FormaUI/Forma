namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Navigation;

using Microsoft.AspNetCore.Components;

/// <summary>One folder in the sidebar tree, rendering its own descendants.</summary>
public sealed partial class FormaExplorerFolderNode : FormaComponentBase
{
    /// <summary>The folder item this node represents.</summary>
    [Parameter, EditorRequired] public IExplorerFolderItem Item { get; set; } = default!;

    /// <summary>How many levels below the root this node sits, driving its indent.</summary>
    [Parameter] public int Depth { get; set; }

    [CascadingParameter] private IExplorerSidebarView Sidebar { get; set; } = default!;

    [CascadingParameter] private IExplorerSidebarInteraction Interaction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

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
