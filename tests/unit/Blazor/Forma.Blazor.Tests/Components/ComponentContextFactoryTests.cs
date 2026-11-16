namespace Forma.Blazor.Tests.Components;

using System.Globalization;

using Forma.Blazor.Components;
using Forma.Framework.Culture;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Culture;
using Forma.Tests.Localization;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;

public sealed class ComponentContextFactoryTests
{
    [Fact]
    public async Task Should_make_a_context_over_the_scope_s_culture_state_reading_the_source_type_s_strings_and_logging_under_the_component_type()
    {
        // Arrange
        var localeFactory = new StubStringLocaleFactory(new Dictionary<string, string> { ["Bold"] = "Negrita" });
        var cultureState = new FakeCultureState(CultureInfo.GetCultureInfo("es"));
        using var loggerFactory = new FakeLoggerFactory();
        using var serviceProvider = CreateServiceProvider(cultureState, loggerFactory, services => services.AddSingleton<IStringLocaleFactory>(localeFactory));
        var factory = new ComponentContextFactory(serviceProvider);

        // Act
        await using var context = factory.Create(typeof(ComponentContextFactoryTests), () => typeof(ComponentContext));
        var text = context.Locale["Bold"].Value;
        context.Logger.Log(LogLevel.Debug, default, "Logged", null, static (state, _) => state);

        // Assert
        Assert.Multiple(
            () => Assert.Same(cultureState, context.CultureState),
            () => Assert.Equal("Negrita", text),
            () => Assert.Equal([typeof(ComponentContext)], localeFactory.RequestedTypes),
            () => Assert.Equal(typeof(ComponentContextFactoryTests).FullName, Assert.Single(loggerFactory.Entries).Category));
    }

    [Fact]
    public async Task Should_ask_for_the_source_type_only_as_the_component_first_reads_a_string()
    {
        // Arrange
        var asked = 0;
        using var loggerFactory = new FakeLoggerFactory();
        using var serviceProvider = CreateServiceProvider(new FakeCultureState(CultureInfo.GetCultureInfo("en")), loggerFactory);
        var factory = new ComponentContextFactory(serviceProvider);

        // Act
        await using var context = factory.Create(typeof(ComponentContextFactoryTests), () => { asked++; return typeof(ComponentContext); });
        var before = asked;
        _ = context.Locale;
        _ = context.Locale;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(0, before),
            () => Assert.Equal(1, asked));
    }

    [Fact]
    public async Task Should_fall_back_to_the_library_s_defaults_where_the_host_registers_none()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        using var serviceProvider = CreateServiceProvider(new FakeCultureState(CultureInfo.GetCultureInfo("en")), loggerFactory);
        var factory = new ComponentContextFactory(serviceProvider);

        // Act
        await using var context = factory.Create(typeof(ComponentContextFactoryTests), () => typeof(ComponentContextFactoryTests));
        var call = await Record.ExceptionAsync(async () => await context.JS.InvokeVoidAsync("read"));

        // Assert
        Assert.Multiple(
            () => Assert.Equal("Bold", context.Locale["Bold"].Value),
            () => Assert.Same(JSTransferOptions.Default, context.JS.TransferOptions),
            () => Assert.IsType<JSRuntimeUnavailableException>(call));
    }

    [Fact]
    public async Task Should_initialize_the_scope_s_culture_support_as_it_makes_a_context()
    {
        // Arrange
        var initializer = new FakeCultureScopeInitializer();
        using var loggerFactory = new FakeLoggerFactory();
        using var serviceProvider = CreateServiceProvider(new FakeCultureState(CultureInfo.GetCultureInfo("en")), loggerFactory, services => services.AddSingleton<ICultureScopeInitializer>(initializer));
        var factory = new ComponentContextFactory(serviceProvider);

        // Act
        await using var context = factory.Create(typeof(ComponentContextFactoryTests), () => typeof(ComponentContextFactoryTests));

        // Assert
        Assert.Equal(1, initializer.InitializeCount);
    }

    private static ServiceProvider CreateServiceProvider(ICultureState cultureState, ILoggerFactory loggerFactory, Action<IServiceCollection>? register = null)
    {
        var services = new ServiceCollection().AddSingleton(cultureState).AddSingleton(loggerFactory);
        register?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
