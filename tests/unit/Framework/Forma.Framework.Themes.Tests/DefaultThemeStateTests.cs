namespace Forma.Framework.Themes.Tests;

using Forma.Framework.Localization;

using Xunit;

public sealed class DefaultThemeStateTests
{
    private static DefaultThemeState CreateState()
    {
        var options = new ThemeOptions();
        options.Add(new ThemeDefinition("forma", LocalizableText.For<DefaultThemeStateTests>("Forma"), ColorSchemes.Light | ColorSchemes.Dark, "forma.css"));
        options.Add(new ThemeDefinition("harbor", LocalizableText.For<DefaultThemeStateTests>("Harbor"), ColorSchemes.Light | ColorSchemes.Dark, "harbor.css"));

        return new DefaultThemeState(new ThemeRegistry(options));
    }

    [Fact]
    public async Task Should_apply_an_accepted_choice_and_tell_its_subscribers_once()
    {
        // Arrange
        var state = CreateState();
        var notifications = 0;
        state.Subscribe(() => { notifications++; return Task.CompletedTask; });

        // Act
        await state.ApplyAsync(new AcceptedTheme(new ThemeChoice("harbor", ColorSchemePreference.Dark), new ThemeVersion("document", 1)));

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Dark), state.Choice),
            () => Assert.Equal(1, state.Revision),
            () => Assert.Equal(1, notifications));
    }

    [Fact]
    public async Task Should_ignore_a_version_it_has_applied_already_or_an_older_one()
    {
        // Arrange
        var state = CreateState();
        await state.ApplyAsync(new AcceptedTheme(new ThemeChoice("harbor", ColorSchemePreference.Dark), new ThemeVersion("document", 2)));

        // Act
        await state.ApplyAsync(new AcceptedTheme(new ThemeChoice("forma", ColorSchemePreference.Light), new ThemeVersion("document", 1)));

        // Assert
        Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Dark), state.Choice);
    }

    [Fact]
    public async Task Should_hold_the_default_theme_for_an_accepted_choice_naming_one_that_isn_t_registered()
    {
        // Arrange
        var state = CreateState();

        // Act
        await state.ApplyAsync(new AcceptedTheme(new ThemeChoice("removed", ColorSchemePreference.Light), new ThemeVersion("document", 1)));

        // Assert
        Assert.Equal(new ThemeChoice("forma", ColorSchemePreference.Light), state.Choice);
    }
}
