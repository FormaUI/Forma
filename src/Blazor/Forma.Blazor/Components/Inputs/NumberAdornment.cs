namespace Forma.Blazor.Components.Inputs;

/// <summary>Text standing beside a number in its field, such as a currency symbol or a unit.</summary>
/// <param name="Text">The text shown beside the number.</param>
/// <param name="Side">The side of the field the text stands on.</param>
/// <param name="Description">The words that describe the number to assistive technology, such as the currency's code.</param>
public sealed record NumberAdornment(string Text, AdornmentSide Side, string? Description = null);
