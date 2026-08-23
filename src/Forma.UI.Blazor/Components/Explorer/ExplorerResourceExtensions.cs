namespace Forma.UI.Blazor.Components.Explorer;

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
}
