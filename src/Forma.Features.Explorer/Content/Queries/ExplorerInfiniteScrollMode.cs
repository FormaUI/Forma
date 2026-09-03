namespace Forma.Features.Explorer.Content.Queries;

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
