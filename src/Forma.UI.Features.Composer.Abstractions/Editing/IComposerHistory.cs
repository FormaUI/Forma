namespace Forma.UI.Features.Composer.Editing;

using Forma.UI.Features.Composer.Documents;

/// <summary>The undo and redo stack shared by every editor in one composer.</summary>
public interface IComposerHistory
{
    /// <summary>Whether a state is available to undo to.</summary>
    bool CanUndo { get; }

    /// <summary>Whether a state is available to redo to.</summary>
    bool CanRedo { get; }

    /// <summary>Records a document state on the undo stack.</summary>
    void Record(ComposerDocument document, ComposerSelection? selection = null);

    /// <summary>Returns the state preceding the current one, or null when none is available.</summary>
    ComposerDocument? Undo(ComposerDocument current);

    /// <summary>Returns the state following the current one, or null when none is available.</summary>
    ComposerDocument? Redo(ComposerDocument current);

    /// <summary>Discards every recorded state.</summary>
    void Clear();
}
