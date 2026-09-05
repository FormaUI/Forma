namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Navigation;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>The explorer's search box, bound to its query and named for the folder it searches.</summary>
public sealed partial class FormaExplorerSearchBox : ComponentBase
{
    private FormaSearchBox _searchBox = default!;

    /// <summary>Additional attributes applied to the search input.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [CascadingParameter] private IExplorerLocationView Location { get; set; } = default!;

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    [CascadingParameter] private IExplorerSearchInteraction SearchInteraction { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    /// <summary>Moves keyboard focus into the search input.</summary>
    public ValueTask FocusAsync()
        => _searchBox.Element.FocusAsync();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Location is null || Search is null || SearchInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerSearchBox)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private SearchBoxOptions Options
        => new() { Placeholder = Placeholder };

    private string Placeholder
    {
        get
        {
            if (!string.IsNullOrEmpty(Search.SearchPlaceholder))
            {
                return Search.SearchPlaceholder;
            }

            if (Location.CurrentFolder is not null)
            {
                return L["Search {Name}...", Location.CurrentFolder.Name].Value;
            }

            return L["Search media..."].Value;
        }
    }

    private Task HandleValueChangedAsync(string query)
        => SearchInteraction.SetSearchQueryAsync(query);
}
