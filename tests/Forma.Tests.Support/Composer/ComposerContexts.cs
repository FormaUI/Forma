namespace Forma.Tests.Composer;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Composer.Syntax;
using Forma.Framework.Sanitization;

/// <summary>Builds a <see cref="ComposerContext"/> over a fixed document.</summary>
public static class ComposerContexts
{
    /// <summary>A composer context over the given markup, with the shipped implementation of every service.</summary>
    /// <param name="html">The markup the document holds.</param>
    /// <param name="selection">The selection the browser last reported.</param>
    /// <param name="activeEditor">The active editor, where null stands for a <see cref="StubEditorPlugin"/>.</param>
    public static ComposerContext Create(
        string html = "<p>Test</p>",
        ComposerSelection? selection = null,
        IComposerEditorPlugin? activeEditor = null)
        => new()
        {
            ActiveEditor = activeEditor ?? new StubEditorPlugin(),
            Document = new ComposerDocument(ComposerDocumentFormat.Html, html),
            Selection = selection ?? ComposerSelection.Empty,
            History = new ComposerHistory(),
            Sanitizer = new AllowlistHtmlSanitizer(),
            AssetLoader = new NullAssetLoader(),
            Regions = new ComposerRegionRegistry(),
            SyntaxHighlighter = NullSyntaxHighlighter.Instance,
        };
}
