namespace Forma.Blazor.Components.Bars;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Css;
using Forma.Framework.Toolbar;

using Microsoft.AspNetCore.Components;

/// <summary>A search field and a row of toolbar items.</summary>
public sealed partial class FormaSearchToolbar : FormaElementBase
{
    /// <summary>The accessible name of the toolbar.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The items shown beside the search field.</summary>
    [Parameter] public IReadOnlyList<ToolbarItem> Items { get; set; } = [];

    /// <summary>The callback invoked with the command an item raised.</summary>
    [Parameter] public EventCallback<string> OnCommand { get; set; }

    /// <summary>The callback invoked with the search text once typing settles.</summary>
    [Parameter, EditorRequired] public EventCallback<string> OnSearch { get; set; }

    /// <summary>The options of the search field.</summary>
    [Parameter, EditorRequired] public SearchBoxOptions Search { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Search);
        ArgumentException.ThrowIfNullOrWhiteSpace(Search.Placeholder, nameof(Search));

        if (!OnSearch.HasDelegate)
        {
            throw new ArgumentException("A search toolbar must handle OnSearch; use FormaToolbar for a row of actions alone.", nameof(OnSearch));
        }

        if (Items.Count > 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        }
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-search-toolbar");
}
