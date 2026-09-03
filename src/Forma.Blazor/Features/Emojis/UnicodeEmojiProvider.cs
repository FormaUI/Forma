namespace Forma.Blazor.Features.Emojis;

using Forma.Framework.Emojis;

/// <summary>Draws every emoji with the reader's own platform font, for a host that registered no artwork of its own.</summary>
public sealed class UnicodeEmojiProvider : IEmojiProvider
{
    /// <summary>The shared provider, which holds no state of its own.</summary>
    public static UnicodeEmojiProvider Default { get; } = new();

    /// <inheritdoc />
    public EmojiArtwork GetArtwork(Emoji emoji)
        => EmojiArtwork.Unicode;
}
