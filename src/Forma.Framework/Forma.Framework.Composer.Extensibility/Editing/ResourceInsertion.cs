namespace Forma.Framework.Composer.Editing;

using System.Diagnostics.CodeAnalysis;

/// <summary>A stored file to put into the document, and how it is put there.</summary>
/// <param name="Source">The source address of the file.</param>
/// <param name="Name">The name of the file.</param>
/// <param name="Kind">The kind of the file: <c>image</c>, <c>video</c>, <c>audio</c>, or <c>file</c>.</param>
/// <param name="Embed">Whether the file is embedded where it is inserted.</param>
/// <param name="Link">Whether a link to the file is inserted.</param>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public sealed record ResourceInsertion(
    string Source,
    string Name,
    string Kind,
    bool Embed,
    bool Link) : ComposerInsertion;
