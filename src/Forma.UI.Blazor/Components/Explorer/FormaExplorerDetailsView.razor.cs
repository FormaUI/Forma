namespace Forma.UI.Blazor.Components.Explorer;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Tabular details view for explorer media resources with sortable column headers.</summary>
public sealed partial class FormaExplorerDetailsView : ComponentBase
{
    /// <summary>The collection of media resources to display.</summary>
    [Parameter] public IReadOnlyList<ResourceInfo> Resources { get; set; } = [];

    [CascadingParameter] private ExplorerContext Context { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private const string Placeholder = "—";

    private bool ShowsKind
        => Context.Options.DetailsColumns.HasFlag(ExplorerDetailsColumns.Kind);

    private bool ShowsSize
        => Context.Options.DetailsColumns.HasFlag(ExplorerDetailsColumns.Size);

    private bool ShowsModified
        => Context.Options.DetailsColumns.HasFlag(ExplorerDetailsColumns.Modified);

    private bool ShowsAdded
        => Context.Options.DetailsColumns.HasFlag(ExplorerDetailsColumns.Added);

    protected override void OnInitialized()
    {
        if (Context is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerDetailsView)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private static string DisplayDate(DateTimeOffset? value)
        => value?.LocalDateTime.ToString("g", CultureInfo.CurrentCulture) ?? Placeholder;

    private string SortState(ExplorerSortField field)
    {
        if (Context.SortField != field)
        {
            return "none";
        }

        return Context.SortDirection == ExplorerSortDirection.Ascending ? "ascending" : "descending";
    }
}
