namespace Forma.Framework.Browser.Interop.Text;

/// <summary>A change a text field's edits made to the text .NET holds: the text as it now stands, and whether the field committed it.</summary>
public sealed record TextChange
{
    /// <summary>The text as it now stands.</summary>
    public required string Text { get; init; }

    /// <summary>Whether the field committed its text with the change, as it does losing the focus.</summary>
    public bool IsCommit { get; init; }

    /// <summary>Whether the text can't follow the field's edits, an edit having been lost on its way or refused with the field's whole text, the text being the last it held.</summary>
    public bool IsFaulted { get; init; }
}
