namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Composer.Documents;

/// <summary>The undo and redo stack shared by every editor in one composer.</summary>
public interface IComposerHistory
{
    /// <summary>Whether a state is available to undo to.</summary>
    bool CanUndo { get; }

    /// <summary>Whether a state is available to redo to.</summary>
    bool CanRedo { get; }

    /// <summary>Records a document state on the undo stack.</summary>
    void Record(ComposerDocument document, ComposerSelection? selection = null);

    /// <summary>Steps back to the state preceding the current one.</summary>
    ComposerDocument? Undo();

    /// <summary>Steps forward to the state following the current one.</summary>
    ComposerDocument? Redo();

    /// <summary>Discards every recorded state.</summary>
    void Clear();
}
