namespace Forma.Features.Explorer;

/// <summary>Specifies the field by which explorer resources are ordered.</summary>
public enum ExplorerSortField
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

/// <summary>Specifies the sort direction for explorer resources.</summary>
public enum ExplorerSortDirection
{
    /// <summary>Ascending order (A-Z, oldest first, smallest first).</summary>
    Ascending,

    /// <summary>Descending order (Z-A, newest first, largest first).</summary>
    Descending
}
