namespace Forma.Blazor.Components.Composer.RichTextEditor;

using System.Globalization;

using Forma.Blazor.Components.Composer.Dialogs;
using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Blazor.Interop.Components;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The visual rich text canvas, filling the surface the engine gives its editor.</summary>
public sealed partial class FormaRichTextEditor : ComponentBase, IAsyncDisposable
{
    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerState? State { get; set; }

    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    /// <summary>The editor plugin cascaded by the composer surface.</summary>
    [CascadingParameter] public IComposerEditorPlugin? Editor { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private ElementReference _editorElement;
    private DotNetObjectReference<FormaRichTextEditor>? _selfReference;
    private RichTextEditor _engine = default!;
    private string? _committedContent;
    private string _blockAnnouncement = string.Empty;
    private bool _isAttached;

    private RichTextDialog _dialog = RichTextDialog.None;
    private bool _isLinkAnchorMode;
    private ExplorerPluginOptions _explorerOptions = ExplorerPluginOptions.Default;

    private RichTextEditorPlugin _plugin = default!;

    private bool IsDisabled
        => State?.IsDisabled ?? false;

    private bool IsReadOnly
        => State?.IsReadOnly ?? false;

    private bool ContentEditable
        => !IsDisabled && !IsReadOnly;

    private bool IsSelectionBarShown
        => State?.IsSelectionBarShown ?? true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _engine = new RichTextEditor(JS);
        _selfReference = DotNetObjectReference.Create(this);

        _plugin = Editor as RichTextEditorPlugin
            ?? throw new InvalidOperationException($"{nameof(FormaRichTextEditor)} requires a cascaded {nameof(RichTextEditorPlugin)}.");

        _plugin.CommandRequested += HandlePluginCommand;
        _plugin.LinkDialogRequested += HandleLinkDialogRequested;
        _plugin.TableDialogRequested += HandleTableDialogRequested;
        _plugin.VideoDialogRequested += HandleVideoDialogRequested;
        _plugin.ImageDialogRequested += HandleImageDialogRequested;
        _plugin.ExplorerDialogRequested += HandleExplorerDialogRequested;
        _plugin.EmbedDialogRequested += HandleEmbedDialogRequested;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var documentContent = State?.Document.Content;

        if (_isAttached && documentContent != _committedContent && documentContent is not null)
        {
            _committedContent = documentContent;
            await _engine.SetHtmlAsync(documentContent);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isAttached = await _engine.AttachAsync(_editorElement, _selfReference!);

            var initialContent = State?.Document.Content;
            if (!string.IsNullOrEmpty(initialContent))
            {
                _committedContent = initialContent;
                await _engine.SetHtmlAsync(initialContent);
            }

            StateHasChanged();
        }
    }

    /// <summary>Callback invoked from JS when content changes via debounced typing or blur.</summary>
    [JSInvokable]
    public async Task HandleContentChangedAsync(string html)
    {
        if (State is null || Interaction is null)
        {
            return;
        }

        var sanitized = State.Sanitizer.Sanitize(html);
        _committedContent = sanitized;

        await Interaction.ChangeDocumentAsync(State.Document with { Content = sanitized });
    }

    /// <summary>Callback invoked from JS when the selection snapshot changes.</summary>
    [JSInvokable]
    public async Task HandleSelectionChangedAsync(RichTextSelectionDto dto)
    {
        if (State is null || Interaction is null || dto is null)
        {
            return;
        }

        UpdateBlockAnnouncement(dto.BlockType);

        var selection = new ComposerSelection
        {
            Start = dto.Start,
            End = dto.End,
            BlockType = dto.BlockType,
            ActiveStyles = new HashSet<string>(dto.ActiveStyles, StringComparer.OrdinalIgnoreCase),
        };

        await Interaction.ReportSelectionAsync(selection);
    }

    /// <summary>Callback invoked from JS when the author double-clicks an image already in the document.</summary>
    /// <param name="image">What that image already holds.</param>
    [JSInvokable]
    public Task HandleImageActivatedAsync(ImageDialogResult image)
        => ReopenAsync(new RichTextDialog(RichTextDialogKind.Image, image));

    /// <summary>Callback invoked from JS when the author double-clicks a video already in the document.</summary>
    /// <param name="video">What that video already holds.</param>
    [JSInvokable]
    public Task HandleVideoActivatedAsync(VideoDialogResult video)
        => ReopenAsync(new RichTextDialog(RichTextDialogKind.Video, video));

    /// <summary>Callback invoked from JS when the author double-clicks an embed already in the document.</summary>
    /// <param name="embed">What that embed already holds.</param>
    [JSInvokable]
    public Task HandleEmbedActivatedAsync(EmbedDialogResult embed)
        => ReopenAsync(new RichTextDialog(RichTextDialogKind.Embed, embed));

