namespace Forma.Framework.Catalog.Queries;

/// <summary>The field catalog items are sorted by.</summary>
public enum CatalogSortField
{
    /// <summary>The item's name.</summary>
    Name,

    /// <summary>The item's size in bytes.</summary>
    Size,

    /// <summary>The item's kind.</summary>
    Kind,

    /// <summary>When the item was added.</summary>
    DateAdded,

    /// <summary>When the item was last modified.</summary>
    DateModified
}
