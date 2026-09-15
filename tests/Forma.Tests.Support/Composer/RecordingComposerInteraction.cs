namespace Forma.Tests.Composer;

using Forma.Framework.Composer.Dialogs;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;

/// <summary>An <see cref="IComposerInteraction"/> that records everything a plugin invoked.</summary>
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

    /// <summary>What <see cref="TryInsertAsync"/> reports, standing for an editor that takes the insertion.</summary>
    public bool TakesInsertions { get; set; } = true;

    /// <summary>How many re-renders were requested.</summary>
    public int RenderRequests { get; private set; }

    public Task ChangeDocumentAsync(ComposerDocument document)
    {
        ChangedDocuments.Add(document);
        return Task.CompletedTask;
    }

    public Task RequestRenderAsync()
    {
        RenderRequests++;
        return Task.CompletedTask;
    }

    public Task ReportSelectionAsync(ComposerSelection selection)
    {
        ReportedSelections.Add(selection);
        return Task.CompletedTask;
    }

    public Task ExecuteCommandAsync(string command)
    {
        ExecutedCommands.Add(command);
        return Task.CompletedTask;
    }

    public Task OpenDialogAsync(ComposerInsertionRequest request)
    {
        OpenedDialogs.Add(request);
        return Task.CompletedTask;
    }

    public ValueTask<bool> TryInsertAsync(ComposerInsertion insertion)
    {
        Insertions.Add(insertion);
        return ValueTask.FromResult(TakesInsertions);
    }
}
