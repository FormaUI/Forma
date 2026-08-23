namespace Forma.UI.Blazor.Components.Composer.RichTextEditor;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Style.Glyphs;

/// <summary>Rich text visual authoring surface for FormaComposer.</summary>
public sealed class RichTextEditorPlugin : IComposerEditorPlugin
{
    /// <summary>The identifier for the rich text editor plugin.</summary>
    public const string EditorId = "rich-text";

    /// <summary>Raised when a plugin requests execution of a rich text command on the active surface.</summary>
    public event Action<string, object?>? CommandRequested;

    /// <inheritdoc />
    public string Id => EditorId;

    /// <inheritdoc />
    public string Title => "Rich Text";

    /// <inheritdoc />
    public Glyph Icon => Glyph.PenToSquare;

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
}
