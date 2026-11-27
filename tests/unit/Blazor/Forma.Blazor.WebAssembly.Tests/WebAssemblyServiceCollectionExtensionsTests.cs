namespace Forma.Blazor.WebAssembly.Tests;

using Forma.Framework.Culture;
using Forma.Framework.Input;
using Forma.Framework.Themes;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class WebAssemblyServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_one_culture_state_per_runtime()
    {
        // Act
        var services = new ServiceCollection().AddFormaWebAssemblyIntegration();

        // Assert
        Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime);
    }

    [Fact]
    public void Should_register_one_theme_state_per_runtime_whichever_registers_the_host_s_themes_first()
    {
        // Act
        var services = new ServiceCollection().AddFormaThemes().AddFormaWebAssemblyIntegration();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IThemeState)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IThemeStateIntegration)).Lifetime));
    }

    [Fact]
    public void Should_register_the_default_input_policy()
    {
        // Act
        using var provider = new ServiceCollection().AddFormaWebAssemblyIntegration().BuildServiceProvider();

        // Assert
        Assert.Same(InputRouterOptions.Default, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_register_the_client_input_policy()
    {
        // Arrange
        var policy = new InputRouterOptions { Escape = DismissBehavior.Ignore };

        // Act
        using var provider = new ServiceCollection().AddFormaWebAssemblyIntegration(options => options.InputRouting = policy).BuildServiceProvider();

        // Assert
        Assert.Same(policy, provider.GetRequiredService<InputRouterOptions>());
    }
}
