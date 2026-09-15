namespace Forma.Framework.Composer.Dialogs;

using Forma.Framework.Composer.Editing;

/// <summary>The body of a dialog a plugin opens, which collects something to put into the document.</summary>
/// <remarks>
/// The body resolves its own title, notice and messages, because the strings belong to whichever package
/// ships the body rather than to the composer hosting it.
/// </remarks>
public interface IComposerInsertionDialog
{
    /// <summary>The title shown in the dialog's header, in the reader's culture.</summary>
    string Title { get; }

    /// <summary>Text explaining why the dialog can do nothing, or null while it can.</summary>
    string? Notice => null;

    /// <summary>What confirming refused, empty while nothing has been refused.</summary>
    IReadOnlyList<string> Messages => [];

    /// <summary>Answers what to put into the document, or null with a message saying why nothing can be.</summary>
    ValueTask<ComposerInsertion?> TryConfirmAsync(CancellationToken cancellationToken = default);
}
