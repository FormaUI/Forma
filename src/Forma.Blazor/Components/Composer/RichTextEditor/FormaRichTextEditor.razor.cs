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
    /// <summary>The shared composer context cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

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

    private bool _isLinkDialogOpen;
    private bool _isLinkAnchorMode;
    private ComposerDialogResult? _reopening;
    private bool _isTableDialogOpen;
    private bool _isVideoDialogOpen;
    private bool _isImageDialogOpen;
    private bool _isExplorerDialogOpen;
    private ExplorerPluginOptions _explorerOptions = ExplorerPluginOptions.Default;
    private bool _isEmbedDialogOpen;

    private RichTextEditorPlugin _plugin = default!;

    private bool IsDisabled
        => Context?.IsDisabled ?? false;

    private bool IsReadOnly
        => Context?.IsReadOnly ?? false;

    private bool ContentEditable
        => !IsDisabled && !IsReadOnly;

    private bool IsSelectionBarShown
        => Context?.IsSelectionBarShown ?? true;

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
        var documentContent = Context?.Document.Content;

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

            var initialContent = Context?.Document.Content;
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
        if (Context is null)
        {
            return;
        }

        var sanitized = Context.Sanitizer.Sanitize(html);
        _committedContent = sanitized;

        await Context.ChangeDocumentAsync(Context.Document with { Content = sanitized });
    }

    /// <summary>Callback invoked from JS when the selection snapshot changes.</summary>
    [JSInvokable]
    public async Task HandleSelectionChangedAsync(RichTextSelectionDto dto)
    {
        if (Context is null || dto is null)
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

        await Context.ReportSelectionAsync(selection);
    }

    /// <summary>Callback invoked from JS when the author double-clicks an image already in the document.</summary>
    /// <param name="image">What that image already holds.</param>
    [JSInvokable]
    public Task HandleImageActivatedAsync(ImageDialogResult image)
        => ReopenAsync(image, () => _isImageDialogOpen = true);

    /// <summary>Callback invoked from JS when the author double-clicks a video already in the document.</summary>
    /// <param name="video">What that video already holds.</param>
    [JSInvokable]
    public Task HandleVideoActivatedAsync(VideoDialogResult video)
        => ReopenAsync(video, () => _isVideoDialogOpen = true);

    /// <summary>Callback invoked from JS when the author double-clicks an embed already in the document.</summary>
    /// <param name="embed">What that embed already holds.</param>
    [JSInvokable]
    public Task HandleEmbedActivatedAsync(EmbedDialogResult embed)
        => ReopenAsync(embed, () => _isEmbedDialogOpen = true);

    /// <summary>Callback invoked from JS when the author double-clicks a link or a named anchor.</summary>
    /// <param name="isAnchor">Whether it is a named anchor rather than a hyperlink.</param>
    [JSInvokable]
    public Task HandleLinkActivatedAsync(bool isAnchor)
        // A link dialog fills its fields from the canvas, whichever way it was asked for.
        => ReopenAsync(null, () => HandleLinkDialogRequested(isAnchor));

    // The canvas has already selected the element whole, so confirming replaces it.
    private Task ReopenAsync(ComposerDialogResult? reopening, Action open)
    {
        if (Context is null || IsDisabled || IsReadOnly)
        {
            return Task.CompletedTask;
        }

        _reopening = reopening;
        open();
        StateHasChanged();

        return Task.CompletedTask;
    }

    /// <summary>Handles commands raised directly by UI elements like the selection bar.</summary>
    public void HandleCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        if (Context is null || IsDisabled || IsReadOnly)
        {
            return;
        }

        // These are plugin command names, so they route through the engine registry rather than straight at the canvas.
        _ = Context.ExecuteCommandAsync(command);
    }

    private void HandleLinkDialogRequested(bool isAnchor)
    {
        _isLinkAnchorMode = isAnchor;

        _ = InvokeAsync(async () =>
        {
            var target = await _engine.BeginLinkDialogAsync(isAnchor);

            // One link is being retargeted, so its address comes back; several share none, and the author names it.
            _reopening = new LinkDialogResult(
                isAnchor,
                target?.Text ?? string.Empty,
                target?.Count == 1 ? target.Href ?? string.Empty : string.Empty,
                OpenInNewTab: false);

            _isLinkDialogOpen = true;
            StateHasChanged();
        });
    }

    private void HandleTableDialogRequested()
    {
        _isTableDialogOpen = true;
        StateHasChanged();
    }

    private void HandleVideoDialogRequested()
    {
        // Asked for from the toolbar, so nothing is being reopened.
        _reopening = null;
        _isVideoDialogOpen = true;
        StateHasChanged();
    }

    private void HandleImageDialogRequested()
    {
        _reopening = null;
        _isImageDialogOpen = true;
        StateHasChanged();
    }

    private void HandleExplorerDialogRequested(ExplorerPluginOptions options)
    {
        _explorerOptions = options;
        _isExplorerDialogOpen = true;
        StateHasChanged();
    }

    private void HandleEmbedDialogRequested()
    {
        _reopening = null;
        _isEmbedDialogOpen = true;
        StateHasChanged();
    }

    private async Task HandleLinkInsertedAsync(LinkDialogResult result)
    {
        _isLinkDialogOpen = false;
        await ExecCommandAsync("createLink", result);
    }

    private async Task HandleTableInsertedAsync(TableDialogResult result)
    {
        _isTableDialogOpen = false;
        await ExecCommandAsync("insertTable", result);
    }

    private async Task HandleVideoInsertedAsync(VideoDialogResult result)
    {
        _isVideoDialogOpen = false;
        await ExecCommandAsync("insertVideo", result);
    }

    private async Task HandleImageInsertedAsync(ImageDialogResult result)
    {
        _isImageDialogOpen = false;
        await ExecCommandAsync("insertImage", result);
    }

    private async Task HandleResourceInsertedAsync(ResourceDialogResult result)
    {
        _isExplorerDialogOpen = false;
        await ExecCommandAsync("insertResource", result);
    }

    private async Task HandleEmbedInsertedAsync(EmbedDialogResult result)
    {
        _isEmbedDialogOpen = false;
        await ExecCommandAsync("insertEmbed", result);
    }

    private async Task ExecCommandAsync(string command, object? value = null)
    {
        if (Context is null || IsDisabled || IsReadOnly)
        {
            return;
        }

        var result = await _engine.ExecAsync(command, value);
        if (result is not null)
        {
            var sanitized = Context.Sanitizer.Sanitize(result.Html);
            _committedContent = sanitized;

            await Context.ChangeDocumentAsync(Context.Document with { Content = sanitized });

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
