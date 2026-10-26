namespace Forma.Framework.Browser.Interop.Tests;

using Forma.Framework.Input.Interop;
using Forma.Tests.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Xunit;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_bridge_once_per_scope()
    {
        // Act
        var services = new ServiceCollection().AddFormaBrowserServices();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(DomBridge));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public async Task Should_make_the_bridge_without_an_input_router_registered()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new FakeJSRuntime((_, _) => null))
            .AddFormaBrowserServices()
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        // Act
        var bridge = scope.ServiceProvider.GetRequiredService<DomBridge>();

        // Assert
        Assert.NotNull(bridge);
    }

    [Fact]
    public async Task Should_make_the_bridge_beside_the_scopes_input_router()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new FakeJSRuntime((_, _) => null))
            .AddFormaInputRouter()
            .AddFormaBrowserServices()
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        // Act
        var bridge = scope.ServiceProvider.GetRequiredService<DomBridge>();

        // Assert
        Assert.Multiple(
            () => Assert.NotNull(bridge),
            () => Assert.Same(bridge, scope.ServiceProvider.GetRequiredService<DomBridge>()));
    }
}
