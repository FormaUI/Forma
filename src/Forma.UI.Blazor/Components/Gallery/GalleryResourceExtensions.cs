namespace Forma.UI.Blazor.Components.Gallery;

using Forma.Framework.Localization;
using Forma.Framework.Resources;

/// <summary>Reads what a gallery lists about a resource.</summary>
internal static class GalleryResourceExtensions
{
    /// <summary>What the resource is, named for a reader in their own language.</summary>
    internal static string KindLabel(this ResourceInfo resource, IStringLocale<FormaGallery> localizer)
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
