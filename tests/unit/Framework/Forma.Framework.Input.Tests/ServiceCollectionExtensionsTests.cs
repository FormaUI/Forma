namespace Forma.Framework.Input.Tests;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_default_policy()
    {
        // Act
        var services = new ServiceCollection().AddFormaInputServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(InputRouterOptions));
        Assert.Same(InputRouterOptions.Default, descriptor.ImplementationInstance);
    }

    [Fact]
    public void Should_register_the_policy_it_is_given()
    {
        // Arrange
        var policy = new InputRouterOptions { Escape = DismissBehavior.Ignore };

        // Act
        var services = new ServiceCollection().AddFormaInputServices(policy);

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(InputRouterOptions));
        Assert.Same(policy, descriptor.ImplementationInstance);
    }

    [Fact]
    public void Should_leave_a_policy_the_host_registered_first()
    {
        // Arrange
        var policy = new InputRouterOptions { PressAway = PressAwayBehavior.Ignore };
        var services = new ServiceCollection();
        services.AddSingleton(policy);

        // Act
        services.AddFormaInputServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(InputRouterOptions));
        Assert.Same(policy, descriptor.ImplementationInstance);
    }
}
