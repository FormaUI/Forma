namespace Forma.UI.Design.Icons;

/// <summary>Artwork drawn as a character by whichever font the reader's platform resolves it with.</summary>
/// <param name="Text">The character sequence depicting the glyph.</param>
public sealed record IconCharacter(string Text) : IconArtwork;
