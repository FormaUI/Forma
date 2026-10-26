namespace Forma.Framework.Globalization.Interop.Server.Tests;

using Forma.Framework.Globalization.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_circuit_handler_beside_the_culture_state_and_the_document_language()
    {
        // Act
        var services = new ServiceCollection().AddFormaGlobalizationCircuitIntegration().AddFormaGlobalizationCircuitIntegration();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CircuitHandler)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(DocumentLanguage)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime));
    }
}
