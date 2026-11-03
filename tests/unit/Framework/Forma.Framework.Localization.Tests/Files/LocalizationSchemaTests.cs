namespace Forma.Framework.Localization.Tests.Files;

public sealed class LocalizationSchemaTests
{
    [Theory]
    [InlineData("$localization")]
    [InlineData("$shared")]
    [InlineData("$tools")]
    [InlineData("$import")]
    [InlineData("$shraed")]
    [InlineData("$x")]
    public void Should_read_a_dollar_and_one_lowercase_word_as_the_reserved_shape(string key)
        => Assert.True(LocalizationSchema.IsReservedShape(key));

    [Theory]
    [InlineData("$")]
    [InlineData("$$price")]
    [InlineData("$ per month")]
    [InlineData("$5 off")]
    [InlineData("$0.00")]
    [InlineData("$USD")]
    [InlineData("$camelCase")]
    [InlineData("$_private")]
    [InlineData("$tools.x")]
    [InlineData("$price/month")]
    [InlineData("price")]
    [InlineData("")]
    public void Should_read_any_other_key_as_text(string key)
        => Assert.False(LocalizationSchema.IsReservedShape(key));

    [Theory]
    [InlineData("$$price", "$price")]
    [InlineData("$$$price", "$$price")]
    [InlineData("$$", "$")]
    [InlineData("$price", "$price")]
    [InlineData("$", "$")]
    [InlineData("price", "price")]
    public void Should_drop_one_dollar_from_an_escaped_key_alone(string key, string expected)
        => Assert.Equal(expected, LocalizationSchema.Unescape(key));
}
