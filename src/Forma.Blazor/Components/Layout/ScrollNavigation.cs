namespace Forma.Blazor.Components.Layout;

/// <summary>What moves a freeze pane's content.</summary>
public enum ScrollNavigation
{
    /// <summary>The browser's own scrollbar.</summary>
    Scrollbar,

    /// <summary>An arrow at each end, each shown only while the content can move that way, with the scrollbar hidden.</summary>
    Arrows,
}
