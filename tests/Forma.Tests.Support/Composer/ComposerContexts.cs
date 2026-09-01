namespace Forma.Tests.Composer;

using Forma.Features.Composer.Documents;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Syntax;
using Forma.Features.Sanitization;

/// <summary>Builds a <see cref="ComposerContext"/> over a fixed document, for exercising a plugin without a composer.</summary>
public static class ComposerContexts
{
    /// <summary>A state over the given markup, defaulting every service to the shipped implementation.</summary>
    public static ComposerContext Create(string html = "<p>Test</p>", ComposerSelection? selection = null)
        => new()
        {
            ActiveEditorId = "rich-text",
            Document = new ComposerDocument(ComposerDocumentFormat.Html, html),
            Selection = selection ?? ComposerSelection.Empty,
            History = new ComposerHistory(),
            Sanitizer = new AllowlistHtmlSanitizer(),
            AssetLoader = new NullAssetLoader(),
            Regions = new ComposerRegionRegistry(),
            SyntaxHighlighter = NullSyntaxHighlighter.Instance,
        };
}
