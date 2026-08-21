namespace Forma.UI.Blazor.Components.Composer.PlainTextEditor;

using Forma.UI.Features.Composer.Editing;

using Microsoft.AspNetCore.Components;

/// <summary>Plain text editing surface for the composer engine.</summary>
public sealed partial class FormaPlainTextEditor : ComponentBase, IDisposable
{
    // Keystrokes within this window commit as one state, so undo steps back a phrase rather than a character.
    private static readonly TimeSpan CommitDelay = TimeSpan.FromMilliseconds(400);

    /// <summary>The shared composer context cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    private CancellationTokenSource? _commitCts;
    private string? _typedContent;
    private string? _committedContent;

    // Rendering what the textarea already holds keeps an interleaved render from overwriting uncommitted typing.
    private string Content
        => _typedContent ?? Context?.Document.Content ?? string.Empty;

    private bool IsDisabled
        => Context?.IsDisabled ?? false;

    private bool IsReadOnly
        => Context?.IsReadOnly ?? false;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var documentContent = Context?.Document.Content;

        if (documentContent != _committedContent)
        {
            _typedContent = null;
            _committedContent = documentContent;
        }
    }

    private async Task HandleInputAsync(ChangeEventArgs e)
    {
        _typedContent = e.Value?.ToString() ?? string.Empty;

        if (_commitCts is not null)
        {
            await _commitCts.CancelAsync();
            _commitCts.Dispose();
        }

        _commitCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(CommitDelay, _commitCts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await CommitAsync();
    }

    private async Task HandleChangeAsync(ChangeEventArgs e)
    {
        _typedContent = e.Value?.ToString() ?? string.Empty;

        if (_commitCts is not null)
        {
            await _commitCts.CancelAsync();
        }

        await CommitAsync();
    }

    private async Task CommitAsync()
    {
        if (Context is null || _typedContent is null || Context.Document.Content == _typedContent)
        {
            return;
        }

        _committedContent = _typedContent;

        await Context.ChangeDocumentAsync(Context.Document with { Content = _typedContent });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _commitCts?.Cancel();
        _commitCts?.Dispose();
    }
}
