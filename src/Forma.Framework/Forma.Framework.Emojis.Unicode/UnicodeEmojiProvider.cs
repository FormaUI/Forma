namespace Forma.Framework.Emojis.Unicode;

using Forma.Framework.Emojis;

/// <summary>Provides the platform's own emoji font as the artwork for every emoji.</summary>
public sealed class UnicodeEmojiProvider : IEmojiProvider
{
    /// <summary>The provider every component without a registered one draws through.</summary>
    public static UnicodeEmojiProvider Instance { get; } = new();

    /// <inheritdoc />
    public EmojiArtwork GetArtwork(Emoji emoji)
        => EmojiArtwork.Unicode;
}
