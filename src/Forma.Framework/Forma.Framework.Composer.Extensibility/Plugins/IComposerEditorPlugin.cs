namespace Forma.Framework.Composer.Plugins;

using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Icons;

/// <summary>Provides one way of authoring, such as rich text, Markdown, or code.</summary>
public interface IComposerEditorPlugin : IComposerPlugin
{
    /// <summary>The title of the editor, in English, used as the localization key.</summary>
    string Title { get; }

    /// <summary>The icon associated with the editor.</summary>
    Icon Icon { get; }

    /// <summary>The document format the editor reads and writes.</summary>
    ComposerDocumentFormat Format { get; }

    /// <summary>The component filling this editor's canvas.</summary>
    Type EditorComponent { get; }

    /// <summary>Puts an insertion into this editor's surface, reporting whether it takes one of that kind.</summary>
    ValueTask<bool> TryInsertAsync(ComposerInsertion insertion, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(false);
}
