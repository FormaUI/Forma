namespace Forma.Framework.Browser.Interop.Server.Tests;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_circuit_handler_beside_the_browser_services()
    {
        // Act
        var services = new ServiceCollection().AddFormaBrowserCircuitIntegration().AddFormaBrowserCircuitIntegration();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CircuitHandler)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(DomBridge)).Lifetime));
    }
}
