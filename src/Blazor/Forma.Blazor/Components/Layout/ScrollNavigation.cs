namespace Forma.Blazor.Components.Layout;

/// <summary>What moves a freeze pane's content.</summary>
public enum ScrollNavigation
{
    /// <summary>The browser's own scrollbar.</summary>
    Scrollbar,

    /// <summary>An arrow at each end in place of the scrollbar, each shown while the content can move that way.</summary>
    Arrows,
}
