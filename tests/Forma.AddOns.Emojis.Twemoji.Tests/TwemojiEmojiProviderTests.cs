namespace Forma.AddOns.Emojis.Twemoji.Tests;

using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Twemoji;

using Xunit;

public sealed class TwemojiEmojiProviderTests
{
    [Fact]
    public void Should_draw_a_known_emoji_from_a_sprite()
    {
        // Arrange
        var provider = new TwemojiEmojiProvider();

        // Act
        var artwork = provider.GetArtwork("😀");

        // Assert
        var sprite = Assert.IsType<EmojiSprite>(artwork);
        Assert.Matches("^emoji-[0-9a-f]+(-[0-9a-f]+)*$", sprite.Symbol);
        Assert.Matches("^_content/Forma.AddOns.Emojis.Twemoji/assets/smileys-\\d+\\.svg$", sprite.Sheet);
    }

    [Fact]
    public void Should_draw_an_unqualified_spelling_as_its_qualified_emoji()
    {
        // Arrange
        var provider = new TwemojiEmojiProvider();

        // Act
        var unqualified = provider.GetArtwork("⚠");
        var qualified = provider.GetArtwork("⚠\uFE0F");

        // Assert
        Assert.Multiple(
            () => Assert.IsType<EmojiSprite>(unqualified),
            () => Assert.Equal(qualified, unqualified));
    }

    [Fact]
    public void Should_draw_a_sequence_missing_its_interior_selector()
    {
        // Arrange
        var provider = new TwemojiEmojiProvider();

        // Act
        // The rainbow flag, whose presentation selector sits inside the sequence.
        var unqualified = provider.GetArtwork("\U0001F3F3\u200D\U0001F308");
        var qualified = provider.GetArtwork("\U0001F3F3\uFE0F\u200D\U0001F308");

        // Assert
        Assert.Multiple(
            () => Assert.IsType<EmojiSprite>(unqualified),
            () => Assert.Equal(qualified, unqualified));
    }

    [Fact]
    public void Should_draw_an_unknown_emoji_from_the_platform_font()
    {
        // Arrange
        var provider = new TwemojiEmojiProvider();

        // Act
        // A joined sequence Unicode never defined.
        var artwork = provider.GetArtwork("👨‍🥑");

        // Assert
        Assert.Same(EmojiArtwork.Unicode, artwork);
    }

    [Fact]
    public void Should_refuse_the_absent_emoji()
    {
        // Arrange
        var provider = new TwemojiEmojiProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetArtwork(default));
    }
}
