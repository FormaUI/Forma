namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Items.Queries;

using Microsoft.AspNetCore.Components;

/// <summary>The items of a columns menu: each optional details column with a check on the ones shown, then whether the header is shown.</summary>
public sealed partial class FormaExplorerColumnsMenuItems : FormaComponentBase
{
    private static readonly ExplorerDetailsColumns[] Columns =
    [
        ExplorerDetailsColumns.Kind,
        ExplorerDetailsColumns.Size,
        ExplorerDetailsColumns.Modified,
        ExplorerDetailsColumns.Added,
    ];

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
            throw new InvalidOperationException($"{nameof(FormaExplorerColumnsMenuItems)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private string GetColumnLabel(ExplorerDetailsColumns column)
        => column switch
        {
            ExplorerDetailsColumns.Kind => L["Type"].Value,
            ExplorerDetailsColumns.Size => L["Size"].Value,
            ExplorerDetailsColumns.Modified => L["Modified"].Value,
            ExplorerDetailsColumns.Added => L["Added"].Value,
            _ => column.ToString(),
        };
}
