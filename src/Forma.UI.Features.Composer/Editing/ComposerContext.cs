namespace Forma.UI.Features.Composer.Editing;

using Forma.Framework.Sanitization;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
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
        IHtmlSanitizer sanitizer,
        IComposerAssetLoader assetLoader,
        IComposerRegionRegistry regions,
        Func<ComposerDocument, Task> changeDocumentAsync,
        Func<Task> requestRenderAsync,
        Func<ComposerSelection, Task> reportSelectionAsync,
        Func<string, Task> executeCommandAsync,
        IComposerSyntaxHighlighter? syntaxHighlighter = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activeEditorId);
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(selection);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(sanitizer);
        ArgumentNullException.ThrowIfNull(assetLoader);
        ArgumentNullException.ThrowIfNull(regions);
        ArgumentNullException.ThrowIfNull(changeDocumentAsync);
        ArgumentNullException.ThrowIfNull(requestRenderAsync);
        ArgumentNullException.ThrowIfNull(reportSelectionAsync);
        ArgumentNullException.ThrowIfNull(executeCommandAsync);

        ActiveEditorId = activeEditorId;
        Document = document;
        Selection = selection;
        History = history;
        Sanitizer = sanitizer;
        AssetLoader = assetLoader;
        Regions = regions;
        ChangeDocumentAsync = changeDocumentAsync;
        RequestRenderAsync = requestRenderAsync;
        ReportSelectionAsync = reportSelectionAsync;
        ExecuteCommandAsync = executeCommandAsync;
        SyntaxHighlighter = syntaxHighlighter;
    }

    /// <summary>The identifier of the editor plugin currently rendering.</summary>
    public string ActiveEditorId { get; }

    /// <summary>The document as the composer currently holds it.</summary>
    public ComposerDocument Document { get; }

    /// <summary>The caret and range as the browser last reported them.</summary>
    public ComposerSelection Selection { get; }

    /// <summary>Whether the composer takes no interaction at all, which an editor renders as a disabled control.</summary>
    public bool IsDisabled { get; init; }

    /// <summary>Whether the document may be read but not edited.</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>Whether the composer currently fills the viewport.</summary>
    public bool IsMaximized { get; init; }

    /// <summary>The undo and redo stack shared by every editor.</summary>
    public IComposerHistory History { get; }

    /// <summary>The sanitizer every path into and out of the composer runs through.</summary>
    public IHtmlSanitizer Sanitizer { get; }

    /// <summary>The loader for the browser assets plugins declare.</summary>
    public IComposerAssetLoader AssetLoader { get; }

    /// <summary>The status items and gutter markers plugins have pushed, shared by every editor.</summary>
    public IComposerRegionRegistry Regions { get; }

    /// <summary>The registered highlighter, or null when code renders unhighlighted.</summary>
    public IComposerSyntaxHighlighter? SyntaxHighlighter { get; }

    /// <summary>Reports a document the editor has changed, so the engine can record, sanitize and raise it.</summary>
    public Func<ComposerDocument, Task> ChangeDocumentAsync { get; }

    /// <summary>Asks the composer shell to re-render.</summary>
    public Func<Task> RequestRenderAsync { get; }

    /// <summary>Reports the caret or range an editor last observed, so the engine can share it with every plugin.</summary>
    public Func<ComposerSelection, Task> ReportSelectionAsync { get; }

    /// <summary>Runs a named command through the engine, which resolves it against the engine's own commands and then the registered plugins.</summary>
    public Func<string, Task> ExecuteCommandAsync { get; }
}
