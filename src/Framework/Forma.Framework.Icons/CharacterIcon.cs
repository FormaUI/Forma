namespace Forma.Framework.Icons;

using System.Text;

/// <summary>An icon drawing a single character set by the font.</summary>
/// <param name="Character">The character to draw.</param>
public sealed record CharacterIcon(Rune Character) : Icon;
