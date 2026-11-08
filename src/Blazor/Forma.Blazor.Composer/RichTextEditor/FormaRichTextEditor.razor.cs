namespace Forma.Blazor.Components.Composer.RichTextEditor;

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Forma.Blazor.Components.Composer.Dialogs;
using Forma.Blazor.Components.Composer.Interop;
using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Framework.Browser.Interop.Text;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The visual rich text canvas, filling the surface the engine gives its editor.</summary>
public sealed partial class FormaRichTextEditor : FormaComponentBase
{
    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <summary>The composer interaction cascaded from the parent shell.</summary>
    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    /// <summary>The editor plugin cascaded by the composer surface.</summary>
    [CascadingParameter] public IComposerEditorPlugin? Editor { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    // The canvas commits at its own pace, so each edit it sends is a commit.
    private static readonly TextEditOptions EditOptions = new();

    private ElementReference _editorElement;
    private DotNetObjectReference<FormaRichTextEditor>? _selfReference;
    private RichTextEditor _canvas = default!;
    private DomTextEdits _edits = default!;
    private IDisposable? _editsRegistration;
    private InputScope? _inputScope;

    // The document the canvas shows: the one it last sent, until the composer's document departs from what the canvas committed.
    private string _canvasText = string.Empty;
    private string? _committedContent;
    private string? _receivedContent;
    private string _announcement = string.Empty;
    private string? _announcedBlockType;
    private IReadOnlySet<string> _activeStyles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool _isAttached;

    private RichTextDialogState _dialog = RichTextDialogState.None;
    private bool _isLinkAnchorMode;

    private RichTextEditorPlugin _editorPlugin = default!;

    private bool IsDisabled
        => Composer?.IsDisabled ?? false;

    private bool IsReadOnly
        => Composer?.IsReadOnly ?? false;

    // A static render shows the document and takes no edit, since the canvas's script never runs there.
    private bool IsCanvasDisabled
        => IsDisabled || !RendererInfo.IsInteractive;

    private bool ContentEditable
        => !IsCanvasDisabled && !IsReadOnly;

    private bool IsSelectionBarShown
        => Composer?.IsSelectionBarShown ?? true;

    /// <summary>Whether the composer holds any gutter markers.</summary>
    private bool HasMarkers
        => Composer?.Regions.GutterMarkers.Count > 0;

    private string EditorClass
        => HasMarkers ? "forma-composer-rich-text-editor has-gutter" : "forma-composer-rich-text-editor";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _canvas = new RichTextEditor(Context.JS);
        _selfReference = DotNetObjectReference.Create(this);

        // The canvas attaches empty, and the document is written into it as a text of .NET's own.
        _edits = new DomTextEdits(Context.DomEngine, string.Empty, HandleContentChangedAsync);
        _committedContent = _receivedContent = Composer?.Document.Content;
        _canvasText = _committedContent ?? string.Empty;

        _editorPlugin = Editor as RichTextEditorPlugin
            ?? throw new InvalidOperationException($"{nameof(FormaRichTextEditor)} requires a cascaded {nameof(RichTextEditorPlugin)}.");

        _editsRegistration = Interaction?.JoinEdits(SettleAsync);

        _editorPlugin.CommandRequested += HandlePluginCommand;
        _editorPlugin.LinkDialogRequested += HandleLinkDialogRequested;
        _editorPlugin.TableDialogRequested += HandleTableDialogRequested;
        _editorPlugin.VideoDialogRequested += HandleVideoDialogRequested;
        _editorPlugin.ImageDialogRequested += HandleImageDialogRequested;
        _editorPlugin.EmbedDialogRequested += HandleEmbedDialogRequested;
    }

    // The canvas takes the document's content only when the document changed since it was last received, and departs from what the canvas
    // committed, so a render that brings the document as it stood never writes over what was typed since.
    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        var documentContent = Composer?.Document.Content;

        if (documentContent == _receivedContent)
        {
            return;
        }

        _receivedContent = documentContent;

