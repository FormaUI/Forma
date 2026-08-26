namespace Forma.UI.Features.Explorer;

/// <summary>Specifies the presentation mode for media in the explorer.</summary>
public enum ExplorerViewMode
{
    /// <summary>A responsive grid of large media preview tiles.</summary>
    Thumbnails,

    /// <summary>A detailed tabular list with sortable columns.</summary>
    Details,

    /// <summary>A compact multi-column list view.</summary>
    List
}

/// <summary>Specifies whether an explorer appends pages as the reader scrolls, and how.</summary>
public enum ExplorerInfiniteScrollMode
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
public enum ExplorerDetailsColumns
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

/// <summary>Specifies how large a media thumbnail renders.</summary>
public enum ExplorerThumbnailSize
{
    /// <summary>Fills a grid tile.</summary>
    Tile,

    /// <summary>Sized for a list row.</summary>
    Row,

    /// <summary>Sized for a table cell.</summary>
    Cell
}
