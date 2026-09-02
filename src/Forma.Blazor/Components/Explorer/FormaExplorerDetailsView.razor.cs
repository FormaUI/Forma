namespace Forma.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Features.Explorer;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Tabular details view for explorer media resources with sortable column headers.</summary>
public sealed partial class FormaExplorerDetailsView : ComponentBase
{
    /// <summary>The collection of items to display.</summary>
    [Parameter] public IReadOnlyList<IExplorerItem> Items { get; set; } = [];

    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private const string Placeholder = "—";

    private bool ShowsKind
        => View.Options.DetailsColumns.HasFlag(ExplorerDetailsColumns.Kind);

    private bool ShowsSize
        => View.Options.DetailsColumns.HasFlag(ExplorerDetailsColumns.Size);

    private bool ShowsModified
        => View.Options.DetailsColumns.HasFlag(ExplorerDetailsColumns.Modified);

    private bool ShowsAdded
        => View.Options.DetailsColumns.HasFlag(ExplorerDetailsColumns.Added);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerDetailsView)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private static string DisplayDate(DateTimeOffset? value)
        => value?.LocalDateTime.ToString("g", CultureInfo.CurrentCulture) ?? Placeholder;

    private string SortState(ExplorerSortField field)
    {
        if (View.SortField != field)
        {
            return "none";
        }

        return View.SortDirection == ExplorerSortDirection.Ascending ? "ascending" : "descending";
    }
}
