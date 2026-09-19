namespace Forma.Framework.Composer.Editing;

using System.Diagnostics.CodeAnalysis;

/// <summary>A stored file to put into the document, and how it is put there.</summary>
/// <param name="Source">Where the file lives.</param>
/// <param name="Name">The file's own name, used as the link's text and the picture's alternative text.</param>
/// <param name="Kind">What the browser can make of it: <c>image</c>, <c>video</c>, <c>audio</c>, or <c>file</c>.</param>
/// <param name="Embed">Whether it is rendered where it is inserted.</param>
/// <param name="Link">Whether a link to it is inserted.</param>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public sealed record ResourceInsertion(
    string Source,
    string Name,
    string Kind,
    bool Embed,
    bool Link) : ComposerInsertion;
