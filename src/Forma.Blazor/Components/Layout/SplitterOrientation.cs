namespace Forma.Blazor.Components.Layout;

/// <summary>Which way a splitter's own bar runs, and so which axis it resizes along.</summary>
public enum SplitterOrientation
{
    /// <summary>A vertical bar between side-by-side panes, dragged along the inline axis.</summary>
    Vertical,

    /// <summary>A horizontal bar between stacked panes, dragged along the block axis.</summary>
    Horizontal
}
