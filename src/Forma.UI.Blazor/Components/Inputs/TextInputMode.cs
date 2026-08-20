namespace Forma.UI.Blazor.Components.Inputs;

/// <summary>The virtual keyboard a text box asks a touch device for.</summary>
public enum TextInputMode
{
    /// <summary>No virtual keyboard, for a box that gathers its input some other way.</summary>
    None,

    /// <summary>The standard text keyboard.</summary>
    Text,

    /// <summary>A digits-only keypad, for a code or a quantity.</summary>
    Numeric,

    /// <summary>A numeric keypad carrying a decimal separator, rendered as <c>decimal</c>.</summary>
    Fractional,

    /// <summary>A telephone keypad.</summary>
    Tel,

    /// <summary>A text keyboard laid out for searching.</summary>
    Search,

    /// <summary>A text keyboard carrying the at sign.</summary>
    Email,

    /// <summary>A text keyboard laid out for web addresses.</summary>
    Url,
}
