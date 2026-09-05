namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer.Content;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>The heading over a group of items: its name, how many it holds, and a chevron that folds it away.</summary>
public sealed partial class FormaExplorerGroupHeading : ComponentBase
{
    /// <summary>The group the heading stands over.</summary>
    [Parameter, EditorRequired] public ExplorerItemGroup Group { get; set; } = default!;

    /// <summary>Whether the group's items are folded away.</summary>
    [Parameter] public bool Collapsed { get; set; }

    /// <summary>Raised when the heading is pressed to fold the group away or open it again.</summary>
    [Parameter] public EventCallback OnToggle { get; set; }

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Group);
}
