namespace Forma.Blazor.Tests;

using Forma.Blazor.Features.Emojis;
using Forma.Framework.Culture;
using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Unicode;
using Forma.Framework.Icons;
using Forma.Framework.Icons.Glyphs;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Localization;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_sprite_loader_once_per_circuit()
    {
        // Act
        var services = new ServiceCollection().AddFormaUI();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IEmojiSpriteLoader));
        Assert.Multiple(
            () => Assert.Equal(typeof(EmojiSpriteLoader), descriptor.ImplementationType),
            () => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
    }

    [Fact]
    public void Should_register_one_culture_state_per_circuit()
    {
        // Act
        var services = new ServiceCollection().AddFormaUI();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICultureState));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void Should_register_the_default_input_policy()
    {
        using var provider = new ServiceCollection().AddFormaUI().BuildServiceProvider();

        Assert.Same(InputRouterOptions.Default, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_register_the_host_input_policy()
    {
        var policy = new InputRouterOptions { Escape = DismissBehavior.Ignore };

        using var provider = new ServiceCollection().AddFormaUI(policy).BuildServiceProvider();

        Assert.Same(policy, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_leave_a_policy_the_host_registered_first()
    {
        var policy = new InputRouterOptions { Routes = [new InputRoute("app.palette", KeyBinding.Shortcut("k"))] };
        var services = new ServiceCollection();
        services.AddSingleton(policy);

        using var provider = services.AddFormaUI().BuildServiceProvider();

        Assert.Same(policy, provider.GetRequiredService<InputRouterOptions>());
    }

    [Fact]
    public void Should_register_the_bundled_artwork()
    {
        using var provider = new ServiceCollection().AddFormaUI().BuildServiceProvider();

        Assert.Multiple(
            () => Assert.IsType<GlyphIconProvider>(provider.GetRequiredService<IIconProvider>()),
            () => Assert.IsType<UnicodeEmojiProvider>(provider.GetRequiredService<IEmojiProvider>()));
    }

    [Fact]
    public void Should_resolve_what_every_component_injects_where_the_host_registered_none_of_it()
    {
        using var provider = new ServiceCollection().AddFormaUI().BuildServiceProvider();

        Assert.Multiple(
            () => Assert.Same(NullLoggerFactory.Instance, provider.GetRequiredService<ILoggerFactory>()),
            () => Assert.IsType<NullLogger<ServiceCollectionExtensionsTests>>(provider.GetRequiredService<ILogger<ServiceCollectionExtensionsTests>>()),
            () => Assert.Same(NullStringLocaleFactory.Instance, provider.GetRequiredService<IStringLocaleFactory>()),
            () => Assert.Same(UnavailableJSRuntime.Instance, provider.GetRequiredService<IJSRuntime>()));
    }

    [Fact]
    public void Should_leave_the_runtime_logging_and_localization_the_host_registered_first()
    {
        using var loggerFactory = new FakeLoggerFactory();
        var localeFactory = new StubStringLocaleFactory(new Dictionary<string, string>());
        var runtime = new BunitJSInterop().JSRuntime;
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(loggerFactory);
        services.AddSingleton<IStringLocaleFactory>(localeFactory);
        services.AddSingleton(runtime);

        using var provider = services.AddFormaUI().BuildServiceProvider();

        Assert.Multiple(
            () => Assert.Same(loggerFactory, provider.GetRequiredService<ILoggerFactory>()),
            () => Assert.Same(localeFactory, provider.GetRequiredService<IStringLocaleFactory>()),
            () => Assert.Same(runtime, provider.GetRequiredService<IJSRuntime>()));
    }
}
