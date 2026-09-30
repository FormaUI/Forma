namespace Forma.Framework.Emojis.Unicode.Tests;

using Forma.Framework.Emojis;

public sealed class UnicodeEmojiProviderTests
{
    [Fact]
    public void Should_draw_every_emoji_with_the_platform_font()
    {
        // Act
        var artwork = new UnicodeEmojiProvider().GetArtwork("😀");

        // Assert
        Assert.Same(EmojiArtwork.Unicode, artwork);
    }

    [Fact]
    public void Should_answer_for_an_emoji_no_catalog_knows()
    {
        // Act
        var artwork = new UnicodeEmojiProvider().GetArtwork("🫠");

        // Assert
        Assert.Same(EmojiArtwork.Unicode, artwork);
    }
}
