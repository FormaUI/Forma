namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>The CSS unit a chosen font size is written in.</summary>
public enum ComposerFontSizeUnit
{
    /// <summary>Sizes relative to the root, so authored content follows the reader's font size preference.</summary>
    Rem,

    /// <summary>Sizes in points, holding the same physical size whatever the reader prefers.</summary>
    Points,
}
