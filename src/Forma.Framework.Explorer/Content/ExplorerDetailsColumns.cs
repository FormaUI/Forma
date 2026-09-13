namespace Forma.Framework.Explorer.Content;

/// <summary>Specifies which optional columns a details view renders.</summary>
[Flags]
public enum ExplorerDetailsColumns
{
    /// <summary>The name column alone.</summary>
    None = 0,

    /// <summary>The kind column.</summary>
    Kind = 1,

    /// <summary>The byte size column.</summary>
    Size = 2,

    /// <summary>The last changed column.</summary>
    Modified = 4,

    /// <summary>The taken in column.</summary>
    Added = 8
}
