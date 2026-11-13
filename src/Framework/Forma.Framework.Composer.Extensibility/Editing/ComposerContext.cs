namespace Forma.Framework.Composer.Editing;

using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Composer.Syntax;
using Forma.Framework.Sanitization;

/// <summary>The composer as a plugin reads it: the active document, the selection, and the live engine services beside them.</summary>
public sealed record ComposerContext
{
    /// <summary>The editor plugin currently rendering.</summary>
    public required IComposerEditorPlugin ActiveEditor { get; init; }

    /// <summary>The view currently showing over the active editor's document, where null is the editor itself.</summary>
    public IComposerViewPlugin? ActiveView { get; init; }

    /// <summary>The document as the composer currently holds it.</summary>
    public required ComposerDocument Document { get; init; }

    /// <summary>The caret and range as the browser last reported them.</summary>
    public required ComposerSelection Selection { get; init; }

    /// <summary>Whether the composer is disabled.</summary>
    public bool IsDisabled { get; init; }

    /// <summary>Whether the document is read-only.</summary>
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

    /// <summary>The syntax highlighter that classifies code.</summary>
    public required IComposerSyntaxHighlighter SyntaxHighlighter { get; init; }

    /// <summary>The key bindings the composer and its plugins answer.</summary>
    public ComposerKeyBindingCatalog KeyBindings { get; init; } = ComposerKeyBindingCatalog.Empty;

    /// <summary>The space-separated ids of the elements describing the document, which an editor's editable element names as its description.</summary>
    public string? DescribedBy { get; init; }
}
