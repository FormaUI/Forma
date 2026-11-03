namespace Forma.Framework.Composer.Editing;

/// <summary>A span of a document's text, together with the text it covers.</summary>
/// <param name="Start">The zero-based start offset.</param>
/// <param name="End">The zero-based end offset, exclusive.</param>
/// <param name="Text">The text between <paramref name="Start"/> and <paramref name="End"/>.</param>
public sealed record ComposerTextSpan(int Start, int End, string Text);
