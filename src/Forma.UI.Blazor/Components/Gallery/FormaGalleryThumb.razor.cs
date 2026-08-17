namespace Forma.UI.Components.Gallery;

using Forma.Framework.Resources;

using Microsoft.AspNetCore.Components;

/// <summary>Renders a media item as an image thumbnail, or as the glyph standing in for its kind.</summary>
public sealed partial class FormaGalleryThumb : ComponentBase
{
    /// <summary>The media item to depict.</summary>
    [Parameter, EditorRequired] public ResourceInfo Entry { get; set; } = default!;

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
        => Entry.Kind switch
        {
            ResourceKind.Video => "fa-film",
            ResourceKind.Audio => "fa-music",
            _ => "fa-file-lines"
        };

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Entry);
}
