namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Composer.Dialogs;
using Forma.Framework.Composer.Documents;

/// <summary>Everything a plugin invokes on the composer.</summary>
public interface IComposerInteraction
{
    /// <summary>Reports a document the editor has changed.</summary>
    Task ChangeDocumentAsync(ComposerDocument document);

    /// <summary>Asks the composer shell to re-render.</summary>
    Task RequestRenderAsync();

    /// <summary>Reports the caret or range an editor last observed.</summary>
    Task ReportSelectionAsync(ComposerSelection selection);

    /// <summary>Runs a named command through the engine.</summary>
    Task ExecuteCommandAsync(string command);

    /// <summary>Opens a dialog the plugin describes, inside the composer's own shell.</summary>
    Task OpenDialogAsync(ComposerInsertionRequest request);

    /// <summary>Hands an insertion to the active editor, reporting whether it took one of that kind.</summary>
    ValueTask<bool> TryInsertAsync(ComposerInsertion insertion);

    /// <summary>Takes the edits of the editor in the surface, which <paramref name="settleEditsAsync"/> settles, answering whether the document holds every edit the reader made there, so the composer settles them before anything reads the document.</summary>
    /// <returns>The registration, which takes the edits out as it is disposed.</returns>
    IDisposable JoinEdits(Func<Task<bool>> settleEditsAsync);

    /// <summary>Shows on the composer's field that the text of the editor in the surface can't follow the reader's edits while <paramref name="isFaulted"/>, and takes it off once it can.</summary>
    void ReportTextFault(bool isFaulted);
}
