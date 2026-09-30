namespace Forma.Framework.Emojis.Unicode;

using Forma.Framework.Emojis;

/// <summary>Provides the platform's own emoji font as the artwork for every emoji.</summary>
public sealed class UnicodeEmojiProvider : IEmojiProvider
{
    /// <inheritdoc />
    public EmojiArtwork GetArtwork(Emoji emoji)
        => EmojiArtwork.Unicode;
}
