namespace Forma.UI.Blazor.Components.Inputs;

/// <summary>What a touch keyboard's action key does, and how it is labelled.</summary>
public enum TextEnterKeyHint
{
    /// <summary>Inserts a new line.</summary>
    Enter,

    /// <summary>Finishes editing, with nothing further to fill in.</summary>
    Done,

    /// <summary>Leaves for the address the text names.</summary>
    Go,

    /// <summary>Moves on to the next field.</summary>
    Next,

    /// <summary>Returns to the previous field.</summary>
    Previous,

    /// <summary>Runs a search for the text.</summary>
    Search,

    /// <summary>Sends the text.</summary>
    Send,
}
