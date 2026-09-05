namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Syntax;
using Forma.Framework.Sanitization;

/// <summary>The composer as a plugin reads it: the active document, the selection, and the live engine services beside them.</summary>
public sealed record ComposerContext
{
    /// <summary>The identifier of the editor plugin currently rendering.</summary>
    public required string ActiveEditorId { get; init; }

    /// <summary>The document as the composer currently holds it.</summary>
    public required ComposerDocument Document { get; init; }

    /// <summary>The caret and range as the browser last reported them.</summary>
    public required ComposerSelection Selection { get; init; }

    /// <summary>Whether the composer takes no interaction at all, which an editor renders as a disabled control.</summary>
    public bool IsDisabled { get; init; }

    /// <summary>Whether the document may be read but not edited.</summary>
    public bool IsReadOnly { get; init; }

    /// <summary>Whether the composer currently fills the viewport.</summary>
    public bool IsMaximized { get; init; }

    /// <summary>Whether the formatting bar that follows the text selection is shown.</summary>
    public bool IsSelectionBarShown { get; init; } = true;

    /// <summary>The undo and redo stack shared by every editor.</summary>
    public required IComposerHistory History { get; init; }

    /// <summary>The sanitizer every path into and out of the composer runs through.</summary>
    public required IHtmlSanitizer Sanitizer { get; init; }

    /// <summary>The loader for the browser assets plugins declare.</summary>
    public required IComposerAssetLoader AssetLoader { get; init; }

    /// <summary>The status items and gutter markers plugins have pushed, shared by every editor.</summary>
    public required IComposerRegionRegistry Regions { get; init; }

    /// <summary>The highlighter code classification runs through.</summary>
    public required IComposerSyntaxHighlighter SyntaxHighlighter { get; init; }

    /// <summary>The shortcuts the composer and its plugins answer, which an editor registers on its own editable element.</summary>
    public ComposerKeyBindingCatalog KeyBindings { get; init; } = ComposerKeyBindingCatalog.Empty;
}
