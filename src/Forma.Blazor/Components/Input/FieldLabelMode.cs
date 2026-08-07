namespace Forma.Blazor.Components.Input;

/// <summary>Where a field's label sits relative to the control it names.</summary>
public enum FieldLabelMode
{
    /// <summary>Sits over the control and floats up on focus or once it holds a value.</summary>
    Floating,

    /// <summary>Stays floated, for a control whose box always shows content of its own.</summary>
    Pinned,

    /// <summary>Sits statically above the control, for a control with no box to float over.</summary>
    Above,

    /// <summary>Sits statically before the control on the same row, for a control read as a named setting.</summary>
    Inline,
}
