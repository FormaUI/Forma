namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;

/// <summary>What the explorer's controls read off the view to decide which actions to offer.</summary>
internal static class ExplorerViewExtensions
{
    /// <summary>Whether the reader can enter and leave selection mode.</summary>
    public static bool CanLeaveSelectMode(this IExplorerView view)
        => !view.Options.SelectOnOpen;

    /// <summary>Whether the selection is kept across locations and offered for review, as a picker's is.</summary>
    public static bool ReviewsSelection(this IExplorerView view)
        => view.Options.SelectOnOpen;

    /// <summary>Whether the controls offer to choose the current folder.</summary>
    public static bool ShowsChooseFolderAction(this IExplorerView view)
        => view.ReviewsSelection() && view.CurrentFolder is { } folder && view.CanSelect(folder);

    /// <summary>Whether the controls offer a new folder.</summary>
    public static bool ShowsNewFolderAction(this IExplorerView view)
        => !view.ShowsSidebar
            && (view.CurrentFolder is { } folder
                ? folder.Permits(view.Item.ItemOptions, CatalogItemCapabilities.Create)
                : view.Item.ItemOptions.AllowedFolderCapabilities.HasFlag(CatalogItemCapabilities.Create));

    /// <summary>Whether anything selected may be deleted.</summary>
    public static bool ShowsDeleteAction(this IExplorerView view)
        => view.Selection.Items.Any(item => item.Permits(view.Item.ItemOptions, CatalogItemCapabilities.Delete));
}
