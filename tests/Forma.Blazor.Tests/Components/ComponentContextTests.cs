namespace Forma.Blazor.Tests.Components;

using Forma.Blazor.Components;
using Forma.Framework.Localization;
using Forma.Tests.Interop;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class ComponentContextTests
{
    [Fact]
    public async Task Should_create_each_service_once_and_only_when_first_read()
    {
        // Arrange
        var localeCreations = 0;
        var loggerCreations = 0;
        var runtimeCreations = 0;
        await using var context = new ComponentContext(
            () => { runtimeCreations++; return new FakeJSRuntime((_, _) => null); },
            () => { localeCreations++; return CreateLocale(); },
            () => { loggerCreations++; return NullLogger.Instance; });

        // Act
        var before = (localeCreations, loggerCreations, runtimeCreations);
        _ = context.Locale;
        _ = context.Locale;
        _ = context.JS;
        _ = context.JS;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal((0, 0, 0), before);
            Assert.Equal(1, localeCreations);
            Assert.Equal(1, loggerCreations);
            Assert.Equal(1, runtimeCreations);
        });
    }

    [Fact]
    public async Task Should_give_its_environment_its_own_identity_and_log()
    {
        // Arrange
        var logger = NullLogger.Instance;
        await using var context = new ComponentContext(() => new FakeJSRuntime((_, _) => null), CreateLocale, () => logger);

        // Act
        var environment = context.JS;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.NotEqual(Guid.Empty, context.RuntimeId);
            Assert.Equal(7, context.RuntimeId.Version);
            Assert.Equal(context.RuntimeId, environment.RuntimeId);
            Assert.Same(logger, environment.Logger);
            Assert.Same(context.Logger, environment.Logger);
        });
    }

    [Fact]
    public async Task Should_mark_itself_disposed_and_take_another_disposal_safely()
    {
        // Arrange
        var context = new ComponentContext(CreateRuntime, CreateLocale, CreateLogger);
        var before = context.IsDisposed;

        // Act
        await context.DisposeAsync();
        await context.DisposeAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.False(before);
            Assert.True(context.IsDisposed);
        });
    }

    [Fact]
    public void Should_refuse_a_missing_function()
    {
        // Act
        var withoutRuntime = Assert.Throws<ArgumentNullException>(() => new ComponentContext(null!, CreateLocale, CreateLogger));
        var withoutLocale = Assert.Throws<ArgumentNullException>(() => new ComponentContext(CreateRuntime, null!, CreateLogger));
        var withoutLogger = Assert.Throws<ArgumentNullException>(() => new ComponentContext(CreateRuntime, CreateLocale, null!));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("jsRuntime", withoutRuntime.ParamName);
            Assert.Equal("locale", withoutLocale.ParamName);
            Assert.Equal("logger", withoutLogger.ParamName);
        });
    }

    private static FakeJSRuntime CreateRuntime()
        => new((_, _) => null);

    private static IStringLocale CreateLocale()
        => NullStringLocaleFactory.Instance.Create(typeof(ComponentContextTests));

    private static NullLogger CreateLogger()
        => NullLogger.Instance;
}
