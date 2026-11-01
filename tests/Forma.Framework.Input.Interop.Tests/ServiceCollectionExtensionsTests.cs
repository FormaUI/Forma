namespace Forma.Framework.Input.Interop.Tests;

using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    public async Task Should_make_the_router_in_a_container_with_no_runtime()
    {
        // Arrange
        await using var provider = new ServiceCollection().AddFormaInputRouter().BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        // Act
        var router = scope.ServiceProvider.GetRequiredService<InputRouter>();

        // Assert
        Assert.NotNull(router);
    }

    [Fact]
    public void Should_log_through_the_logging_a_host_registers_after_it()
    {
        // Arrange
        using var loggerProvider = new FakeLoggerProvider();
        var services = new ServiceCollection().AddFormaInputRouter();

        // Act
        services.AddLogging(logging => logging.AddProvider(loggerProvider));

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ILogger<InputRouter>>().Log(LogLevel.Information, default, "Logged.", null, (message, _) => message);
        Assert.Contains(loggerProvider.Entries, entry => entry.Message == "Logged.");
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
