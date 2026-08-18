namespace Forma.UI.Blazor.Components;

/// <summary>Where a field's label sits relative to the element it names.</summary>
public enum FieldLabelMode
{
    /// <summary>Sits over the element and floats up on focus or once it holds a value.</summary>
    Floating,

    /// <summary>Stays floated, for an element whose box always shows content of its own.</summary>
    Pinned,

    /// <summary>Sits statically above the element, for an element with no box to float over.</summary>
    Above,

    /// <summary>Sits statically before the element on the same row, for an element read as a named setting.</summary>
    Inline,
}
