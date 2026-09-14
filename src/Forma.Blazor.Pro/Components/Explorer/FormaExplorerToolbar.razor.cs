namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Buttons;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>Action toolbar for explorer operations, view switching, and sorting.</summary>
public sealed partial class FormaExplorerToolbar : FormaComponentBase
{
    [CascadingParameter] private IExplorerView View { get; set; } = default!;

    [CascadingParameter] private IExplorerInteraction Interaction { get; set; } = default!;

    [CascadingParameter] private IExplorerItemInteraction ItemInteraction { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaExplorer);

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
