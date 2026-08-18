namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Localization;
using Forma.Framework.Resources;

/// <summary>Reads what an explorer lists about a resource.</summary>
internal static class ExplorerResourceExtensions
{
    /// <summary>What the resource is, named for a reader in their own language.</summary>
    internal static string KindLabel(this ResourceInfo resource, IStringLocale<FormaExplorer> localizer)
        => resource switch
        {
            FolderResourceInfo => localizer["Folder"].Value,
            ImageResourceInfo => localizer["Image"].Value,
            VideoResourceInfo => localizer["Video"].Value,
            AudioResourceInfo => localizer["Audio"].Value,
            ArchiveResourceInfo => localizer["Archive"].Value,
            DocumentResourceInfo => localizer["Document"].Value,
            FileResourceInfo => localizer["File"].Value,
            _ => localizer["Unknown"].Value,
        };
}
