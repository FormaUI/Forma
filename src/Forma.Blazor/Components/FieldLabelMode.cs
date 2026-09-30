namespace Forma.Blazor.Components;

/// <summary>Where a field's label sits relative to the element it names.</summary>
public enum FieldLabelMode
{
    /// <summary>Sits over the element and floats up on focus or once it holds a value.</summary>
    Floating,

    /// <summary>Stays floated at the top of the element.</summary>
    Pinned,

    /// <summary>Sits statically above the element.</summary>
    Above,

    /// <summary>Sits statically before the element on the same row.</summary>
    Inline,
}
