namespace Forma.Framework.Emojis;

/// <summary>Provides artwork for emoji.</summary>
public interface IEmojiProvider
{
    /// <summary>Gets the artwork used to draw an emoji.</summary>
    /// <param name="emoji">The emoji to draw.</param>
    /// <returns>The artwork for the emoji.</returns>
    EmojiArtwork GetArtwork(Emoji emoji);
}
