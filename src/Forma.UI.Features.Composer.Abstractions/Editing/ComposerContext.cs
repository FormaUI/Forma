namespace Forma.UI.Features.Composer.Editing;

using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Sanitization;
using Forma.UI.Features.Composer.Syntax;

/// <summary>The composer state and engine services cascaded to every plugin component.</summary>
public sealed class ComposerContext
{
    /// <summary>Creates a context over the active document, selection and engine services.</summary>
    public ComposerContext(
        string activeEditorId,
        ComposerDocument document,
        ComposerSelection selection,
        IComposerHistory history,
        IComposerHtmlSanitizer sanitizer,
        IComposerAssetLoader assetLoader,
        Func<Task> requestRenderAsync,
        IComposerSyntaxHighlighter? syntaxHighlighter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activeEditorId);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(sanitizer);
        ArgumentNullException.ThrowIfNull(assetLoader);
        ArgumentNullException.ThrowIfNull(requestRenderAsync);

        ActiveEditorId = activeEditorId;
        Document = document;
        Selection = selection;
        History = history;
        Sanitizer = sanitizer;
        AssetLoader = assetLoader;
        RequestRenderAsync = requestRenderAsync;
        SyntaxHighlighter = syntaxHighlighter;
    }

    /// <summary>The identifier of the editor plugin currently rendering.</summary>
    public string ActiveEditorId { get; }

    /// <summary>The document as the composer currently holds it.</summary>
    public ComposerDocument Document { get; }

    /// <summary>The caret and range as the browser last reported them.</summary>
    public ComposerSelection Selection { get; }

    /// <summary>The undo and redo stack shared by every editor.</summary>
    public IComposerHistory History { get; }

    /// <summary>The sanitizer every path into and out of the composer runs through.</summary>
    public IComposerHtmlSanitizer Sanitizer { get; }

    /// <summary>The loader for the browser assets plugins declare.</summary>
    public IComposerAssetLoader AssetLoader { get; }

    /// <summary>The registered highlighter, or null when code renders unhighlighted.</summary>
    public IComposerSyntaxHighlighter? SyntaxHighlighter { get; }

    /// <summary>Asks the composer shell to re-render.</summary>
    public Func<Task> RequestRenderAsync { get; }
}
