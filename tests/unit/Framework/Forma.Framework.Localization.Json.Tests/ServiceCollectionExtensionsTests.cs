namespace Forma.Framework.Localization.Json.Tests;

using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void Should_log_through_the_logging_a_host_registers_after_it()
    {
        // Arrange
        using var loggerProvider = new FakeLoggerProvider();
        var services = new ServiceCollection().AddJsonLocalization();

        // Act
        services.AddLogging(logging => logging.AddProvider(loggerProvider));

        // Assert
        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(ServiceCollectionExtensionsTests)).Log(LogLevel.Information, default, "Logged.", null, (message, _) => message);
        Assert.Contains(loggerProvider.Entries, entry => entry.Message == "Logged.");
    }

    [Fact]
    public void Should_leave_the_locale_factory_a_host_registered_first()
    {
        // Arrange
        var localeFactory = new JsonStringLocaleFactory();
        var services = new ServiceCollection().AddSingleton<IStringLocaleFactory>(localeFactory);

        // Act
        services.AddJsonLocalization();

        // Assert
        using var provider = services.BuildServiceProvider();
        Assert.Same(localeFactory, provider.GetRequiredService<IStringLocaleFactory>());
    }
}
