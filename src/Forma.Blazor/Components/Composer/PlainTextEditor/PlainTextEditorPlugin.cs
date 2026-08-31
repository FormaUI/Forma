namespace Forma.Blazor.Components.Composer.PlainTextEditor;

using Forma.Features.Composer.Assets;
using Forma.Features.Composer.Documents;
using Forma.Features.Composer.Plugins;
using Forma.Framework.Icons;

/// <summary>Proof-of-routing and fallback editor plugin, authoring plain text.</summary>
public sealed class PlainTextEditorPlugin : IComposerEditorPlugin
{
    /// <inheritdoc />
    public Type EditorComponent
        => typeof(FormaPlainTextEditor);

    /// <inheritdoc />
    public ComposerDocumentFormat Format
        => ComposerDocumentFormat.PlainText;

    /// <inheritdoc />
    public Icon Icon
        => Glyph.FileText;

    /// <inheritdoc />
    public string Id
        => "plain-text";

    /// <inheritdoc />
    public string Title
        => "Plain Text";

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);
}