    /// <summary>Callback invoked from JS when the author double-clicks a link or a named anchor.</summary>
    /// <param name="isAnchor">Whether it is a named anchor rather than a hyperlink.</param>
    [JSInvokable]
    public Task HandleLinkActivatedAsync(bool isAnchor)
    {
        if (State is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return Task.CompletedTask;
        }

        // A link dialog fills its fields from the canvas, whichever way it was asked for.
        HandleLinkDialogRequested(isAnchor);

        return Task.CompletedTask;
    }

    // The canvas has already selected the element whole, so confirming replaces it.
    private Task ReopenAsync(RichTextDialog dialog)
    {
        if (State is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return Task.CompletedTask;
        }

        _dialog = dialog;
        StateHasChanged();

        return Task.CompletedTask;
    }

    /// <summary>Handles commands raised directly by UI elements like the selection bar.</summary>
    public void HandleCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        if (State is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return;
        }

        // These are plugin command names, so they route through the engine registry rather than straight at the canvas.
        _ = Interaction.ExecuteCommandAsync(command);
    }

    private void HandleLinkDialogRequested(bool isAnchor)
    {
        _isLinkAnchorMode = isAnchor;

        _ = InvokeAsync(async () =>
        {
            var target = await _engine.BeginLinkDialogAsync(isAnchor);

            // One link is being retargeted, so its address comes back; several share none, and the author names it.
            _dialog = new RichTextDialog(
                RichTextDialogKind.Link,
                new LinkDialogResult(
                    isAnchor,
                    target?.Text ?? string.Empty,
                    target?.Count == 1 ? target.Href ?? string.Empty : string.Empty,
                    OpenInNewTab: false));

            StateHasChanged();
        });
    }

    private void HandleTableDialogRequested()
    {
        _dialog = new RichTextDialog(RichTextDialogKind.Table);
        StateHasChanged();
    }

    private void HandleVideoDialogRequested()
    {
        _dialog = new RichTextDialog(RichTextDialogKind.Video);
        StateHasChanged();
    }

    private void HandleImageDialogRequested()
    {
        _dialog = new RichTextDialog(RichTextDialogKind.Image);
        StateHasChanged();
    }

    private void HandleExplorerDialogRequested(ExplorerPluginOptions options)
    {
        _explorerOptions = options;
        _dialog = new RichTextDialog(RichTextDialogKind.Explorer);
        StateHasChanged();
    }

    private void HandleEmbedDialogRequested()
    {
        _dialog = new RichTextDialog(RichTextDialogKind.Embed);
        StateHasChanged();
    }

    private void CloseDialog()
        => _dialog = RichTextDialog.None;

    private async Task HandleLinkInsertedAsync(LinkDialogResult result)
    {
        CloseDialog();
        await ExecCommandAsync("createLink", result);
    }

    private async Task HandleTableInsertedAsync(TableDialogResult result)
    {
        CloseDialog();
        await ExecCommandAsync("insertTable", result);
    }

    private async Task HandleVideoInsertedAsync(VideoDialogResult result)
    {
        CloseDialog();
        await ExecCommandAsync("insertVideo", result);
    }

    private async Task HandleImageInsertedAsync(ImageDialogResult result)
    {
        CloseDialog();
        await ExecCommandAsync("insertImage", result);
    }

    private async Task HandleResourceInsertedAsync(ResourceDialogResult result)
    {
        CloseDialog();
        await ExecCommandAsync("insertResource", result);
    }

    private async Task HandleEmbedInsertedAsync(EmbedDialogResult result)
    {
        CloseDialog();
        await ExecCommandAsync("insertEmbed", result);
    }

    private async Task ExecCommandAsync(string command, object? value = null)
    {
        if (State is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return;
        }

        var result = await _engine.ExecAsync(command, value);
        if (result is not null)
        {
            var sanitized = State.Sanitizer.Sanitize(result.Html);
            _committedContent = sanitized;

            await Interaction.ChangeDocumentAsync(State.Document with { Content = sanitized });

            if (result.Selection is not null)
            {
                await HandleSelectionChangedAsync(result.Selection);
            }
        }
    }

    private void HandlePluginCommand(string command, object? value)
    {
        _ = InvokeAsync(async () =>
        {
            await ExecCommandAsync(command, value);
        });
    }

    private void UpdateBlockAnnouncement(string? blockType)
    {
        if (string.IsNullOrEmpty(blockType))
        {
            return;
        }

        _blockAnnouncement = blockType.ToUpperInvariant() switch
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
    public async ValueTask DisposeAsync()
    {
        // The canvas throws from OnInitialized without a cascaded plugin, and is disposed all the same.
        if (_plugin is not null)
        {
            _plugin.CommandRequested -= HandlePluginCommand;
            _plugin.LinkDialogRequested -= HandleLinkDialogRequested;
            _plugin.TableDialogRequested -= HandleTableDialogRequested;
            _plugin.VideoDialogRequested -= HandleVideoDialogRequested;
            _plugin.ImageDialogRequested -= HandleImageDialogRequested;
            _plugin.ExplorerDialogRequested -= HandleExplorerDialogRequested;
            _plugin.EmbedDialogRequested -= HandleEmbedDialogRequested;
        }

        _selfReference?.Dispose();
        await _engine.DisposeAsync();
    }
}
