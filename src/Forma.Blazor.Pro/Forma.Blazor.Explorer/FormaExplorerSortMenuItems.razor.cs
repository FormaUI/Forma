namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items.Queries;

using Microsoft.AspNetCore.Components;

/// <summary>The items of a sort menu: every field with the current one checked, both directions with the current one checked, and every grouping with the current one checked.</summary>
public sealed partial class FormaExplorerSortMenuItems : FormaComponentBase
{
    private static readonly ExplorerSortField[] SortFields = Enum.GetValues<ExplorerSortField>();

    private static readonly ExplorerSortDirection[] SortDirections = Enum.GetValues<ExplorerSortDirection>();

    private static readonly ExplorerGroupField[] GroupFields = Enum.GetValues<ExplorerGroupField>();

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerSortMenuItems)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
