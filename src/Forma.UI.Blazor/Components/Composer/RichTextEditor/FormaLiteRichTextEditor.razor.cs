namespace Forma.UI.Blazor.Components.Composer.RichTextEditor;

using System.Diagnostics.CodeAnalysis;

using Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>Lite rich text editor preset providing basic formatting, alignment, and typography.</summary>
public sealed partial class FormaLiteRichTextEditor : InputBase<ComposerDocument>
{
    private readonly RichTextEditorPlugin _editorPlugin = new();
    private readonly IReadOnlyList<IComposerPlugin> _plugins;

    /// <summary>Options configuring the underlying composer instance.</summary>
    [Parameter] public FormaComposerOptions Options { get; set; } = FormaComposerOptions.Default;

    /// <summary>Whether the editor is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the editor is in a read-only state.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Constructs a lite rich text editor preset with its pre-configured plugin set.</summary>
    public FormaLiteRichTextEditor()
    {
        _plugins =
        [
            _editorPlugin,
            new TypographyPlugin(_editorPlugin),
            new BasicFormattingPlugin(_editorPlugin),
            new AlignmentBidiPlugin(_editorPlugin),
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
