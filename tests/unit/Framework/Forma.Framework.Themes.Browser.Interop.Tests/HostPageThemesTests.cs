namespace Forma.Framework.Themes.Browser.Interop.Tests;

using Forma.Framework.Localization;

using Xunit;

public sealed class HostPageThemesTests
{
    private static ThemeRegistry CreateRegistry()
    {
        var options = new ThemeOptions { Default = new ThemeChoice("harbor", ColorSchemePreference.System) };
        options.Add(new ThemeDefinition("forma", LocalizableText.For<HostPageThemesTests>("Forma"), ColorSchemes.Light | ColorSchemes.Dark, "forma.css"));
        options.Add(new ThemeDefinition("harbor", LocalizableText.For<HostPageThemesTests>("Harbor"), ColorSchemes.Light | ColorSchemes.Dark, "harbor.css"));

        return new ThemeRegistry(options);
    }

    [Fact]
    public void Should_find_no_difference_in_a_page_declaring_the_registration_s_themes_and_default()
    {
        // Arrange
        var page = new HostPageThemes { Themes = ["harbor", "forma"], DefaultTheme = "harbor", DefaultScheme = "system" };

        // Act
        var differences = page.FindDifferences(CreateRegistry());

        // Assert
        Assert.Empty(differences);
    }

    [Fact]
    public void Should_name_each_theme_and_each_default_the_page_declares_otherwise()
    {
        // Arrange
        var page = new HostPageThemes { Themes = ["forma", "dusk"], DefaultTheme = "forma", DefaultScheme = "dark" };

        // Act
        var differences = page.FindDifferences(CreateRegistry());

        // Assert
        Assert.Equal(
            [
                "the host page links no stylesheet for harbor, which the registration names",
                "the host page links a stylesheet for dusk, which the registration doesn't name",
                "the host page's default theme is forma, where the registration's is harbor",
                "the host page's default scheme is dark, where the registration's is system",
            ],
            differences);
    }
}
