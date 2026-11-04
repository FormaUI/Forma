namespace Forma.Blazor.Components.Composer.PlainTextEditor;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The canvas of the plain text editor.</summary>
public sealed partial class FormaPlainTextEditor : FormaComponentBase
{
    // Keystrokes within this delay commit as one history state, and the selection they move is reported once they pause.
    private static readonly TimeSpan CommitDelay = TimeSpan.FromMilliseconds(400);

    private static readonly TextSelectionWatchOptions SelectionOptions = new();

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
    private IDisposable? _editsRegistration;
    private DomObservation<TextSelectionWatchOptions, TextSelection>? _selection;
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

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _edits = new DomTextEdits(Context.DomEngine, Content, HandleTextChangedAsync);
        _textEntry = new TextEntry(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);

        _editsRegistration = Interaction?.JoinEdits(SettleAsync);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Updating on change, the selection arrives with the commit, so the textarea's selection is followed only while it updates on input. The
        // watch starts inactive, so _selection holds it before anything is awaited and a render landing meanwhile syncs the same watch.
        if (firstRender)
        {
            _selection = await Context.DomWatcher.GetElement(_textareaElement).Text.WatchSelectionAsync(HandleSelectionAsync, shouldObserve: false);
        }

        await _edits.SyncAsync(_textareaElement, Content, EditOptions);

        if (_selection is { } selection)
        {
            await selection.SyncAsync(_textareaElement, SelectionOptions, !UpdatesOnChange);
        }

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

        if (Interaction is null || !await SettleAsync())
        {
            return;
        }

        await Interaction.ExecuteCommandAsync(action);
    }

    // What reads the document reads the reader's edits made before it, applied and committed, and the selection as it stands.
    private async Task<bool> SettleAsync()
    {
        if (!await _edits.SettleAsync())
        {
            return false;
        }

        if (_commitCancellation is not null)
        {
            await _commitCancellation.CancelAsync();
        }

        if (_selectionCancellation is not null)
        {
            await _selectionCancellation.CancelAsync();
        }

        await CommitAsync();
        await ReportSelectionAsync();

        return true;
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

    private Task HandleSelectionAsync(TextSelection selection)
        => InvokeAsync(async () =>
        {
            if (_selectionCancellation is not null)
            {
                await _selectionCancellation.CancelAsync();
                _selectionCancellation.Dispose();
            }

            _selectionCancellation = new CancellationTokenSource();
            _ = ReportSelectionAfterPauseAsync(new ComposerSelection { Start = selection.Start, End = selection.End }, _selectionCancellation.Token);
        });

    [SuppressMessage("Design", "CA1031", Justification = "The report failing is the component's own failure, handed to the renderer as it would be from an event.")]
    private async Task ReportSelectionAfterPauseAsync(ComposerSelection selection, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(CommitDelay, cancellationToken);

            if (Interaction is { } interaction)
            {
                await interaction.ReportSelectionAsync(selection);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await DispatchExceptionAsync(ex);
        }
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
        _editsRegistration?.Dispose();

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

        if (_selection is { } selection)
        {
            await selection.DisposeAsync();
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
