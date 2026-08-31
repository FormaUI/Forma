namespace Forma.Features.Composer.Editing;

using Forma.Features.Composer.Documents;

/// <summary>An undo and redo stack bounded to a fixed number of recorded states.</summary>
public sealed class ComposerHistory : IComposerHistory
{
    private readonly int _maxCapacity;

    // The top of the undo stack is the state the document currently holds, so undoing needs a second entry beneath it.
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
        => _undoStack.Count > 1;

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
    public ComposerDocument? Undo()
    {
        if (!CanUndo)
        {
            return null;
        }

        var index = _undoStack.Count - 1;
        _redoStack.Add(_undoStack[index]);
        _undoStack.RemoveAt(index);

        return _undoStack[^1].Document;
    }

    /// <inheritdoc />
    public ComposerDocument? Redo()
    {
        if (!CanRedo)
        {
            return null;
        }

        var index = _redoStack.Count - 1;
        var next = _redoStack[index];
        _redoStack.RemoveAt(index);
        _undoStack.Add(next);

        return next.Document;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
