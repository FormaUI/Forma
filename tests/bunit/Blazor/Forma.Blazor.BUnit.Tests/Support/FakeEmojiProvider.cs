namespace Forma.Blazor.BUnit.Tests.Support;

using Forma.Framework.Emojis;

/// <summary>An emoji provider drawing every emoji with one artwork.</summary>
/// <param name="artwork">The artwork every emoji is drawn with.</param>
public sealed class FakeEmojiProvider(EmojiArtwork artwork) : IEmojiProvider
{
    /// <inheritdoc />
    public EmojiArtwork GetArtwork(Emoji emoji)
        => artwork;
}
