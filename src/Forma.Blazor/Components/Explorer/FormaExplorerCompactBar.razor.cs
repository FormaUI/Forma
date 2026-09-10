namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer.Content.Queries;

using Microsoft.AspNetCore.Components;

/// <summary>The explorer's header at a phone's width: the breadcrumb, a search glyph that opens the search across the row, and a menu holding the rest.</summary>
public sealed partial class FormaExplorerCompactBar : FormaComponentBase
{
    private bool _isSearching;

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    private bool IsSearching
        => _isSearching || !string.IsNullOrEmpty(Search.SearchQuery);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Search is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerCompactBar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
