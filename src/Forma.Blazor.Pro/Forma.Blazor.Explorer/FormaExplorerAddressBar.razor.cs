namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Navigation;

using Microsoft.AspNetCore.Components;

/// <summary>The explorer's address bar: navigation buttons, the folder path, and a search box that takes the row over while it is in use.</summary>
public sealed partial class FormaExplorerAddressBar : FormaComponentBase
{
    /// <summary>Whether the search stands as a glyph until pressed, where the row has no room for the box.</summary>
    [Parameter] public bool CollapsesSearch { get; set; }

    [CascadingParameter] private IExplorerContentInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationView Location { get; set; } = default!;

    [CascadingParameter] private IExplorerLocationInteraction LocationInteraction { get; set; } = default!;

    [CascadingParameter] private IExplorerSearchView Search { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Interaction is null || Location is null || LocationInteraction is null || Search is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerAddressBar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }
}
