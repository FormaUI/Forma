namespace Forma.UI.Blazor.Components.Composer.PlainTextEditor;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Style.Glyphs;

/// <summary>Proof-of-routing and fallback editor plugin providing a plain text editing surface.</summary>
public sealed class PlainTextEditorPlugin : IComposerEditorPlugin
{
    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets
        => [];

    /// <inheritdoc />
    public Type EditorComponent
        => typeof(FormaPlainTextEditor);

    /// <inheritdoc />
    public ComposerDocumentFormat Format
        => ComposerDocumentFormat.PlainText;

    /// <inheritdoc />
    public Glyph Icon
        => Glyph.FileLines;

    /// <inheritdoc />
    public string Id
        => "plain-text";

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers
        => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings
        => [];

    /// <inheritdoc />
    public string Title
        => "Plain Text";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items
        => [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);
}
