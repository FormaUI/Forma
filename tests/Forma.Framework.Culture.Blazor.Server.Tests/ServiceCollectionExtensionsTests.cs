namespace Forma.Framework.Culture.Blazor.Server.Tests;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_circuit_handler_beside_the_culture_state_its_registration_and_the_coordinated_switch()
    {
        // Act
        var services = new ServiceCollection().AddFormaCultureServer().AddFormaCultureServer();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CircuitHandler)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CultureCoordinatorRegistration)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureSwitch)).Lifetime),
            () => Assert.Same(CultureSwitchPolicy.Default, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CultureSwitchPolicy)).ImplementationInstance),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime));
    }

    [Fact]
    public void Should_register_the_policy_the_host_gives()
    {
        // Arrange
        var policy = new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie };

        // Act
        var services = new ServiceCollection().AddFormaCultureServer(policy);

        // Assert
        Assert.Same(policy, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CultureSwitchPolicy)).ImplementationInstance);
    }

    [Fact]
    public void Should_reject_a_cookie_sent_from_every_site_that_is_not_secure()
    {
        // Arrange
        var policy = new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie, CookieSameSite = CultureCookieSameSite.None };

        // Act
        var exception = Record.Exception(() => new ServiceCollection().AddFormaCultureServer(policy));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }
}
