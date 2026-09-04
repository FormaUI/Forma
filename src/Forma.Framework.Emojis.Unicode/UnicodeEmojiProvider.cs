namespace Forma.Framework.Emojis.Unicode;

using Forma.Framework.Emojis;

/// <summary>Draws every emoji with the reader's own platform font, for a host that registered no artwork of its own.</summary>
public sealed class UnicodeEmojiProvider : IEmojiProvider
{
    /// <inheritdoc />
    public EmojiArtwork GetArtwork(Emoji emoji)
        => EmojiArtwork.Unicode;
}
