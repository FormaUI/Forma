namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Renders a media resource as an image thumbnail, or as the glyph standing in for what it is.</summary>
public sealed partial class FormaGalleryThumb : ComponentBase
{
    /// <summary>The media resource to depict.</summary>
    [Parameter, EditorRequired] public ResourceInfo Resource { get; set; } = default!;

    /// <summary>How large the thumbnail renders.</summary>
    [Parameter] public GalleryThumbSize Size { get; set; } = GalleryThumbSize.Tile;

    private string SizeClass
        => Size switch
        {
            GalleryThumbSize.Row => "is-row",
            GalleryThumbSize.Cell => "is-cell",
            _ => "is-tile"
        };

    private string KindGlyph
        => Resource switch
        {
            FolderResourceInfo => "fa-folder",
            VideoResourceInfo => "fa-film",
            AudioResourceInfo => "fa-music",
            ArchiveResourceInfo => "fa-file-zipper",
            _ => "fa-file-lines"
        };

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Resource);
}
