namespace Forma.Framework.Globalization.AspNetCore.Tests;

using Microsoft.Extensions.DependencyInjection;

public sealed class CultureRoutingExtensionsTests
{
    [Fact]
    public void Should_register_the_switcher_beside_the_router_and_the_culture_state_once()
    {
        // Arrange
        var options = new CultureRouterOptions { DefaultCulture = "en", Routes = [new() { Token = "en", Culture = new("en") }] };

        // Act
        var services = new ServiceCollection().AddCultureRouting(options).AddCultureRouting(options);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CultureRouter)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CultureSwitcher)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime));
    }
}
