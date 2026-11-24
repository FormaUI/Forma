namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Icons;
using Forma.Framework.Resources;
using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an item's thumbnail, or the glyph standing in for what it is.</summary>
/// <remarks>
/// The item's own <see cref="ICatalogItem.ThumbnailSource"/> draws for any kind, and an image without one draws its own source.
/// A video without one draws its first frame where the item offers View, the browser drawing it over the glyph, which
/// shows while the frame loads and where it can't be drawn; every other item draws its glyph. A thumbnail drawn for an item
/// that isn't an image carries its kind's glyph over it.
/// </remarks>
public sealed partial class FormaExplorerThumbnail : FormaComponentBase
{
    /// <summary>The item to depict.</summary>
    [Parameter, EditorRequired] public ICatalogItem Item { get; set; } = default!;

    /// <summary>How large the thumbnail renders.</summary>
    [Parameter] public ExplorerThumbnailSize Size { get; set; } = ExplorerThumbnailSize.Tile;

    private string SizeClass
        => Size switch
        {
            ExplorerThumbnailSize.Cell => "is-cell",
            _ => "is-tile"
        };

    // The still the item is drawn with and the video whose first frame it shows, each none where its kind doesn't allow the item's address.
    private string? StillSource { get; set; }

    private string? FrameSource { get; set; }

    private bool IsMarked
        => Item.Resource is not ImageResourceInfo;

    private Glyph KindGlyph
        => Item.Resource switch
        {
            FolderResourceInfo => Glyph.Folder,
            ImageResourceInfo => Glyph.Image,
            VideoResourceInfo => Glyph.Video,
            AudioResourceInfo => Glyph.Audio,
            ArchiveResourceInfo => Glyph.FileArchive,
            DocumentResourceInfo => Glyph.FileText,
            null when Item is ICatalogFolderItem => Glyph.Folder,

            // An unrecognized kind takes the generic file glyph.
            _ => Glyph.File
        };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Item);

        StillSource = Context.GetAllowedAddress(
            !string.IsNullOrWhiteSpace(Item.ThumbnailSource) ? Item.ThumbnailSource : (Item.Resource as ImageResourceInfo)?.Source,
            AddressKind.ImageSource);

        FrameSource = Item.Resource is VideoResourceInfo video && Item.Capabilities.HasFlag(CatalogItemCapabilities.View)
            ? Context.GetAllowedAddress(video.Source, AddressKind.VideoSource)
            : null;
    }
}
