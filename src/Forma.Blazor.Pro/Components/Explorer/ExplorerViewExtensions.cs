namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Explorer;
using Forma.Framework.Explorer.Items;

/// <summary>What the explorer's controls read off the view to decide which actions to offer.</summary>
internal static class ExplorerViewExtensions
{
    /// <summary>Whether the reader can enter and leave selection mode, rather than the explorer opening in it.</summary>
    public static bool CanLeaveSelectMode(this IExplorerView view)
        => !view.Options.SelectOnOpen;

    /// <summary>Whether the controls offer a new folder: only without the sidebar, and only where the current folder permits one.</summary>
    public static bool ShowsNewFolderAction(this IExplorerView view)
        => !view.ShowsSidebar
            && (view.CurrentFolder is { } folder
                ? folder.Permits(view.Item.ItemOptions, ExplorerItemCapabilities.Create)
                : view.Item.ItemOptions.AllowedFolderCapabilities.HasFlag(ExplorerItemCapabilities.Create));

    /// <summary>Whether anything selected may be deleted.</summary>
    public static bool ShowsDeleteAction(this IExplorerView view)
        => view.Selection.Items.Any(item => item.Permits(view.Item.ItemOptions, ExplorerItemCapabilities.Delete));
}
