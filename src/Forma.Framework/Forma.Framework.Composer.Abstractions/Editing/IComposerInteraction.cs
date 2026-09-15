namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Composer.Dialogs;
using Forma.Framework.Composer.Documents;

/// <summary>Everything a plugin invokes on the composer.</summary>
public interface IComposerInteraction
{
    /// <summary>Reports a document the editor has changed, so the engine can record, sanitize and raise it.</summary>
    Task ChangeDocumentAsync(ComposerDocument document);

    /// <summary>Asks the composer shell to re-render.</summary>
    Task RequestRenderAsync();

    /// <summary>Reports the caret or range an editor last observed, so the engine can share it with every plugin.</summary>
    Task ReportSelectionAsync(ComposerSelection selection);

    /// <summary>Runs a named command through the engine, which resolves it against the engine's own commands and then the registered plugins.</summary>
    Task ExecuteCommandAsync(string command);

    /// <summary>Opens a dialog the plugin describes, inside the composer's own shell.</summary>
    Task OpenDialogAsync(ComposerInsertionRequest request);

    /// <summary>Hands an insertion to the active editor, reporting whether it took one of that kind.</summary>
    ValueTask<bool> TryInsertAsync(ComposerInsertion insertion);
}
