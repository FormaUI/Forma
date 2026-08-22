namespace Forma.UI.Blazor.Components.Composer.RichTextEditor;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Interop.Components;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Visual rich text editing canvas for FormaComposer.</summary>
public sealed partial class FormaRichTextEditor : ComponentBase, IAsyncDisposable
{
    /// <summary>The shared composer context cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    /// <summary>The editor plugin cascaded by the composer surface.</summary>
    [CascadingParameter] public IComposerEditorPlugin? Editor { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private ElementReference _editorElement;
    private DotNetObjectReference<FormaRichTextEditor>? _dotNetRef;
    private RichTextEditor _interop = default!;
    private string? _committedContent;
    private string _blockAnnouncement = string.Empty;
    private bool _isAttached;

    private RichTextEditorPlugin _plugin = default!;

    private bool IsDisabled
        => Context?.IsDisabled ?? false;

    private bool IsReadOnly
        => Context?.IsReadOnly ?? false;

    private bool ContentEditable
        => !IsDisabled && !IsReadOnly;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _interop = new RichTextEditor(JS);
        _dotNetRef = DotNetObjectReference.Create(this);

        _plugin = Editor as RichTextEditorPlugin
            ?? throw new InvalidOperationException($"{nameof(FormaRichTextEditor)} requires a cascaded {nameof(RichTextEditorPlugin)}.");

        _plugin.CommandRequested += HandlePluginCommand;
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        var documentContent = Context?.Document.Content;

        if (_isAttached && documentContent != _committedContent && documentContent is not null)
        {
            _committedContent = documentContent;
            await _interop.SetHtmlAsync(documentContent);
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _isAttached = await _interop.AttachAsync(_editorElement, _dotNetRef!);

            var initialContent = Context?.Document.Content;
            if (!string.IsNullOrEmpty(initialContent))
            {
                _committedContent = initialContent;
                await _interop.SetHtmlAsync(initialContent);
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

    /// <summary>Handles commands raised directly by UI elements like the bubble menu.</summary>
    public void HandleCommand(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        if (Context is null || IsDisabled || IsReadOnly)
        {
            return;
        }

        _plugin.RequestCommand(command);
    }

    private void HandlePluginCommand(string command, object? value)
    {
        _ = InvokeAsync(async () =>
        {
            if (Context is null || IsDisabled || IsReadOnly)
            {
                return;
            }

            var result = await _interop.ExecAsync(command, value);
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
        // The surface throws from OnInitialized without a cascaded plugin, and is disposed all the same.
        _plugin?.CommandRequested -= HandlePluginCommand;

        _dotNetRef?.Dispose();
        await _interop.DisposeAsync();
    }
}
