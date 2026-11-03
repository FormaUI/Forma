namespace Forma.Blazor.Components.Composer.PlainTextEditor;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;

/// <summary>The editor plugin that authors plain text, and the editor a composer falls back to.</summary>
public sealed class PlainTextEditorPlugin : IComposerEditorPlugin
{
    /// <inheritdoc />
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
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
