namespace Forma.Framework.Explorer.Content;

/// <summary>Specifies the presentation mode for media in the explorer.</summary>
public enum ExplorerViewMode
{
    /// <summary>A responsive grid of large media preview tiles.</summary>
    Thumbnails,

    /// <summary>A detailed tabular list with sortable columns.</summary>
    Details,

    /// <summary>Names alone, flowing down the height and on into further columns.</summary>
    List
}
