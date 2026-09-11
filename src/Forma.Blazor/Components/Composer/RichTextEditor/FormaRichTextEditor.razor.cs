namespace Forma.Blazor.Components.Composer.RichTextEditor;

using System.Globalization;

using Forma.Blazor.Components.Composer.Dialogs;
using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Interop;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The visual rich text canvas, filling the surface the engine gives its editor.</summary>
public sealed partial class FormaRichTextEditor : FormaComponentBase
{
    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? ComposerContext { get; set; }

    [CascadingParameter] public IComposerInteraction? Interaction { get; set; }

    /// <summary>The editor plugin cascaded by the composer surface.</summary>
    [CascadingParameter] public IComposerEditorPlugin? Editor { get; set; }

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private ElementReference _editorElement;
    private DotNetObjectReference<FormaRichTextEditor>? _selfReference;
    private RichTextCanvas _canvas = default!;
    private InputScope? _inputScope;
    private string? _committedContent;
    private string _blockAnnouncement = string.Empty;
    private bool _isAttached;

    private RichTextDialogState _dialog = RichTextDialogState.None;
    private bool _isLinkAnchorMode;
    private ExplorerPluginOptions _explorerOptions = ExplorerPluginOptions.Default;

    private RichTextEditorPlugin _editorPlugin = default!;

    private bool IsDisabled
        => ComposerContext?.IsDisabled ?? false;

    private bool IsReadOnly
        => ComposerContext?.IsReadOnly ?? false;

    private bool ContentEditable
        => !IsDisabled && !IsReadOnly;

    private bool IsSelectionBarShown
        => ComposerContext?.IsSelectionBarShown ?? true;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _canvas = new RichTextCanvas(JS);
        _selfReference = DotNetObjectReference.Create(this);

        _editorPlugin = Editor as RichTextEditorPlugin
            ?? throw new InvalidOperationException($"{nameof(FormaRichTextEditor)} requires a cascaded {nameof(RichTextEditorPlugin)}.");

        _editorPlugin.CommandRequested += HandlePluginCommand;
        _editorPlugin.LinkDialogRequested += HandleLinkDialogRequested;
        _editorPlugin.TableDialogRequested += HandleTableDialogRequested;
        _editorPlugin.VideoDialogRequested += HandleVideoDialogRequested;
        _editorPlugin.ImageDialogRequested += HandleImageDialogRequested;
        _editorPlugin.ExplorerDialogRequested += HandleExplorerDialogRequested;
        _editorPlugin.EmbedDialogRequested += HandleEmbedDialogRequested;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var documentContent = ComposerContext?.Document.Content;

