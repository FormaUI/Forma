namespace Forma.Framework.Input.Interop.Tests;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_router_once_per_circuit()
    {
        // Act
        var services = new ServiceCollection().AddFormaInputRouter();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(InputRouter));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Should_register_the_policy_it_is_given()
    {
        // Arrange
        var policy = new InputRouterOptions { Escape = DismissBehavior.Ignore };

        // Act
        var services = new ServiceCollection().AddFormaInputRouter(policy);

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(InputRouterOptions));
        Assert.Same(policy, descriptor.ImplementationInstance);
    }
}