        if (documentContent != _committedContent)
        {
            _committedContent = documentContent;
            _canvasText = documentContent ?? string.Empty;
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isAttached = await _canvas.AttachAsync(_editorElement, _selfReference!);

            // The composer's routes are registered on the editable itself, so they are answered before its native shortcuts.
            if (Composer is { } context)
            {
                _inputScope = await InputRouter.RegisterAsync(_editorElement, new InputProfile { InputRoutes = context.KeyBindings.ToInputRoutes() }, _selfReference);
            }

            StateHasChanged();
        }

        // The canvas's script provides the document its edits read, so they follow it once it is attached.
        if (_isAttached)
        {
            await _edits.SyncAsync(_editorElement, _canvasText, EditOptions);
        }
    }

    /// <summary>Runs the composer command named by the action the router matched inside the canvas.</summary>
    /// <remarks>The composer settles the canvas's edits, which it joined, before the command reads the document.</remarks>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return Interaction?.ExecuteCommandAsync(action) ?? Task.CompletedTask;
    }

    // Each commit the canvas sends becomes the composer's document, and a text that can't follow the reader's edits is shown on the composer's
    // field once the change is handled, so a render the report causes finds the document as the change left it.
    private async Task HandleContentChangedAsync(TextChange change)
    {
        _canvasText = change.Text;

        try
        {
            if (Composer is null || Interaction is null || change.IsFaulted)
            {
                return;
            }

            var sanitized = Composer.Sanitizer.Sanitize(change.Text);
            _committedContent = sanitized;

            await Interaction.ChangeDocumentAsync(Composer.Document with { Content = sanitized });
        }
        finally
        {
            Interaction?.ReportTextFault(change.IsFaulted);
        }
    }

    // What reads the document reads every edit the reader made in the canvas, its pause cut short.
    private async Task<bool> SettleAsync()
    {
        if (!_isAttached)
        {
            return true;
        }

        await _canvas.CommitAsync();

        return await _edits.SettleAsync();
    }

    /// <summary>Reports the canvas's selection snapshot to the composer.</summary>
    [JSInvokable]
    public async Task HandleSelectionChangedAsync(RichTextSelectionDto dto)
    {
        if (Composer is null || Interaction is null || dto is null)
        {
            return;
        }

        AnnounceBlock(dto.BlockType);

        var selection = new ComposerSelection
        {
            Start = dto.Start,
            End = dto.End,
            BlockType = dto.BlockType,
            ActiveStyles = new HashSet<string>(dto.ActiveStyles, StringComparer.OrdinalIgnoreCase),
        };

        _activeStyles = selection.ActiveStyles;
        await Interaction.ReportSelectionAsync(selection);
    }

    /// <summary>Reopens the image dialog on an image in the document.</summary>
    /// <param name="image">The image's current values, as the transfer carries them.</param>
    [JSInvokable]
    public Task HandleImageActivatedAsync(JSTransfer image)
        => ReopenAsync(RichTextDialogKind.Image, image, RichTextEditorJsonContext.Default.ImageDialogResult);

    /// <summary>Reopens the video dialog on a video in the document.</summary>
    /// <param name="video">The video's current values, as the transfer carries them.</param>
    [JSInvokable]
    public Task HandleVideoActivatedAsync(JSTransfer video)
        => ReopenAsync(RichTextDialogKind.Video, video, RichTextEditorJsonContext.Default.VideoDialogResult);

    /// <summary>Reopens the embed dialog on an embed in the document.</summary>
    /// <param name="embed">The embed's current values, as the transfer carries them.</param>
    [JSInvokable]
    public Task HandleEmbedActivatedAsync(JSTransfer embed)
        => ReopenAsync(RichTextDialogKind.Embed, embed, RichTextEditorJsonContext.Default.EmbedDialogResult);

    /// <summary>Reopens the link dialog on a link or a named anchor in the document.</summary>
    /// <param name="isAnchor">Whether the link is a named anchor.</param>
    [JSInvokable]
    public Task HandleLinkActivatedAsync(bool isAnchor)
    {
        if (Composer is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return Task.CompletedTask;
        }

        HandleLinkDialogRequested(isAnchor);

        return Task.CompletedTask;
    }

    // The canvas has already selected the element whole, so confirming replaces it. What the element holds can be large, an image's
    // data address among it, so it crosses through the transfer.
    private async Task ReopenAsync<TResult>(RichTextDialogKind kind, JSTransfer values, JsonTypeInfo<TResult> typeInfo)
        where TResult : ComposerDialogResult
    {
        ArgumentNullException.ThrowIfNull(values);

        JsonElement json;

        try
        {
            json = await values.ReadJsonAsync(Context.JS.TransferOptions.MaximumLength);
        }
        catch (JSTransferException ex)
        {
            RichTextEditorLog.ActivationLost(Context.Logger, ex, kind.ToString(), Context.RuntimeId);
            return;
        }

        if (Composer is null || Interaction is null || IsDisabled || IsReadOnly || json.Deserialize(typeInfo) is not { } result)
        {
            return;
        }

        _dialog = new RichTextDialogState(kind, result);
        StateHasChanged();
    }

    /// <summary>Runs the named composer command.</summary>
    public void HandleCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        if (Composer is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return;
        }

        // Plugin command names route through the engine's command registry, which brings the document in step with the canvas first.
        _ = Interaction.ExecuteCommandAsync(command);
    }

    private void HandleLinkDialogRequested(bool isAnchor)
    {
        _isLinkAnchorMode = isAnchor;

        _ = InvokeAsync(async () =>
        {
            var target = await _canvas.BeginLinkDialogAsync(isAnchor);

            // A single link or anchor the selection holds prefills its address, its name and its tab.
            var single = target?.Count == 1 ? target : null;
            _dialog = new RichTextDialogState(
                RichTextDialogKind.Link,
                new LinkDialogResult
                {
                    IsAnchor = isAnchor,
                    Name = (isAnchor ? single?.Name : null) ?? target?.Text ?? string.Empty,
                    Href = single?.Href ?? string.Empty,
                    OpenInNewTab = single?.OpensInNewTab ?? false,
                });

            StateHasChanged();
        });
    }

    private void HandleTableDialogRequested()
    {
        _dialog = new RichTextDialogState(RichTextDialogKind.Table);
        StateHasChanged();
    }

    private void HandleVideoDialogRequested()
    {
        _dialog = new RichTextDialogState(RichTextDialogKind.Video);
        StateHasChanged();
    }

    private void HandleImageDialogRequested()
    {
        _dialog = new RichTextDialogState(RichTextDialogKind.Image);
        StateHasChanged();
    }

    private void HandleEmbedDialogRequested()
    {
        _dialog = new RichTextDialogState(RichTextDialogKind.Embed);
        StateHasChanged();
    }

    private void CloseDialog()
        => _dialog = RichTextDialogState.None;

    private async Task HandleLinkInsertedAsync(LinkDialogResult result)
    {
        CloseDialog();
        await ExecuteCommandAsync("createLink", result);
    }

    private async Task HandleTableInsertedAsync(TableDialogResult result)
    {
        CloseDialog();
        await ExecuteCommandAsync("insertTable", result);
    }

    private async Task HandleVideoInsertedAsync(VideoDialogResult result)
    {
        CloseDialog();
        await ExecuteCommandAsync("insertVideo", result);
    }

    private async Task HandleImageInsertedAsync(ImageDialogResult result)
    {
        CloseDialog();
        await ExecuteCommandAsync("insertImage", result);
    }

    private async Task HandleEmbedInsertedAsync(EmbedDialogResult result)
    {
        CloseDialog();
        await ExecuteCommandAsync("insertEmbed", result);
    }

    private Task ExecuteCommandAsync(string command, string? value = null)
        => ApplyCommandAsync(() => _canvas.ExecuteCommandAsync(command, value));

    private Task ExecuteCommandAsync(string command, int value)
        => ApplyCommandAsync(() => _canvas.ExecuteCommandAsync(command, value));

    private Task ExecuteCommandAsync(string command, ComposerDialogResult result)
        => ApplyCommandAsync(() => _canvas.ExecuteCommandAsync(command, result));

    private Task ExecuteCommandAsync(string command, ComposerInsertion insertion)
        => ApplyCommandAsync(() => _canvas.ExecuteCommandAsync(command, insertion));

    /// <summary>Runs a command on the canvas and takes the document and the selection it leaves.</summary>
    private async Task ApplyCommandAsync(Func<ValueTask<RichTextCommandResult?>> execute)
    {
        if (Composer is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return;
        }

        // The document the command leaves reaches the canvas's edits ahead of its answer, and is the composer's once they are applied.
        var result = await execute();
        if (result is not null && await _edits.SettleAsync() && result.Selection is not null)
        {
            var before = _activeStyles;
            await HandleSelectionChangedAsync(result.Selection);
            AnnounceFormatting(before, _activeStyles);
        }
    }

    // The plugin hands over only the text, the number or the insertion a command takes.
    private void HandlePluginCommand(string command, object? value)
    {
        _ = InvokeAsync(async () =>
        {
            await (value switch
            {
                null => ExecuteCommandAsync(command),
                string text => ExecuteCommandAsync(command, text),
                int number => ExecuteCommandAsync(command, number),
                ComposerInsertion insertion => ExecuteCommandAsync(command, insertion),
                _ => throw new UnreachableException($"The command '{command}' carries a {value.GetType()}."),
            });
        });
    }

    /// <summary>The formatting a command can turn on or off, by the style the canvas reports and the name it is announced by.</summary>
    private static readonly (string Style, string Name)[] Formats =
    [
        ("bold", "Bold"),
        ("italic", "Italic"),
        ("underline", "Underline"),
        ("strike", "Strikethrough"),
        ("subscript", "Subscript"),
        ("superscript", "Superscript"),
        ("code", "Code"),
    ];

    // Announces the first formatting a command turned on or off; the caret moving through formatting is left to the screen reader.
    private void AnnounceFormatting(IReadOnlySet<string> before, IReadOnlySet<string> after)
    {
        foreach (var (style, name) in Formats)
        {
            var wasOn = before.Contains(style);
            var isOn = after.Contains(style);

            if (wasOn != isOn)
            {
                _announcement = isOn ? L["{Format} on", L[name]] : L["{Format} off", L[name]];
                StateHasChanged();

                return;
            }
        }
    }

    // A block is announced once, as the caret enters it.
    private void AnnounceBlock(string? blockType)
    {
        if (string.IsNullOrEmpty(blockType) || string.Equals(blockType, _announcedBlockType, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _announcedBlockType = blockType;
        _announcement = blockType.ToUpperInvariant() switch
        {
            "H1" => L["Heading 1"],
            "H2" => L["Heading 2"],
            "H3" => L["Heading 3"],
            "H4" => L["Heading 4"],
            "H5" => L["Heading 5"],
            "H6" => L["Heading 6"],
            "BLOCKQUOTE" => L["Blockquote"],
            "PRE" => L["Code Block"],
            _ => L["Paragraph"],
        };
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        // The plugin is unset when OnInitialized threw for a missing cascade, and disposal still runs.
        if (_editorPlugin is not null)
        {
            _editorPlugin.CommandRequested -= HandlePluginCommand;
            _editorPlugin.LinkDialogRequested -= HandleLinkDialogRequested;
            _editorPlugin.TableDialogRequested -= HandleTableDialogRequested;
            _editorPlugin.VideoDialogRequested -= HandleVideoDialogRequested;
            _editorPlugin.ImageDialogRequested -= HandleImageDialogRequested;
            _editorPlugin.EmbedDialogRequested -= HandleEmbedDialogRequested;
        }

        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            await inputScope.DisposeAsync();
        }

        _editsRegistration?.Dispose();
        _selfReference?.Dispose();

        // The edits are made before the plugin is checked, so disposal after a missing cascade finds none.
        if (_edits is not null)
        {
            await _edits.DisposeAsync();
        }

        await _canvas.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
