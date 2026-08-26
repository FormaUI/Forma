namespace Forma.UI.Design.Emojis.Tests;

using Xunit;

public sealed class EmojiTests
{
    [Fact]
    public void Should_take_a_character_as_the_emoji_it_names()
    {
        // Act
        Emoji emoji = "😀";

        // Assert
        Assert.Equal("😀", emoji.Character);
    }

    [Fact]
    public void Should_default_to_no_character()
    {
        // Act
        var emoji = default(Emoji);

        // Assert
        Assert.Null(emoji.Character);
    }

    [Fact]
    public void Should_treat_the_same_character_as_equal()
    {
        // Act
        Emoji first = "😀";
        Emoji second = "😀";

        // Assert
        Assert.Equal(first, second);
    }
}
