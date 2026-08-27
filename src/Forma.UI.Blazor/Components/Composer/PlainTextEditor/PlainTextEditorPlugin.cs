namespace Forma.UI.Blazor.Components.Composer.PlainTextEditor;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Plugins;

/// <summary>Proof-of-routing and fallback editor plugin providing a plain text editing surface.</summary>
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
