namespace Forma.Blazor.Tests;

using Forma.Blazor.Features.Emojis;
using Forma.Framework.Culture;
using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Twemoji;
using Forma.Framework.Icons;
using Forma.Framework.Input;
using Forma.Framework.Input.Keyboard;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization.Json;
using Forma.Tests.Interop;
using Forma.Tests.Localization;
using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_register_the_sprite_loader_once_per_circuit()
    {
        // Act
        var services = new ServiceCollection().AddFormaUI();

        // Assert
        var descriptor = Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IEmojiSpriteLoader));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        Assert.Multiple(
            () => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime),
            () => Assert.IsType<EmojiSpriteLoader>(scope.ServiceProvider.GetRequiredService<IEmojiSpriteLoader>()));
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
    public void Should_register_no_artwork_of_its_own()
    {
        // Act
        var services = new ServiceCollection().AddFormaUI();

        // Assert
        Assert.Multiple(
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IIconProvider)),
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IEmojiProvider)));
    }

    [Fact]
    public void Should_register_the_platform_s_logging_and_no_stand_in_for_the_runtime_or_the_localization()
    {
        // Act
        var services = new ServiceCollection().AddFormaUI();

        // Assert
        using var provider = services.BuildServiceProvider();
        Assert.Multiple(
            () => Assert.IsType<LoggerFactory>(provider.GetRequiredService<ILoggerFactory>()),
            () => Assert.IsType<Logger<ServiceCollectionExtensionsTests>>(provider.GetRequiredService<ILogger<ServiceCollectionExtensionsTests>>()),
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IJSRuntime)),
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IStringLocaleFactory)),
            () => Assert.Same(UnavailableJSRuntime.Instance, provider.GetJSRuntime()),
            () => Assert.Same(NullStringLocaleFactory.Instance, provider.GetStringLocaleFactory()));
    }

    [Fact]
    public void Should_take_the_runtime_logging_localization_and_artwork_registered_after_it()
    {
        // Arrange — each registered as its own registrar does, only where nothing is registered for it yet
        IJSRuntime runtime = new FakeJSRuntime((_, _) => null);
        using var loggerProvider = new FakeLoggerProvider();
        var services = new ServiceCollection().AddFormaUI();

        // Act
        services.TryAddScoped(_ => runtime);
        services.AddLogging(logging => logging.AddProvider(loggerProvider));
        services.AddJsonLocalization();
        services.AddFormaTwemojiPack();

        // Assert
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ILogger<ServiceCollectionExtensionsTests>>().Log(LogLevel.Information, default, "Logged.", null, (message, _) => message);
        Assert.Multiple(
            () => Assert.Same(runtime, scope.ServiceProvider.GetJSRuntime()),
            () => Assert.Contains(loggerProvider.Entries, entry => entry.Message == "Logged."),
            () => Assert.IsType<JsonStringLocaleFactory>(scope.ServiceProvider.GetStringLocaleFactory()),
            () => Assert.IsType<TwemojiEmojiProvider>(scope.ServiceProvider.GetRequiredService<IEmojiProvider>()));
    }

    [Fact]
    public void Should_leave_the_runtime_logging_and_localization_the_host_registered_first()
    {
        using var loggerFactory = new FakeLoggerFactory();
        var localeFactory = new StubStringLocaleFactory(new Dictionary<string, string>());
        IJSRuntime runtime = new FakeJSRuntime((_, _) => null);
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
