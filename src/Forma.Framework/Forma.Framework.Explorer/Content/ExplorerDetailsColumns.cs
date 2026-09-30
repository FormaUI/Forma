namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog.Queries;

/// <summary>Specifies which optional columns a details view renders.</summary>
[Flags]
public enum ExplorerDetailsColumns
{
    /// <summary>The name column alone.</summary>
    None = 0,

    /// <summary>The kind column.</summary>
    Kind = 1,

    /// <summary>The size column.</summary>
    Size = 2,

    /// <summary>The date modified column.</summary>
    Modified = 4,

    /// <summary>The date added column.</summary>
    Added = 8
}
