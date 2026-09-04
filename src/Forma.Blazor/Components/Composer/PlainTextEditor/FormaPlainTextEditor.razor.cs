namespace Forma.Blazor.Components.Composer.PlainTextEditor;

using Forma.Blazor.Interop.Operations;
using Forma.Framework.Composer.Editing;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>The plain text canvas, filling the surface the engine gives its editor.</summary>
public sealed partial class FormaPlainTextEditor : ComponentBase, IAsyncDisposable
{
    // Keystrokes within this window commit as one state, so undo steps back a phrase rather than a character.
    private static readonly TimeSpan CommitDelay = TimeSpan.FromMilliseconds(400);

    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _textareaElement;
    private TextEntry _textEntry = default!;
    private CancellationTokenSource? _commitCancellation;
    private CancellationTokenSource? _selectionCancellation;
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
    protected override void OnInitialized()
        => _textEntry = new TextEntry(JS);

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

    private async Task HandleInputAsync(ChangeEventArgs args)
    {
        _typedContent = args.Value?.ToString() ?? string.Empty;

        if (_commitCancellation is not null)
        {
            await _commitCancellation.CancelAsync();
            _commitCancellation.Dispose();
        }

        _commitCancellation = new CancellationTokenSource();

        try
        {
            await Task.Delay(CommitDelay, _commitCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await CommitAsync();
    }

    private async Task HandleChangeAsync(ChangeEventArgs args)
    {
        _typedContent = args.Value?.ToString() ?? string.Empty;

        if (_commitCancellation is not null)
        {
            await _commitCancellation.CancelAsync();
        }

        await CommitAsync();
        await ReportSelectionAsync();
    }

    private Task HandleClickAsync(MouseEventArgs args)
        => ReportSelectionAsync();

    // Coalesced the same way as the content commit, so holding an arrow key does not flood interop with one call per repeat.
    private async Task HandleKeyUpAsync(KeyboardEventArgs args)
    {
        if (_selectionCancellation is not null)
        {
            await _selectionCancellation.CancelAsync();
            _selectionCancellation.Dispose();
        }

        _selectionCancellation = new CancellationTokenSource();

        try
        {
            await Task.Delay(CommitDelay, _selectionCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await ReportSelectionAsync();
    }

    private async Task ReportSelectionAsync()
    {
        if (Context is null || Interaction is null)
        {
            return;
        }

        var range = await _textEntry.ReadSelectionAsync(_textareaElement);

        if (range is not null)
        {
            await Interaction.ReportSelectionAsync(new ComposerSelection { Start = range.Start, End = range.End });
        }
    }

    private async Task CommitAsync()
    {
        if (Context is null || Interaction is null || _typedContent is null || Context.Document.Content == _typedContent)
        {
            return;
        }

        _committedContent = _typedContent;

        await Interaction.ChangeDocumentAsync(Context.Document with { Content = _typedContent });
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_commitCancellation is not null)
        {
            await _commitCancellation.CancelAsync();
            _commitCancellation.Dispose();
        }

        if (_selectionCancellation is not null)
        {
            await _selectionCancellation.CancelAsync();
            _selectionCancellation.Dispose();
        }

        await _textEntry.DisposeAsync();
    }
}
