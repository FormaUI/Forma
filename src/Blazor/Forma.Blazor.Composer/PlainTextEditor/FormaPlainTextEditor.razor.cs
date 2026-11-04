namespace Forma.Blazor.Components.Composer.PlainTextEditor;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Browser.Interop.Text;
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

    /// <summary>The options of the composer the canvas belongs to.</summary>
    [CascadingParameter] public ComposerOptions? ComposerOptions { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private ElementReference _textareaElement;
    private TextEntry _textEntry = default!;
    private DomTextEdits _edits = default!;
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

    private bool UpdatesOnChange
        => ComposerOptions?.PlainTextUpdateOn is TextUpdate.Change;

    // The text the textarea renders, which a render never writes back over the reader's typing.
    private string? ShownText
        => _edits.GetShownText(Content);

    private TextEditOptions EditOptions
        => new() { SendsOnInput = !UpdatesOnChange };

    // Updating on change, the selection arrives with the commit, so the textarea handles no click or keystroke of its own.
    private EventCallback<MouseEventArgs> ClickCallback
        => UpdatesOnChange ? default : EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync);

    private EventCallback<KeyboardEventArgs> KeyUpCallback
        => UpdatesOnChange ? default : EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyUpAsync);

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _edits = new DomTextEdits(Context.DomEngine, Content, HandleTextChangedAsync);
        _textEntry = new TextEntry(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _edits.SyncAsync(_textareaElement, Content, EditOptions);

        // The composer's routes are registered on the textarea, ahead of its native shortcuts.
        if (firstRender && Composer is { } context)
        {
            _inputScope = await InputRouter.RegisterAsync(_textareaElement, new InputProfile { InputRoutes = context.KeyBindings.ToInputRoutes() }, _selfReference);
        }
    }

    /// <summary>Runs the composer command named by the action the router matched inside the textarea.</summary>
    [JSInvokable]
    public async Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // The command reads the document, so the reader's edits made before it are applied and committed first.
        if (Interaction is null || !await _edits.SettleAsync())
        {
            return;
        }

        if (_commitCancellation is not null)
        {
            await _commitCancellation.CancelAsync();
        }

        await CommitAsync();
        await Interaction.ExecuteCommandAsync(action);
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

    // Typing commits after a pause, and the textarea's commit at once with the selection it leaves, since updating on change no keystroke
    // reports either.
    private async Task HandleTextChangedAsync(TextChange change)
    {
        _typedContent = change.Text;

        if (change.IsCommit)
        {
            if (_commitCancellation is not null)
            {
                await _commitCancellation.CancelAsync();
            }

            await CommitAsync();
            await ReportSelectionAsync();
            return;
        }

        if (_commitCancellation is not null)
        {
            await _commitCancellation.CancelAsync();
            _commitCancellation.Dispose();
        }

        _commitCancellation = new CancellationTokenSource();
        _ = CommitAfterPauseAsync(_commitCancellation.Token);
    }

    // The pause is waited out apart from the edit, so the next keystroke's edit is applied meanwhile and restarts it.
    [SuppressMessage("Design", "CA1031", Justification = "The commit failing is the component's own failure, handed to the renderer as it would be from an event.")]
    private async Task CommitAfterPauseAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(CommitDelay, cancellationToken);
            await CommitAsync();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
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

        await _edits.DisposeAsync();

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
