namespace Forma.UI.Features.Composer.Editing;

/// <summary>A replacement of a document's text together with where the caret lands afterwards.</summary>
/// <param name="Content">The document's text after the edit.</param>
/// <param name="Selection">The caret or range to restore once the text is applied.</param>
public sealed record ComposerEdit(string Content, ComposerSelection Selection);
