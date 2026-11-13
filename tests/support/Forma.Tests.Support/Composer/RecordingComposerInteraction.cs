namespace Forma.Tests.Composer;

using Forma.Framework.Composer.Dialogs;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;

/// <summary>An <see cref="IComposerInteraction"/> that records every call made on it.</summary>
public sealed class RecordingComposerInteraction : IComposerInteraction
{
    /// <summary>The documents reported through <see cref="ChangeDocumentAsync"/>, in order.</summary>
    public List<ComposerDocument> ChangedDocuments { get; } = [];

    /// <summary>The selections reported through <see cref="ReportSelectionAsync"/>, in order.</summary>
    public List<ComposerSelection> ReportedSelections { get; } = [];

    /// <summary>The commands run through <see cref="ExecuteCommandAsync"/>, in order.</summary>
    public List<string> ExecutedCommands { get; } = [];

    /// <summary>The dialogs opened through <see cref="OpenDialogAsync"/>, in order.</summary>
    public List<ComposerInsertionRequest> OpenedDialogs { get; } = [];

    /// <summary>The insertions handed to the active editor through <see cref="TryInsertAsync"/>, in order.</summary>
    public List<ComposerInsertion> Insertions { get; } = [];

    /// <summary>Whether the active editor takes an insertion, as <see cref="TryInsertAsync"/> reports.</summary>
    public bool TakesInsertions { get; set; } = true;

    /// <summary>How many re-renders were requested.</summary>
    public int RenderRequestCount { get; private set; }

    /// <summary>The settling of the edits joined through <see cref="JoinEdits"/>, while joined.</summary>
    public Func<Task<bool>>? JoinedEdits { get; private set; }

    /// <summary>The focus joined through <see cref="JoinFocus"/>, while joined.</summary>
    public Func<bool, ValueTask<bool>>? JoinedFocus { get; private set; }

    /// <summary>Whether each report the editor made said its text can't follow the reader's edits, in order.</summary>
    public List<bool> TextFaults { get; } = [];

    /// <inheritdoc />
    public void ReportTextFault(bool isFaulted)
        => TextFaults.Add(isFaulted);

    /// <inheritdoc />
    public Task ChangeDocumentAsync(ComposerDocument document)
    {
        ChangedDocuments.Add(document);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RequestRenderAsync()
    {
        RenderRequestCount++;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ReportSelectionAsync(ComposerSelection selection)
    {
        ReportedSelections.Add(selection);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ExecuteCommandAsync(string command)
    {
        ExecutedCommands.Add(command);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task OpenDialogAsync(ComposerInsertionRequest request)
    {
        OpenedDialogs.Add(request);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<bool> TryInsertAsync(ComposerInsertion insertion)
    {
        Insertions.Add(insertion);
        return ValueTask.FromResult(TakesInsertions);
    }

    /// <inheritdoc />
    public IDisposable JoinEdits(Func<Task<bool>> settleEditsAsync)
    {
        JoinedEdits = settleEditsAsync;

        return new Joined(this, settleEditsAsync);
    }

    /// <inheritdoc />
    public IDisposable JoinFocus(Func<bool, ValueTask<bool>> focusAsync)
    {
        JoinedFocus = focusAsync;

        return new JoinedFocusRegistration(this, focusAsync);
    }

    /// <summary>The focus joined, taken out as the registration is disposed.</summary>
    private sealed class JoinedFocusRegistration(RecordingComposerInteraction interaction, Func<bool, ValueTask<bool>> focusAsync) : IDisposable
    {
        public void Dispose()
        {
            if (interaction.JoinedFocus == focusAsync)
            {
                interaction.JoinedFocus = null;
            }
        }
    }

    /// <summary>The edits joined, taken out as the registration is disposed.</summary>
    private sealed class Joined(RecordingComposerInteraction interaction, Func<Task<bool>> settleEditsAsync) : IDisposable
    {
        public void Dispose()
        {
            if (interaction.JoinedEdits == settleEditsAsync)
            {
                interaction.JoinedEdits = null;
            }
        }
    }
}
