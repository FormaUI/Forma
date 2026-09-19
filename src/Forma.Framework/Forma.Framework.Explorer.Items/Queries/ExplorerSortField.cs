namespace Forma.Framework.Explorer.Items.Queries;

/// <summary>Specifies the field by which explorer items are ordered.</summary>
public enum ExplorerSortField
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
