namespace Forma.Tests.Composer;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;

/// <summary>A stub editor plugin that is always available.</summary>
public sealed class StubEditorPlugin(string id = "rich-text", ComposerDocumentFormat? format = null) : IComposerEditorPlugin
{
    /// <inheritdoc />
    public string Id { get; } = id;

    /// <inheritdoc />
    public string Title => "Stub";

    /// <inheritdoc />
    public Icon Icon => Glyph.File;

    /// <inheritdoc />
    public ComposerDocumentFormat Format { get; } = format ?? ComposerDocumentFormat.Html;

    /// <inheritdoc />
    public Type EditorComponent => typeof(StubEditorPlugin);

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);
}
