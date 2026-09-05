namespace Forma.Framework.Explorer.Content.Queries;

/// <summary>Specifies the field explorer items are gathered into groups by.</summary>
public enum ExplorerGroupField
{
    /// <summary>No grouping.</summary>
    None,

    /// <summary>By the first character of the name: digits, then bands of letters.</summary>
    Name,

    /// <summary>By byte size band.</summary>
    Size,

    /// <summary>By resource kind.</summary>
    Kind,

    /// <summary>By how long ago the resource was taken in.</summary>
    DateAdded,

    /// <summary>By how long ago the resource last changed.</summary>
    DateModified
}
