namespace Forma.Blazor.WebView.Tests;

using Forma.Framework.Culture;
using Forma.Framework.Input;
using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Xunit;

public sealed class WebViewServiceCollectionExtensionsTests
{
    // What a web view's platform registers for each web view's scope, its JS runtime and its navigation, with its own registrar.
    private static IServiceCollection AddPlatform(IServiceCollection services)
        => services
            .AddScoped<IJSRuntime>(_ => FakeCultureCoordinatorBrowser.CreateWithoutMarker().Runtime)
            .AddScoped<NavigationManager>(_ => new FakeNavigationManager(new Uri("https://0.0.0.1/"), new Uri("https://0.0.0.1/")));

    [Fact]
    public void Should_register_one_culture_state_and_one_switch_for_the_application()
    {
        // Act
        var services = new ServiceCollection().AddFormaWebViewIntegration();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Singleton, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureSwitch)).Lifetime),
            () => Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureScopeStart)).Lifetime));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Should_start_each_web_view_s_part_in_the_culture_whichever_registers_first_it_or_the_platform(bool isPlatformFirst)
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var services = new ServiceCollection();

        if (isPlatformFirst)
        {
            AddPlatform(services).AddFormaWebViewIntegration();
        }
        else
        {
            AddPlatform(services.AddFormaWebViewIntegration());
        }

        await using var provider = services.BuildServiceProvider();
        await using var first = provider.CreateAsyncScope();
        await using var second = provider.CreateAsyncScope();

        // Act
        first.ServiceProvider.GetRequiredService<ICultureScopeStart>().Start();
        second.ServiceProvider.GetRequiredService<ICultureScopeStart>().Start();

        // Assert
        Assert.Same(first.ServiceProvider.GetRequiredService<ICultureState>(), second.ServiceProvider.GetRequiredService<ICultureState>());
    }

    [Fact]
    public async Task Should_give_each_web_view_a_culture_state_and_a_switch_of_its_own_under_a_culture_per_web_view()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        await using var provider = AddPlatform(new ServiceCollection())
            .AddFormaWebViewIntegration(options => options.CultureArrangement = CultureArrangement.Independent)
            .BuildServiceProvider();
        await using var first = provider.CreateAsyncScope();
        await using var second = provider.CreateAsyncScope();

        // Act
        first.ServiceProvider.GetRequiredService<ICultureScopeStart>().Start();
        second.ServiceProvider.GetRequiredService<ICultureScopeStart>().Start();

        // Assert
        Assert.Multiple(
            () => Assert.NotSame(first.ServiceProvider.GetRequiredService<ICultureState>(), second.ServiceProvider.GetRequiredService<ICultureState>()),
            () => Assert.NotSame(first.ServiceProvider.GetRequiredService<ICultureSwitch>(), second.ServiceProvider.GetRequiredService<ICultureSwitch>()));
    }

    [Fact]
    public void Should_register_the_default_input_policy()
    {
        // Act
        using var provider = new ServiceCollection().AddFormaWebViewIntegration().BuildServiceProvider();

        // Assert
        Assert.Same(InputRouterOptions.Default, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_register_the_application_s_input_policy()
    {
        // Arrange
        var policy = new InputRouterOptions { Escape = DismissBehavior.Ignore };

        // Act
        using var provider = new ServiceCollection().AddFormaWebViewIntegration(options => options.InputRouting = policy).BuildServiceProvider();

        // Assert
        Assert.Same(policy, provider.GetRequiredService<InputRouterOptions>());
    }
}
