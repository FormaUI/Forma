namespace Forma.Framework.Culture.Blazor.Server.Tests;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_circuit_handler_beside_the_culture_state_and_the_document_culture_synchronizer()
    {
        // Act
        var services = new ServiceCollection().AddFormaCultureServer().AddFormaCultureServer();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CircuitHandler)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(DocumentCultureSynchronizer)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime));
    }
}
