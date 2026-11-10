namespace Forma.Blazor.Components.DataView;

/// <summary>How the next page of a paging data view arrives.</summary>
public enum DataViewAppendMode
{
    /// <summary>The paginator moves from one page to the next.</summary>
    Off,

    /// <summary>A Load More action after the items appends the next page to them.</summary>
    Manual,

    /// <summary>The Load More action appends the next page, and so does the reader reaching the end of the items.</summary>
    Automatic
}
