namespace Forma.Framework.Culture.Router.Tests;

using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class CultureRoutingExtensionsTests
{
    [Fact]
    public void Should_register_the_switcher_beside_the_router_once_and_leave_the_culture_state_to_the_product()
    {
        // Arrange
        var options = new CultureRouterOptions { DefaultCulture = "en", Routes = [new() { Token = "en", UICulture = new("en") }] };

        // Act
        var services = new ServiceCollection().AddFormaCultureRouting(options).AddFormaCultureRouting(options);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CultureRouter)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CultureSwitcher)).Lifetime),
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(CircuitHandler)),
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(ICultureState)));
    }

    [Fact]
    public void Should_log_through_the_logging_a_host_registers_after_it()
    {
        // Arrange
        using var loggerProvider = new FakeLoggerProvider();
        var options = new CultureRouterOptions { DefaultCulture = "en", Routes = [new() { Token = "en", UICulture = new("en") }] };
        var services = new ServiceCollection().AddFormaCultureRouting(options);

        // Act
        services.AddLogging(logging => logging.AddProvider(loggerProvider));

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ILogger<CultureSwitcher>>().Log(LogLevel.Information, default, "Logged.", null, (message, _) => message);
        Assert.Contains(loggerProvider.Entries, entry => entry.Message == "Logged.");
    }
}
