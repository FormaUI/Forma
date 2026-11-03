namespace Forma.Blazor.Components.Composer.RichTextEditor;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;

/// <summary>The visual rich text editor for FormaComposer.</summary>
public sealed class RichTextEditorPlugin : IComposerEditorPlugin
{
    /// <summary>The identifier for the rich text editor plugin.</summary>
    public const string EditorId = "rich-text";

    /// <summary>Raised when a plugin requests execution of a rich text command on the active canvas, with the text, number or insertion it takes.</summary>
    public event Action<string, object?>? CommandRequested;

    /// <summary>Raised when a plugin requests opening the link or anchor dialog.</summary>
    public event Action<bool>? LinkDialogRequested;

    /// <summary>Raised when a plugin requests opening the table creation dialog.</summary>
    public event Action? TableDialogRequested;

    /// <summary>Raised when a plugin requests opening the video dialog.</summary>
    public event Action? VideoDialogRequested;

    /// <summary>Raised when a plugin requests opening the image insertion dialog.</summary>
    public event Action? ImageDialogRequested;

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
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type EditorComponent => typeof(FormaRichTextEditor);

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask<bool> TryInsertAsync(ComposerInsertion insertion, CancellationToken cancellationToken = default)
    {
        if (insertion is not ResourceInsertion resource)
        {
            return ValueTask.FromResult(false);
        }

        CommandRequested?.Invoke("insertResource", resource);

        return ValueTask.FromResult(true);
    }

    /// <summary>Requests execution of a rich text command on the active canvas, with the text it takes.</summary>
    public void RequestCommand(string command, string? value = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        CommandRequested?.Invoke(command, value);
    }

    /// <summary>Requests execution of a rich text command on the active canvas, with the number it takes.</summary>
    public void RequestCommand(string command, int value)
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

    /// <summary>Requests opening the video dialog.</summary>
    public void RequestVideoDialog()
        => VideoDialogRequested?.Invoke();

    /// <summary>Requests opening the image insertion dialog.</summary>
    public void RequestImageDialog()
        => ImageDialogRequested?.Invoke();

    /// <summary>Requests opening the embed insertion dialog.</summary>
    public void RequestEmbedDialog()
        => EmbedDialogRequested?.Invoke();
}
