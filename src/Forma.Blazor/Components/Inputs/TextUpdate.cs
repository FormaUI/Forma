namespace Forma.Blazor.Components.Inputs;

/// <summary>When a text field's value reaches its component.</summary>
public enum TextUpdate
{
    /// <summary>As each character is typed.</summary>
    Input,

    /// <summary>As the field commits its edit: as it loses the focus, and before an action the input router routes while it holds the focus.</summary>
    Change,
}
