namespace Forma.Framework.Catalog.Queries;

/// <summary>Specifies the field by which catalog items are ordered.</summary>
public enum CatalogSortField
{
    /// <summary>Sort by file name.</summary>
    Name,

    /// <summary>Sort by byte size.</summary>
    Size,

    /// <summary>Sort by kind.</summary>
    Kind,

    /// <summary>Sort by when the item was taken in.</summary>
    DateAdded,

    /// <summary>Sort by when the item last changed.</summary>
    DateModified
}
