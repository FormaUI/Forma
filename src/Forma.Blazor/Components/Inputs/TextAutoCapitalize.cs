namespace Forma.Blazor.Components.Inputs;

/// <summary>What a touch keyboard capitalizes automatically as text is typed.</summary>
public enum TextAutoCapitalize
{
    /// <summary>No letters.</summary>
    None,

    /// <summary>The first letter of each sentence.</summary>
    Sentences,

    /// <summary>The first letter of each word.</summary>
    Words,

    /// <summary>Every letter.</summary>
    Characters,
}
