namespace Forma.Framework.Themes.Tests;

using Forma.Framework.Localization;

using Xunit;

public sealed class ThemeRegistryTests
{
    private static ThemeDefinition Define(string name, ColorSchemes schemes = ColorSchemes.Light | ColorSchemes.Dark)
        => new(name, LocalizableText.For<ThemeRegistryTests>(name), schemes, $"css/{name}.css");

    private static ThemeRegistry CreateRegistry()
    {
        var options = new ThemeOptions();
        options.Add(Define("forma"));
        options.Add(Define("harbor"));
        options.Add(Define("paper", ColorSchemes.Light));

        return new ThemeRegistry(options);
    }

    [Fact]
    public void Should_resolve_a_choice_naming_a_registered_theme_to_itself()
    {
        // Arrange
        var choice = new ThemeChoice("harbor", ColorSchemePreference.Dark);

        // Act
        var resolved = CreateRegistry().Resolve(choice);

        // Assert
        Assert.Same(choice, resolved);
    }

    [Fact]
    public void Should_resolve_a_choice_naming_a_theme_that_isn_t_registered_to_the_default_theme_keeping_its_scheme_preference()
    {
        // Act
        var resolved = CreateRegistry().Resolve(new ThemeChoice("removed", ColorSchemePreference.Dark));

        // Assert
        Assert.Equal(new ThemeChoice("forma", ColorSchemePreference.Dark), resolved);
    }

    [Theory]
    [InlineData("harbor", ColorSchemePreference.Dark, ColorSchemePreference.Dark)]
    [InlineData("harbor", ColorSchemePreference.System, ColorSchemePreference.System)]
    [InlineData("paper", ColorSchemePreference.Dark, ColorSchemePreference.Light)]
    [InlineData("paper", ColorSchemePreference.System, ColorSchemePreference.Light)]
    public void Should_draw_a_theme_with_a_light_scheme_alone_light_whatever_the_preference(string theme, ColorSchemePreference preference, ColorSchemePreference expected)
    {
        // Act
        var scheme = CreateRegistry().GetDrawnScheme(new ThemeChoice(theme, preference));

        // Assert
        Assert.Equal(expected, scheme);
    }

    [Fact]
    public void Should_find_a_registered_theme_by_its_name()
    {
        // Arrange
        var registry = CreateRegistry();

        // Act
        var isFound = registry.TryGetTheme("paper", out var theme);
        var isRemovedFound = registry.TryGetTheme("removed", out _);

        // Assert
        Assert.Multiple(
            () => Assert.True(isFound),
            () => Assert.Equal(ColorSchemes.Light, theme?.Schemes),
            () => Assert.False(isRemovedFound));
    }

    [Fact]
    public void Should_refuse_a_second_theme_of_a_registered_name()
    {
        // Arrange
        var options = new ThemeOptions();
        options.Add(Define("harbor"));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => options.Add(Define("harbor")));

        // Assert
        Assert.Contains("harbor", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_refuse_a_theme_without_a_light_scheme()
    {
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => Define("night", ColorSchemes.Dark));

        // Assert
        Assert.Equal("schemes", exception.ParamName);
    }
}
