namespace Forma.Framework.Culture.Browser.Interop.Tests;

using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
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
