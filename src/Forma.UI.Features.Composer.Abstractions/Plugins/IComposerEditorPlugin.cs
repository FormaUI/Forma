namespace Forma.UI.Features.Composer.Plugins;

using Forma.UI.Features.Composer.Documents;
using Forma.UI.Style.Icons;

/// <summary>Provides an authoring surface such as rich text, Markdown, or code.</summary>
public interface IComposerEditorPlugin : IComposerPlugin
{
    /// <summary>The title of the editor, in English, used as the localization key.</summary>
    string Title { get; }

    /// <summary>The icon associated with the editor.</summary>
    Icon Icon { get; }

    /// <summary>The document format the editor reads and writes.</summary>
    ComposerDocumentFormat Format { get; }

    /// <summary>The component rendering the editing surface.</summary>
    Type EditorComponent { get; }
}
