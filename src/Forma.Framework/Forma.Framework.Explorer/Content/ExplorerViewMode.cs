namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog.Queries;

/// <summary>Specifies the layout an explorer presents its items in.</summary>
public enum ExplorerViewMode
{
    /// <summary>A grid of large thumbnail tiles.</summary>
    Thumbnails,

    /// <summary>A table of item details with sortable columns.</summary>
    Details,

    /// <summary>Names alone, flowing into columns.</summary>
    List
}
