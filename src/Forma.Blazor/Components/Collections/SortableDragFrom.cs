namespace Forma.Blazor.Components.Collections;

/// <summary>Where on a row a drag may start.</summary>
public enum SortableDragFrom
{
    /// <summary>Only the drag handle starts a drag.</summary>
    Handle,

    /// <summary>Anywhere on the row outside its interactive elements starts a drag, after a brief hold on touch.</summary>
    Item,
}
