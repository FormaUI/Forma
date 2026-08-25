namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;
using Forma.UI.Style.Glyphs;

using Microsoft.AspNetCore.Components;

/// <summary>Renders a media resource as an image thumbnail, or as the glyph standing in for what it is.</summary>
public sealed partial class FormaExplorerThumbnail : ComponentBase
{
    /// <summary>The media resource to depict.</summary>
    [Parameter, EditorRequired] public ResourceInfo Resource { get; set; } = default!;

    /// <summary>How large the thumbnail renders.</summary>
    [Parameter] public ExplorerThumbnailSize Size { get; set; } = ExplorerThumbnailSize.Tile;

    private string SizeClass
        => Size switch
        {
            ExplorerThumbnailSize.Row => "is-row",
            ExplorerThumbnailSize.Cell => "is-cell",
            _ => "is-tile"
        };

    private Glyph KindGlyph
        => Resource switch
        {
            FolderResourceInfo => Glyph.Folder,
            ImageResourceInfo => Glyph.Image,
            VideoResourceInfo => Glyph.Film,
            AudioResourceInfo => Glyph.Music,
            ArchiveResourceInfo => Glyph.FileZipper,
            DocumentResourceInfo => Glyph.FileLines,

            // Anything the library does not recognize says so, rather than passing itself off as a document.
            _ => Glyph.File
        };

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Resource);
}
