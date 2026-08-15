namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class LocaleNameTests
{
    [Theory]
    [InlineData("count")]
    [InlineData("Count")]
    [InlineData("_count")]
    [InlineData("count2")]
    [InlineData("días")]
    public void Should_accept_a_plain_identifier(string name)
    {
        // Assert
        Assert.True(LocaleName.IsValid(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("2count")]
    [InlineData("a.b")]
    [InlineData(" count")]
    [InlineData("count ")]
    [InlineData("items.Length")]
    [InlineData("a-b")]
    public void Should_reject_anything_a_translation_could_not_spell(string name)
    {
        // Assert
        Assert.False(LocaleName.IsValid(name));
    }
}
