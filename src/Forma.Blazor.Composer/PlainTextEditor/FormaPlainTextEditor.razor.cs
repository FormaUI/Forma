namespace Forma.Blazor.Components.Composer.PlainTextEditor;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>The canvas of the plain text editor.</summary>
public sealed partial class FormaPlainTextEditor : FormaComponentBase
{
    // Keystrokes within this delay commit as one history state.
    private static readonly TimeSpan CommitDelay = TimeSpan.FromMilliseconds(400);

    /// <summary>The context of the composer the canvas belongs to.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <summary>The interaction the canvas invokes on the composer.</summary>
    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private ElementReference _textareaElement;
    private TextEntry _textEntry = default!;
    private DotNetObjectReference<FormaPlainTextEditor>? _selfReference;
    private InputScope? _inputScope;
    private CancellationTokenSource? _commitCancellation;
    private CancellationTokenSource? _selectionCancellation;
    private string? _typedContent;
    private string? _committedContent;

    // Uncommitted typing renders ahead of the document's content.
    private string Content
        => _typedContent ?? Composer?.Document.Content ?? string.Empty;

    private bool IsDisabled
        => Composer?.IsDisabled ?? false;

    private bool IsReadOnly
        => Composer?.IsReadOnly ?? false;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _textEntry = new TextEntry(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // The composer's routes are registered on the textarea, ahead of its native shortcuts.
        if (firstRender && Composer is { } context)
        {
            _inputScope = await InputRouter.RegisterAsync(_textareaElement, new InputProfile { InputRoutes = context.KeyBindings.ToInputRoutes() }, _selfReference);
        }
    }

    /// <summary>Runs the composer command named by the action the router matched inside the textarea.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return Interaction?.ExecuteCommandAsync(action) ?? Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var documentContent = Composer?.Document.Content;

        if (documentContent != _committedContent)
        {
            _typedContent = null;
            _committedContent = documentContent;
        }
    }

    private async Task HandleInputAsync(string? text)
    {
        _typedContent = text ?? string.Empty;

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

    // Selection reports are debounced by the same delay as the content commit.
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
        if (Composer is null || Interaction is null)
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
        if (Composer is null || Interaction is null || _typedContent is null || Composer.Document.Content == _typedContent)
        {
            return;
        }

        _committedContent = _typedContent;

        await Interaction.ChangeDocumentAsync(Composer.Document with { Content = _typedContent });
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
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

        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            await inputScope.DisposeAsync();
        }

        _selfReference?.Dispose();
        await _textEntry.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
