namespace Forma.Blazor.Components.Explorer;

/// <summary>The shape the explorer's header takes for the width it is given.</summary>
internal enum ExplorerHeaderLayout
{
    /// <summary>An address bar over a toolbar.</summary>
    Wide,

    /// <summary>An address bar over a toolbar, the search standing as a glyph until pressed.</summary>
    Medium,

    /// <summary>One row: the breadcrumb, a search glyph and a menu holding the rest.</summary>
    Compact,
}
