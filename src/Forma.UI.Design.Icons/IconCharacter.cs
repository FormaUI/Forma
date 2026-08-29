namespace Forma.UI.Design.Icons;

using System.Text;

/// <summary>Artwork drawn as a single character by the font.</summary>
/// <param name="Value">The character drawn.</param>
public sealed record IconCharacter(Rune Value) : IconArtwork;
