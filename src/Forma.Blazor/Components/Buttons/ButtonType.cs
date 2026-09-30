namespace Forma.Blazor.Components.Buttons;

/// <summary>The behavior a button has when it sits inside a form.</summary>
public enum ButtonType
{
    /// <summary>Raises its click callback.</summary>
    Button,

    /// <summary>Submits the surrounding form.</summary>
    Submit,

    /// <summary>Restores the surrounding form's inputs to their initial values.</summary>
    Reset
}
