namespace Forma.Framework.Culture.Browser.Interop.Tests;

using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task Should_make_the_registration_in_a_container_with_no_runtime()
    {
        // Arrange
        await using var provider = new ServiceCollection().AddFormaCultureCoordination().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        // Act
        var registration = scope.ServiceProvider.GetRequiredService<CultureCoordinatorRegistration>();

        // Assert
        Assert.NotNull(registration);
    }

    [Fact]
    public void Should_log_through_the_logging_a_host_registers_after_it()
    {
        // Arrange
        using var loggerProvider = new FakeLoggerProvider();
        var services = new ServiceCollection().AddFormaCultureCoordination();

        // Act
        services.AddLogging(logging => logging.AddProvider(loggerProvider));

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ILogger<CultureCoordinatorRegistration>>().Log(LogLevel.Information, default, "Logged.", null, (message, _) => message);
        Assert.Contains(loggerProvider.Entries, entry => entry.Message == "Logged.");
    }

    [Fact]
    public void Should_register_the_coordinator_registration_beside_the_culture_state_once_per_scope()
    {
        // Act
        var services = new ServiceCollection().AddFormaCultureCoordination().AddFormaCultureCoordination();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CultureCoordinatorRegistration)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime));
    }

    public sealed class SwitchPolicy
    {
        [Fact]
        public void Should_register_the_default_policy_where_none_is_given_and_keep_the_first_registered()
        {
            // Arrange
            var policy = new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie };

            // Act
            var defaulted = new ServiceCollection().AddFormaCultureSwitchPolicy();
            var given = new ServiceCollection().AddFormaCultureSwitchPolicy(policy).AddFormaCultureSwitchPolicy();

            // Assert
            Assert.Multiple(
                () => Assert.Same(CultureSwitchPolicy.Default, Assert.Single(defaulted).ImplementationInstance),
                () => Assert.Same(policy, Assert.Single(given).ImplementationInstance));
        }

        [Theory]
        [InlineData("", "/", CultureCookieSameSite.Lax)]
        [InlineData("culture", " ", CultureCookieSameSite.Lax)]
        [InlineData("culture", "/", CultureCookieSameSite.None)]
        public void Should_reject_a_cookie_policy_a_browser_can_t_keep(string name, string path, CultureCookieSameSite sameSite)
        {
            // Arrange
            var policy = new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie, CookieName = name, CookiePath = path, CookieSameSite = sameSite };

            // Act
            var exception = Record.Exception(() => new ServiceCollection().AddFormaCultureSwitchPolicy(policy));

            // Assert
            Assert.IsType<ArgumentException>(exception);
        }
    }
}
