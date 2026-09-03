namespace Forma.Features.Explorer.Content.Queries;

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
