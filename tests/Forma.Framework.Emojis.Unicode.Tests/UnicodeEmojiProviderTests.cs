namespace Forma.Framework.Emojis.Unicode.Tests;

using Forma.Framework.Emojis;

public sealed class UnicodeEmojiProviderTests
{
    [Fact]
    public void Should_draw_every_emoji_with_the_platform_font()
    {
        // Act
        var artwork = UnicodeEmojiProvider.Default.GetArtwork("😀");

        // Assert
        Assert.Same(EmojiArtwork.Unicode, artwork);
    }

    [Fact]
    public void Should_answer_for_an_emoji_no_catalog_knows()
    {
        // Act
        var artwork = UnicodeEmojiProvider.Default.GetArtwork("🫠");

        // Assert
        // The reader's own font decides what it can draw, so the provider never has to know the character.
        Assert.Same(EmojiArtwork.Unicode, artwork);
    }
}
