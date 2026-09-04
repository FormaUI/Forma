namespace Forma.Blazor.Components.Explorer;

using Forma.Blazor.Components.Buttons;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Content.Queries;
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

    private static readonly ExplorerSortField[] SortFields = Enum.GetValues<ExplorerSortField>();

    private static readonly ExplorerSortDirection[] SortDirections = Enum.GetValues<ExplorerSortDirection>();

    private static readonly ButtonOptions SortTriggerOptions =
        new() { Variant = ButtonVariant.Ghost };

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (View is null || Interaction is null || ItemInteraction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaExplorerToolbar)} must be rendered inside a {nameof(FormaExplorer)}.");
        }
    }

    private int SelectedCount
        => View.Selection.Count;

    private bool CanLeaveSelectMode
        => !View.Options.SelectOnOpen;

    private bool ShowsNewFolderAction
        => !View.ShowsSidebar && CurrentFolderAcceptsNewFolders;

    private bool CurrentFolderAcceptsNewFolders
        => View.CurrentFolder is { } folder
            ? folder.Permits(View.Item.ItemOptions, ExplorerItemCapabilities.Create)
            : View.Item.ItemOptions.AllowedFolderCapabilities.HasFlag(ExplorerItemCapabilities.Create);

    private bool ShowsDeleteAction
        => View.Selection.Items.Any(item => item.Permits(View.Item.ItemOptions, ExplorerItemCapabilities.Delete));

    private string SelectionAnnouncement
        => SelectedCount > 0 ? L["{SelectedCount} selected", SelectedCount].Value : string.Empty;

    private Glyph DirectionGlyph
        => GlyphFor(View.SortField, View.SortDirection);

    // Names and kinds sort alphabetically, sizes and dates numerically, so the mark says which order is meant.
    private static Glyph GlyphFor(ExplorerSortField field, ExplorerSortDirection direction)
        => (field is ExplorerSortField.Name or ExplorerSortField.Kind, direction is ExplorerSortDirection.Ascending) switch
        {
            (true, true) => Glyph.SortAlphaAscending,
            (true, false) => Glyph.SortAlphaDescending,
            (false, true) => Glyph.SortNumericAscending,
            (false, false) => Glyph.SortNumericDescending,
        };

    private string SortFieldLabel(ExplorerSortField field)
        => field switch
        {
            ExplorerSortField.Name => L["Name"].Value,
            ExplorerSortField.Size => L["Size"].Value,
            ExplorerSortField.Kind => L["Type"].Value,
            ExplorerSortField.DateAdded => L["Date Added"].Value,
            ExplorerSortField.DateModified => L["Date Modified"].Value,
            _ => field.ToString(),
        };

    private string DirectionLabel(ExplorerSortDirection direction)
        => direction == ExplorerSortDirection.Ascending ? L["Ascending"].Value : L["Descending"].Value;
}
