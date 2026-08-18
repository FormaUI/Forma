namespace Forma.UI.Blazor.Components.Explorer;

using Forma.Framework.Resources;
using Forma.UI.Style.Glyphs;

using Microsoft.AspNetCore.Components;

/// <summary>Renders a media resource as an image thumbnail, or as the glyph standing in for what it is.</summary>
public sealed partial class FormaExplorerThumb : ComponentBase
{
    /// <summary>The media resource to depict.</summary>
    [Parameter, EditorRequired] public ResourceInfo Resource { get; set; } = default!;

    /// <summary>How large the thumbnail renders.</summary>
    [Parameter] public ExplorerThumbSize Size { get; set; } = ExplorerThumbSize.Tile;

    private string SizeClass
        => Size switch
        {
            ExplorerThumbSize.Row => "is-row",
            ExplorerThumbSize.Cell => "is-cell",
            _ => "is-tile"
        };

    private Glyph KindGlyph
        => Resource switch
        {
            FolderResourceInfo => Glyph.Folder,
            VideoResourceInfo => Glyph.Film,
            AudioResourceInfo => Glyph.Music,
            ArchiveResourceInfo => Glyph.FileZipper,
            _ => Glyph.FileLines
        };

    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Resource);
}
