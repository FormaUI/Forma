namespace Forma.UI.Design.Emojis.Tests;

using Xunit;

public sealed class EmojiArtworkTests
{
    [Fact]
    public void Should_build_a_sprite_carrying_its_symbol_and_sheet()
    {
        // Act
        var artwork = EmojiArtwork.Sprite("emoji-1f600", "smileys-0.svg");

        // Assert
        var sprite = Assert.IsType<EmojiSprite>(artwork);
        Assert.Multiple(
            () => Assert.Equal("emoji-1f600", sprite.Symbol),
            () => Assert.Equal("smileys-0.svg", sprite.Sheet));
    }

    [Fact]
    public void Should_build_an_image_carrying_its_url()
    {
        // Act
        var artwork = EmojiArtwork.Image("https://example.test/emoji.png");

        // Assert
        var image = Assert.IsType<EmojiImage>(artwork);
        Assert.Equal("https://example.test/emoji.png", image.Url);
    }

    [Fact]
    public void Should_carry_no_extra_data_for_the_unicode_case()
    {
        // Assert
        Assert.IsType<EmojiArtwork>(EmojiArtwork.Unicode);
    }
}
