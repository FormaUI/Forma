namespace Forma.UI.Components.Gallery;

/// <summary>Specifies the presentation mode for media in the gallery.</summary>
public enum GalleryViewMode
{
    /// <summary>A responsive grid of large media preview tiles.</summary>
    Thumbnails,

    /// <summary>A detailed tabular list with sortable columns.</summary>
    Details,

    /// <summary>A compact multi-column list view.</summary>
    List
}

/// <summary>Specifies the field by which gallery items are ordered.</summary>
public enum GallerySortField
{
    /// <summary>Sort by file name.</summary>
    Name,

    /// <summary>Sort by byte size.</summary>
    Size,

    /// <summary>Sort by resource kind/type.</summary>
    Kind,

    /// <summary>Sort by when the resource was taken in.</summary>
    DateAdded,

    /// <summary>Sort by when the resource last changed.</summary>
    DateModified
}

/// <summary>Specifies whether a gallery appends pages as the reader scrolls, and how.</summary>
public enum GalleryInfiniteScrollMode
{
    /// <summary>The collection is paged, and the reader moves between pages.</summary>
    Off,

    /// <summary>A button at the end of the list loads each further page.</summary>
    Manual,

    /// <summary>Scrolling to the end loads the next page on its own, showing a spinner while it arrives.</summary>
    Automatic
}

/// <summary>Specifies which optional columns a details view renders.</summary>
[Flags]
public enum GalleryDetailsColumns
{
    /// <summary>The name column alone.</summary>
    None = 0,

    /// <summary>The resource kind column.</summary>
    Kind = 1,

    /// <summary>The byte size column.</summary>
    Size = 2,

    /// <summary>The last changed column.</summary>
    Modified = 4,

    /// <summary>The taken in column.</summary>
    Added = 8
}

/// <summary>Specifies the sort direction for gallery items.</summary>
public enum GallerySortDirection
{
    /// <summary>Ascending order (A-Z, oldest first, smallest first).</summary>
    Ascending,

    /// <summary>Descending order (Z-A, newest first, largest first).</summary>
    Descending
}

/// <summary>Specifies how a view positions its item action buttons.</summary>
public enum GalleryActionBarVariant
{
    /// <summary>Placed in the flow of a row.</summary>
    Inline,

    /// <summary>Floated over a thumbnail.</summary>
    Overlay
}

/// <summary>Specifies how large a media thumbnail renders.</summary>
public enum GalleryThumbSize
{
    /// <summary>Fills a grid tile.</summary>
    Tile,

    /// <summary>Sized for a list row.</summary>
    Row,

    /// <summary>Sized for a table cell.</summary>
    Cell
}
