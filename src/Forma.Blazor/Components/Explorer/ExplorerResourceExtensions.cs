namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

/// <summary>Reads what an explorer lists about a resource.</summary>
internal static class ExplorerResourceExtensions
{
    /// <summary>What the resource is, named for a reader in their own language.</summary>
    internal static string KindLabel(this ResourceInfo resource, IStringLocale<FormaExplorer> locale)
        => resource switch
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

    /// <summary>The accessible name for selecting or deselecting the resource.</summary>
    internal static string SelectLabel(this ResourceInfo resource, IStringLocale<FormaExplorer> locale, IExplorerSelectionView selection)
        => selection.IsSelected(resource)
            ? locale["Deselect {Name}", ("Name", resource.Name)].Value
            : locale["Select {Name}", ("Name", resource.Name)].Value;

    /// <summary>The accessible name for activating the resource, reflecting the current mode.</summary>
    internal static string ActivateLabel(this ResourceInfo resource, IStringLocale<FormaExplorer> locale, IExplorerSelectionView selection)
        => selection.IsSelectMode
            ? resource.SelectLabel(locale, selection)
            : locale["Preview {Name}", ("Name", resource.Name)].Value;

    /// <summary>The accessible name for the resource's actions menu.</summary>
    internal static string ActionsLabel(this ResourceInfo resource, IStringLocale<FormaExplorer> locale)
        => locale["Actions for {Name}", ("Name", resource.Name)].Value;
}
