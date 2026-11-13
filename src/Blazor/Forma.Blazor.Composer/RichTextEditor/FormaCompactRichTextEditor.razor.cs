namespace Forma.Blazor.Components.Composer.RichTextEditor;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Composer.RichTextEditor.Plugins;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;

using Microsoft.AspNetCore.Components;

/// <summary>Compact rich text editor preset providing typography, basic formatting, alignment, and links.</summary>
public sealed partial class FormaCompactRichTextEditor : FormaInputBase<ComposerDocument>
{
    // A definition, from which each composer rendering this preset builds its own plugins.
    private static readonly ComposerDefinition Preset = ComposerDefinition.Create(composer => composer
        .AddRichTextEditor(editor => editor
            .AddFeaturePlugin<TypographyPlugin>()
            .AddFeaturePlugin<BasicFormattingPlugin>()
            .AddFeaturePlugin<AlignmentBidiPlugin>()
            .AddFeaturePlugin<LinkPlugin>()));

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

    /// <inheritdoc />
    protected override bool CanFocus
        => !Disabled && base.CanFocus;

    /// <inheritdoc />
    protected override ValueTask<bool> FocusControlAsync(bool preventScroll)
        => Composer is null ? ValueTask.FromResult(false) : Composer.FocusAsync(preventScroll);

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
