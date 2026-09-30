namespace Forma.Framework.Explorer.Content.Queries;

using Forma.Framework.Catalog.Queries;

/// <summary>Specifies the field explorer items are gathered into groups by.</summary>
public enum ExplorerGroupField
{
    /// <summary>No grouping.</summary>
    None,

    /// <summary>By the first character of the name.</summary>
    Name,

    /// <summary>By size.</summary>
    Size,

    /// <summary>By kind.</summary>
    Kind,

    /// <summary>By how long ago the item was added.</summary>
    DateAdded,

    /// <summary>By how long ago the item last changed.</summary>
    DateModified
}
