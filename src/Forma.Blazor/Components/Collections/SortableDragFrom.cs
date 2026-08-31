namespace Forma.Blazor.Components.Collections;

/// <summary>Where on a row a drag may start.</summary>
public enum SortableDragFrom
{
    /// <summary>Only the drag handle starts a drag.</summary>
    Handle,

    /// <summary>Anywhere on the row starts a drag, apart from interactive elements inside it. On touch the row must be held briefly, so a swipe still scrolls.</summary>
    Item,
}
