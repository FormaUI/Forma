namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

/// <summary>Reads what an explorer lists about a resource.</summary>
internal static class ExplorerItemExtensions
{
    /// <summary>What the resource is, named for a reader in their own language.</summary>
    internal static string KindLabel(this IExplorerItem item, IStringLocale<FormaExplorer> locale)
        => item.Resource switch
        {
            FolderResourceInfo => locale["Folder"].Value,
            ImageResourceInfo => locale["Image"].Value,
            VideoResourceInfo => locale["Video"].Value,
            AudioResourceInfo => locale["Audio"].Value,
            ArchiveResourceInfo => locale["Archive"].Value,
            DocumentResourceInfo => locale["Document"].Value,
            FileResourceInfo => locale["File"].Value,
            _ => locale["Unknown"].Value,
        };

    /// <summary>The accessible name for selecting or deselecting the item.</summary>
    internal static string SelectLabel(this IExplorerItem item, IStringLocale<FormaExplorer> locale, IExplorerSelectionView selection)
        => selection.IsSelected(item)
            ? locale["Deselect {Name}", item.Name].Value
            : locale["Select {Name}", item.Name].Value;

    /// <summary>The accessible name for activating the item, reflecting the current mode.</summary>
    internal static string ActivateLabel(this IExplorerItem item, IStringLocale<FormaExplorer> locale, IExplorerSelectionView selection)
        => selection.IsSelectionModeActive
            ? item.SelectLabel(locale, selection)
            : locale["Preview {Name}", item.Name].Value;

    /// <summary>The accessible name for the item's actions menu.</summary>
    internal static string ActionsLabel(this IExplorerItem item, IStringLocale<FormaExplorer> locale)
        => locale["Actions for {Name}", item.Name].Value;
}
