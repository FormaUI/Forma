namespace Forma.Framework.Globalization.Tests;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_culture_state_scoped()
    {
        // Act
        var services = new ServiceCollection().AddFormaGlobalizationCoreServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Should_leave_a_state_the_host_registered_first()
    {
        // Arrange
        Func<IServiceProvider, ICultureState> hostFactory = _ => throw new InvalidOperationException("The host's state is never created here.");
        var services = new ServiceCollection();
        services.AddScoped(hostFactory);

        // Act
        services.AddFormaGlobalizationCoreServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState));
        Assert.Same(hostFactory, descriptor.ImplementationFactory);
    }
}
