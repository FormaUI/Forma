namespace Forma.Framework.Globalization.Interop.Tests;

using Microsoft.Extensions.DependencyInjection;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_document_language_beside_the_culture_state_once_per_scope()
    {
        // Act
        var services = new ServiceCollection().AddFormaDocumentLanguage().AddFormaDocumentLanguage();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(DocumentLanguage)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime));
    }
}
