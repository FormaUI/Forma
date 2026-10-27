namespace Forma.Framework.Culture.Browser.Interop.Tests;

using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_document_culture_synchronizer_beside_the_culture_state_once_per_scope()
    {
        // Act
        var services = new ServiceCollection().AddFormaCultureBrowserSynchronization().AddFormaCultureBrowserSynchronization();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(DocumentCultureSynchronizer)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime));
    }
}
