namespace Forma.UI.Design.Emojis.Tests;

using Xunit;

public sealed class EmojiTests
{
    [Fact]
    public void Should_take_a_sequence_as_the_emoji_it_names()
    {
        // Act
        Emoji emoji = "😀";

        // Assert
        Assert.Equal("😀", emoji.Sequence);
    }

    [Theory]
    [InlineData("😀")]
    [InlineData("❤️")]
    [InlineData("👍🏽")]
    [InlineData("🇮🇱")]
    [InlineData("👨‍👩‍👧")]
    [InlineData("1️⃣")]
    public void Should_accept_any_single_character_however_many_scalars_it_takes(string sequence)
    {
        // Act
        var emoji = new Emoji(sequence);

        // Assert
        Assert.Equal(sequence, emoji.Sequence);
    }

    [Theory]
    [InlineData("")]
    [InlineData("😀😀")]
    [InlineData("ab")]
    public void Should_refuse_anything_but_one_character(string sequence)
        => Assert.Throws<ArgumentException>(() => new Emoji(sequence));

    [Theory]
    [InlineData("😀", true)]
    [InlineData("👨‍👩‍👧", true)]
    [InlineData("", false)]
    [InlineData("😀😀", false)]
    [InlineData(null, false)]
    public void Should_tell_a_sequence_that_names_an_emoji_from_one_that_does_not(string? sequence, bool expected)
    {
        // Act
        var named = Emoji.TryCreate(sequence, out var emoji);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(expected, named),
            () => Assert.Equal(expected ? sequence : null, emoji.Sequence));
    }

    [Fact]
    public void Should_refuse_a_null_sequence()
        => Assert.Throws<ArgumentNullException>(() => new Emoji(null!));

    [Fact]
    public void Should_default_to_no_sequence()
    {
        // Act
        var emoji = default(Emoji);

        // Assert
        // A default emoji names nothing rather than failing, the way an absent glyph does.
        Assert.Null(emoji.Sequence);
    }

    [Fact]
    public void Should_treat_the_same_sequence_as_equal()
    {
        // Act
        Emoji first = "😀";
        Emoji second = "😀";

        // Assert
        Assert.Equal(first, second);
    }
}
