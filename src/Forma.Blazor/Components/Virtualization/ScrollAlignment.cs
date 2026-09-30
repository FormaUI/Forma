namespace Forma.Blazor.Components.Virtualization;

/// <summary>Where a scroll brings an item: by the least scroll that shows it, or to the top of the viewport.</summary>
public enum ScrollAlignment
{
    /// <summary>By the least scroll that shows the item, which leaves an item already in view where it stands.</summary>
    Nearest,

    /// <summary>With the item at the top of the viewport.</summary>
    Start,
}
