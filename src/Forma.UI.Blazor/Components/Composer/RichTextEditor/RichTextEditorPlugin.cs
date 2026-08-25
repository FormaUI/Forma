namespace Forma.UI.Blazor.Components.Composer.RichTextEditor;

using Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Style.Icons;

/// <summary>Rich text visual authoring surface for FormaComposer.</summary>
public sealed class RichTextEditorPlugin : IComposerEditorPlugin
{
    /// <summary>The identifier for the rich text editor plugin.</summary>
    public const string EditorId = "rich-text";

    /// <summary>Raised when a plugin requests execution of a rich text command on the active surface.</summary>
    public event Action<string, object?>? CommandRequested;

    /// <summary>Raised when a plugin requests opening the link or anchor dialog.</summary>
    public event Action<bool>? LinkDialogRequested;

    /// <summary>Raised when a plugin requests opening the table creation dialog.</summary>
    public event Action? TableDialogRequested;

    /// <summary>Raised when a plugin requests opening the video embed dialog.</summary>
    public event Action? VideoDialogRequested;

    /// <summary>Raised when a plugin requests opening the image insertion dialog.</summary>
    public event Action? ImageDialogRequested;

    /// <summary>Raised when a plugin requests opening the stored files dialog, carrying what it browses and inserts.</summary>
    public event Action<ExplorerPluginOptions>? ExplorerDialogRequested;

    /// <summary>Raised when a plugin requests opening the embed dialog.</summary>
    public event Action? EmbedDialogRequested;

    /// <inheritdoc />
    public string Id => EditorId;

    /// <inheritdoc />
    public string Title => "Rich Text";

    /// <inheritdoc />
    public Icon Icon => Glyph.Edit;

    /// <inheritdoc />
    public ComposerDocumentFormat Format => ComposerDocumentFormat.Html;

    /// <inheritdoc />
    public Type EditorComponent => typeof(FormaRichTextEditor);

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarRow> Rows => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets => [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <summary>Requests execution of a rich text command on the active surface.</summary>
    public void RequestCommand(string command, object? value = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        CommandRequested?.Invoke(command, value);
    }

    /// <summary>Requests opening the link or anchor dialog.</summary>
    public void RequestLinkDialog(bool isAnchor = false)
        => LinkDialogRequested?.Invoke(isAnchor);

    /// <summary>Requests opening the table creation dialog.</summary>
    public void RequestTableDialog()
        => TableDialogRequested?.Invoke();

    /// <summary>Requests opening the video embed dialog.</summary>
    public void RequestVideoDialog()
        => VideoDialogRequested?.Invoke();

    /// <summary>Requests opening the image insertion dialog.</summary>
    public void RequestImageDialog()
        => ImageDialogRequested?.Invoke();

    /// <summary>Requests opening the stored files dialog.</summary>
    public void RequestExplorerDialog(ExplorerPluginOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ExplorerDialogRequested?.Invoke(options);
    }

    /// <summary>Requests opening the embed insertion dialog.</summary>
    public void RequestEmbedDialog()
        => EmbedDialogRequested?.Invoke();
}
