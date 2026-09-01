namespace Forma.Tests.Composer;

using Forma.Features.Composer.Documents;
using Forma.Features.Composer.Editing;

/// <summary>An <see cref="IComposerInteraction"/> that records everything a plugin invoked.</summary>
public sealed class RecordingComposerInteraction : IComposerInteraction
{
    /// <summary>The documents reported through <see cref="ChangeDocumentAsync"/>, in order.</summary>
    public List<ComposerDocument> ChangedDocuments { get; } = [];

    /// <summary>The selections reported through <see cref="ReportSelectionAsync"/>, in order.</summary>
    public List<ComposerSelection> ReportedSelections { get; } = [];

    /// <summary>The commands run through <see cref="ExecuteCommandAsync"/>, in order.</summary>
    public List<string> ExecutedCommands { get; } = [];

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
}
