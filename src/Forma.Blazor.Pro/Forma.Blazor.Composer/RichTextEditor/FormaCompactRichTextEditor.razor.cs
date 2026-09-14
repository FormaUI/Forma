namespace Forma.Blazor.Components.Composer.RichTextEditor;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>Compact rich text editor preset providing basic formatting, alignment, and typography.</summary>
public sealed partial class FormaCompactRichTextEditor : FormaInputBase<ComposerDocument>
{
    private readonly RichTextEditorPlugin _editorPlugin = new();
    private readonly IReadOnlyList<IComposerPlugin> _plugins;

    /// <summary>Options configuring the underlying composer instance.</summary>
    [Parameter] public ComposerOptions Options { get; set; } = ComposerOptions.Default;

    /// <summary>Whether the editor is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the editor is in a read-only state.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    private FormaComposer Composer { get; set; } = default!;

    /// <inheritdoc />
    public override ElementReference RootElement
        => Composer is null ? default : Composer.RootElement;

    /// <summary>Constructs a compact rich text editor preset with its pre-configured plugin set.</summary>
    public FormaCompactRichTextEditor()
    {
        _plugins =
        [
            _editorPlugin,
            new TypographyPlugin(_editorPlugin),
            new BasicFormattingPlugin(_editorPlugin),
            new AlignmentBidiPlugin(_editorPlugin),
            new LinkPlugin(_editorPlugin),
        ];
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(
        string? value,
        [MaybeNullWhen(false)] out ComposerDocument result,
        [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = new ComposerDocument(ComposerDocumentFormat.Html, value ?? string.Empty);
        validationErrorMessage = null;
        return true;
    }
}
