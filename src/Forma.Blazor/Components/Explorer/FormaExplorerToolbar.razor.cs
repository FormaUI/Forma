namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Buttons;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Icons;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>Action toolbar for media explorer operations, view switching, and sorting.</summary>
public sealed partial class FormaExplorerToolbar : ComponentBase
{
    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    [Inject] private IStringLocale<FormaExplorer> L { get; set; } = default!;

    private static readonly ButtonOptions SortTriggerOptions =
        new() { Variant = ButtonVariant.Ghost };

    private static readonly ButtonOptions ColumnsTriggerOptions =
        new() { Variant = ButtonVariant.Ghost, IconOnly = true };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerToolbar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private Glyph DirectionGlyph
        => ExplorerSortPresentation.GetGlyph(View.SortField, View.SortDirection);
}
