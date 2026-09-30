namespace Forma.Framework.Emojis;

/// <summary>Artwork drawn from a symbol in a sprite sheet.</summary>
/// <param name="Symbol">The identifier of the symbol within the sheet.</param>
/// <param name="Sheet">The address of the sheet the symbol is loaded from.</param>
public sealed record EmojiSprite(string Symbol, string Sheet) : EmojiArtwork;
