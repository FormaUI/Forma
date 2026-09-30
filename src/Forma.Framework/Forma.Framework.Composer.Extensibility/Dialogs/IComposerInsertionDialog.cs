namespace Forma.Framework.Composer.Dialogs;

using Forma.Framework.Composer.Editing;

/// <summary>The body of a dialog a plugin opens, which collects something to put into the document.</summary>
/// <remarks>The body localizes its own title, notice and messages.</remarks>
public interface IComposerInsertionDialog
{
    /// <summary>The title shown in the dialog's header, in the reader's culture.</summary>
    string Title { get; }

    /// <summary>The text explaining why the dialog can do nothing.</summary>
    string? Notice => null;

    /// <summary>The messages saying what confirming refused.</summary>
    IReadOnlyList<string> Messages => [];

    /// <summary>Answers what to put into the document, where null is a refusal whose reasons are the dialog's messages.</summary>
    ValueTask<ComposerInsertion?> TryConfirmAsync(CancellationToken cancellationToken = default);
}
