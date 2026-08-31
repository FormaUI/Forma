namespace Forma.Blazor.Components.Inputs;

/// <summary>The kind of text a text box holds, limited to those the box itself draws.</summary>
public enum TextInputType
{
    /// <summary>Free text.</summary>
    Text,

    /// <summary>An email address.</summary>
    Email,

    /// <summary>A telephone number, which the browser leaves unvalidated because formats vary by country.</summary>
    Tel,

    /// <summary>A web address.</summary>
    Url,
}