        if (_isAttached && documentContent != _committedContent && documentContent is not null)
        {
            _committedContent = documentContent;
            await _canvas.SetHtmlAsync(documentContent);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isAttached = await _canvas.AttachAsync(_editorElement, _selfReference!);

            // An editable claims its shortcuts natively, so the composer's routes are registered on it to be answered first.
            if (ComposerContext is { } context)
            {
                _inputScope = await InputRouter.RegisterAsync(_editorElement, new InputProfile { Routes = context.KeyBindings.ToInputRoutes() }, _selfReference);
            }

            var initialContent = ComposerContext?.Document.Content;
            if (!string.IsNullOrEmpty(initialContent))
            {
                _committedContent = initialContent;
                await _canvas.SetHtmlAsync(initialContent);
            }

            StateHasChanged();
        }
    }

    /// <summary>Runs the composer command whose shortcut the router matched inside the canvas; a command is its own action.</summary>
    [JSInvokable]
    public Task HandleInputActionAsync(string action, InputInfo input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return Interaction?.ExecuteCommandAsync(action) ?? Task.CompletedTask;
    }

    /// <summary>Callback invoked from JS when content changes via debounced typing or blur.</summary>
    [JSInvokable]
    public async Task HandleContentChangedAsync(string html)
    {
        if (ComposerContext is null || Interaction is null)
        {
            return;
        }

        var sanitized = ComposerContext.Sanitizer.Sanitize(html);
        _committedContent = sanitized;

        await Interaction.ChangeDocumentAsync(ComposerContext.Document with { Content = sanitized });
    }

    /// <summary>Callback invoked from JS when the selection snapshot changes.</summary>
    [JSInvokable]
    public async Task HandleSelectionChangedAsync(RichTextSelectionDto dto)
    {
        if (ComposerContext is null || Interaction is null || dto is null)
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
        => ReopenAsync(new RichTextDialogState(RichTextDialogKind.Image, image));

    /// <summary>Callback invoked from JS when the author double-clicks a video already in the document.</summary>
    /// <param name="video">What that video already holds.</param>
    [JSInvokable]
    public Task HandleVideoActivatedAsync(VideoDialogResult video)
        => ReopenAsync(new RichTextDialogState(RichTextDialogKind.Video, video));

    /// <summary>Callback invoked from JS when the author double-clicks an embed already in the document.</summary>
    /// <param name="embed">What that embed already holds.</param>
    [JSInvokable]
    public Task HandleEmbedActivatedAsync(EmbedDialogResult embed)
        => ReopenAsync(new RichTextDialogState(RichTextDialogKind.Embed, embed));

    /// <summary>Callback invoked from JS when the author double-clicks a link or a named anchor.</summary>
    /// <param name="isAnchor">Whether it is a named anchor rather than a hyperlink.</param>
    [JSInvokable]
    public Task HandleLinkActivatedAsync(bool isAnchor)
    {
        if (ComposerContext is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return Task.CompletedTask;
        }

        // A link dialog fills its fields from the canvas, whichever way it was asked for.
        HandleLinkDialogRequested(isAnchor);

        return Task.CompletedTask;
    }

    // The canvas has already selected the element whole, so confirming replaces it.
    private Task ReopenAsync(RichTextDialogState dialog)
    {
        if (ComposerContext is null || Interaction is null || IsDisabled || IsReadOnly)
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

        if (ComposerContext is null || Interaction is null || IsDisabled || IsReadOnly)
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
            var target = await _canvas.BeginLinkDialogAsync(isAnchor);

            // One link is being retargeted, so its address comes back; several share none, and the author names it.
            _dialog = new RichTextDialogState(
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

    private void HandleExplorerDialogRequested(ExplorerPluginOptions options)
    {
        _explorerOptions = options;
        _dialog = new RichTextDialogState(RichTextDialogKind.Explorer);
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

    private async Task HandleResourceInsertedAsync(ResourceDialogResult result)
    {
        CloseDialog();
        await ExecuteCommandAsync("insertResource", result);
    }

    private async Task HandleEmbedInsertedAsync(EmbedDialogResult result)
    {
        CloseDialog();
        await ExecuteCommandAsync("insertEmbed", result);
    }

    private async Task ExecuteCommandAsync(string command, object? value = null)
    {
        if (ComposerContext is null || Interaction is null || IsDisabled || IsReadOnly)
        {
            return;
        }

        var result = await _canvas.ExecuteCommandAsync(command, value);
        if (result is not null)
        {
            var sanitized = ComposerContext.Sanitizer.Sanitize(result.Html);
            _committedContent = sanitized;

            await Interaction.ChangeDocumentAsync(ComposerContext.Document with { Content = sanitized });

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
            await ExecuteCommandAsync(command, value);
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
    protected override async ValueTask DisposeAsyncCore()
    {
        // The canvas throws from OnInitialized without a cascaded plugin, and is disposed all the same.
        if (_editorPlugin is not null)
        {
            _editorPlugin.CommandRequested -= HandlePluginCommand;
            _editorPlugin.LinkDialogRequested -= HandleLinkDialogRequested;
            _editorPlugin.TableDialogRequested -= HandleTableDialogRequested;
            _editorPlugin.VideoDialogRequested -= HandleVideoDialogRequested;
            _editorPlugin.ImageDialogRequested -= HandleImageDialogRequested;
            _editorPlugin.ExplorerDialogRequested -= HandleExplorerDialogRequested;
            _editorPlugin.EmbedDialogRequested -= HandleEmbedDialogRequested;
        }

        if (_inputScope is { } inputScope)
        {
            _inputScope = null;
            await inputScope.DisposeAsync();
        }

        _selfReference?.Dispose();
        await _canvas.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
