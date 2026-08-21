namespace Forma.UI.Features.Composer.Editing;

using Forma.UI.Features.Composer.Documents;

/// <summary>An undo and redo stack bounded to a fixed number of recorded states.</summary>
public sealed class ComposerHistory : IComposerHistory
{
    private readonly int _maxCapacity;
    private readonly List<(ComposerDocument Document, ComposerSelection? Selection)> _undoStack = [];
    private readonly List<(ComposerDocument Document, ComposerSelection? Selection)> _redoStack = [];

    /// <summary>Creates a history retaining at most the given number of states.</summary>
    public ComposerHistory(int maxCapacity = 100)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCapacity, 1);
        _maxCapacity = maxCapacity;
    }

    /// <inheritdoc />
    public bool CanUndo
        => _undoStack.Count > 0;

    /// <inheritdoc />
    public bool CanRedo
        => _redoStack.Count > 0;

    /// <inheritdoc />
    public void Record(ComposerDocument document, ComposerSelection? selection = null)
    {
        ArgumentNullException.ThrowIfNull(document);

        _redoStack.Clear();

        if (_undoStack.Count > 0 && _undoStack[^1].Document == document)
        {
            _undoStack[^1] = (document, selection);
            return;
        }

        _undoStack.Add((document, selection));

        if (_undoStack.Count > _maxCapacity)
        {
            _undoStack.RemoveAt(0);
        }
    }

    /// <inheritdoc />
    public ComposerDocument? Undo(ComposerDocument current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (!CanUndo)
        {
            return null;
        }

        _redoStack.Add((current, null));

        var index = _undoStack.Count - 1;
        var (previous, _) = _undoStack[index];
        _undoStack.RemoveAt(index);

        return previous;
    }

    /// <inheritdoc />
    public ComposerDocument? Redo(ComposerDocument current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (!CanRedo)
        {
            return null;
        }

        _undoStack.Add((current, null));

        var index = _redoStack.Count - 1;
        var (next, _) = _redoStack[index];
        _redoStack.RemoveAt(index);

        return next;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
