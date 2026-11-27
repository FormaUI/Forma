namespace Forma.Framework.Themes.Tests;

using Forma.Framework.Localization;

using Xunit;

public sealed class ThemeAttributesTests
{
    [Theory]
    [InlineData(ColorSchemePreference.System, "system")]
    [InlineData(ColorSchemePreference.Light, "light")]
    [InlineData(ColorSchemePreference.Dark, "dark")]
    public void Should_read_back_the_value_each_scheme_preference_writes(ColorSchemePreference scheme, string value)
    {
        // Act
        var written = ThemeAttributes.GetSchemeValue(scheme);
        var isRead = ThemeAttributes.TryParseScheme(value, out var read);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(value, written),
            () => Assert.True(isRead),
            () => Assert.Equal(scheme, read));
    }

    [Theory]
    [InlineData("Dark")]
    [InlineData("dusk")]
    [InlineData("")]
    [InlineData(null)]
    public void Should_refuse_a_value_that_names_no_scheme_preference(string? value)
    {
        // Act
        var isRead = ThemeAttributes.TryParseScheme(value, out _);

        // Assert
        Assert.False(isRead);
    }

    [Fact]
    public void Should_write_a_theme_with_a_light_scheme_alone_in_its_light_scheme()
    {
        // Arrange
        var options = new ThemeOptions();
        options.Add(new ThemeDefinition("forma", LocalizableText.For<ThemeAttributesTests>("Forma"), ColorSchemes.Light | ColorSchemes.Dark, "forma.css"));
        options.Add(new ThemeDefinition("paper", LocalizableText.For<ThemeAttributesTests>("Paper"), ColorSchemes.Light, "paper.css"));

        // Act
        var attributes = ThemeAttributes.Of(new ThemeRegistry(options), new ThemeChoice("paper", ColorSchemePreference.Dark));

        // Assert
        Assert.Equal(new Dictionary<string, object> { ["data-forma-theme"] = "paper", ["data-forma-scheme"] = "light" }, attributes);
    }
}
