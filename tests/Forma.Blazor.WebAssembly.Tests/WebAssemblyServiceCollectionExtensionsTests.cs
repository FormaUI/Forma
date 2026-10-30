namespace Forma.Blazor.WebAssembly.Tests;

using Forma.Framework.Culture;
using Forma.Framework.Input;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class WebAssemblyServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_one_culture_state_per_runtime()
    {
        // Act
        var services = new ServiceCollection().AddFormaUIWebAssembly();

        // Assert
        Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime);
    }

    [Fact]
    public void Should_register_the_default_input_policy()
    {
        // Act
        using var provider = new ServiceCollection().AddFormaUIWebAssembly().BuildServiceProvider();

        // Assert
        Assert.Same(InputRouterOptions.Default, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_register_the_client_input_policy()
    {
        // Arrange
        var policy = new InputRouterOptions { Escape = DismissBehavior.Ignore };

        // Act
        using var provider = new ServiceCollection().AddFormaUIWebAssembly(options => options.InputRouting = policy).BuildServiceProvider();

        // Assert
        Assert.Same(policy, provider.GetRequiredService<InputRouterOptions>());
    }
}
