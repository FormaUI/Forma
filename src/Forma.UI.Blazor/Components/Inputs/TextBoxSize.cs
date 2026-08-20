namespace Forma.UI.Blazor.Components.Inputs;

/// <summary>How tall a <see cref="FormaTextBox"/> stands.</summary>
public enum TextBoxSize
{
    /// <summary>Sits level with a <c>FormaButton</c>, for a toolbar, a filter row or a grid cell.</summary>
    Normal,

    /// <summary>Matches the height of the label-bearing fields, for a form beside <c>FormaInput</c>.</summary>
    Large,
}
