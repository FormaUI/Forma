namespace Forma.Framework.Themes.Tests;

using Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    private static readonly ThemeDefinition Forma = Define("forma");
    private static readonly ThemeDefinition Harbor = Define("harbor");

    private static ThemeDefinition Define(string name, ColorSchemes schemes = ColorSchemes.Light | ColorSchemes.Dark)
        => new(name, LocalizableText.For<ServiceCollectionExtensionsTests>(name), schemes, $"css/{name}.css");

    private static ThemeRegistry Resolve(IServiceCollection services)
    {
        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<ThemeRegistry>();
    }

    [Fact]
    public void Should_register_the_registry_once_and_the_state_and_its_integration_scoped()
    {
        // Act
        var services = new ServiceCollection().AddFormaThemes().AddFormaThemes();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ThemeRegistry)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IThemeState)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IThemeStateIntegration)).Lifetime));
    }

    [Fact]
    public void Should_start_a_reader_in_forma_s_theme_in_the_system_preference()
    {
        // Arrange
        var services = new ServiceCollection().AddFormaThemeDefaults(options => options.Add(Forma));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var state = scope.ServiceProvider.GetRequiredService<IThemeState>();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemeChoice("forma", ColorSchemePreference.System), state.Choice),
            () => Assert.Same(state, scope.ServiceProvider.GetRequiredService<IThemeStateIntegration>()),
            () => Assert.Equal(ThemeVersion.Initial, state.Version));
    }

    [Fact]
    public void Should_apply_a_host_s_configuration_after_the_library_s_themes_whichever_registers_first()
    {
        // Arrange — the host removes Forma's theme and adds its own before the library registers Forma's
        var services = new ServiceCollection().AddFormaThemes(options =>
        {
            options.Remove("forma");
            options.Add(Harbor);
            options.Default = new ThemeChoice("harbor", ColorSchemePreference.Dark);
        });
        services.AddFormaThemeDefaults(options => options.Add(Forma));

        // Act
        var registry = Resolve(services);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["harbor"], registry.Themes.Select(theme => theme.Name)),
            () => Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Dark), registry.Default));
    }

    [Fact]
    public void Should_offer_the_library_s_themes_first_and_each_host_s_after_them_in_order()
    {
        // Arrange
        var services = new ServiceCollection().AddFormaThemes(options => options.Add(Harbor));
        services.AddFormaThemeDefaults(options => options.Add(Forma));
        services.AddFormaThemes(options => options.Add(Define("dusk")));

        // Act
        var registry = Resolve(services);

        // Assert
        Assert.Equal(["forma", "harbor", "dusk"], registry.Themes.Select(theme => theme.Name));
    }

    [Fact]
    public void Should_refuse_a_default_naming_a_theme_that_isn_t_registered_as_the_registry_is_made()
    {
        // Arrange
        var services = new ServiceCollection().AddFormaThemes(options => options.Add(Harbor));

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => Resolve(services));

        // Assert
        Assert.Contains("forma", exception.Message, StringComparison.Ordinal);
    }
}
