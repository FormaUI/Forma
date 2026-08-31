namespace Forma.Blazor.Components.Inputs;

/// <summary>What a touch keyboard capitalizes automatically as text is typed.</summary>
public enum TextAutoCapitalize
{
    /// <summary>Nothing, for a value such as a username or a code.</summary>
    None,

    /// <summary>The first letter of each sentence.</summary>
    Sentences,

    /// <summary>The first letter of each word, for a name or a place.</summary>
    Words,

    /// <summary>Every letter.</summary>
    Characters,
}
