namespace Forma.UI.Blazor.Tests.Emojis;

using Forma.UI.Blazor.Emojis;
using Forma.UI.Design.Emojis;

public sealed class DefaultEmojiProviderTests
{
    [Fact]
    public void Should_draw_a_known_emoji_from_a_sprite()
    {
        // Arrange
        var provider = new DefaultEmojiProvider();

        // Act
        var artwork = provider.GetArtwork("😀");

        // Assert
        var sprite = Assert.IsType<EmojiSprite>(artwork);
        Assert.Matches("^emoji-[0-9a-f]+(-[0-9a-f]+)*$", sprite.Symbol);
        Assert.Matches("^_content/Forma.UI/emojis/smileys-\\d+\\.svg$", sprite.Sheet);
    }

    [Fact]
    public void Should_draw_an_unknown_emoji_from_the_platform_font()
    {
        // Arrange
        var provider = new DefaultEmojiProvider();

        // Act
        var artwork = provider.GetArtwork("🛸👾🫠🫠🫠🫠🫠");

        // Assert
        Assert.Same(EmojiArtwork.Unicode, artwork);
    }

    [Fact]
    public void Should_refuse_the_absent_emoji()
    {
        // Arrange
        var provider = new DefaultEmojiProvider();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => provider.GetArtwork(default));
    }
}
