namespace Forma.Framework.Culture.Tests;

using System.Globalization;

using Forma.Tests.Culture;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");

    [Fact]
    public void Should_register_the_culture_state_and_its_integration_scoped()
    {
        // Act
        var services = new ServiceCollection().AddFormaCultureServices();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureStateIntegration)).Lifetime));
    }

    [Fact]
    public void Should_resolve_the_integration_as_the_states_own_instance()
    {
        // Arrange
        using var provider = new ServiceCollection().AddFormaCultureServices().BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var integration = scope.ServiceProvider.GetRequiredService<ICultureStateIntegration>();

        // Assert
        Assert.Same(scope.ServiceProvider.GetRequiredService<ICultureState>(), integration);
    }

    [Fact]
    public void Should_keep_a_state_the_host_registered_first()
    {
        // Arrange
        var hostState = new FakeCultureState(English);
        var services = new ServiceCollection();
        services.AddScoped<ICultureState>(_ => hostState);

        // Act
        services.AddFormaCultureServices().AddFormaCultureServices();

        // Assert
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Multiple(
            () => Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)),
            () => Assert.Same(hostState, scope.ServiceProvider.GetRequiredService<ICultureState>()),
            () => Assert.Same(hostState, scope.ServiceProvider.GetRequiredService<ICultureStateIntegration>()));
    }

    [Fact]
    public void Should_reject_a_state_without_the_integration_as_a_scope_first_resolves_it()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<ICultureState>(_ => new InterleavedCultureState(English, English));
        using var provider = services.AddFormaCultureServices().BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        var exception = Record.Exception(scope.ServiceProvider.GetRequiredService<ICultureState>);

        // Assert
        Assert.Contains(nameof(ICultureStateIntegration), Assert.IsType<InvalidOperationException>(exception).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_reject_a_state_instance_without_the_integration_as_it_is_registered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ICultureState>(new InterleavedCultureState(English, English));

        // Act
        var exception = Record.Exception(services.AddFormaCultureServices);

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }
}
