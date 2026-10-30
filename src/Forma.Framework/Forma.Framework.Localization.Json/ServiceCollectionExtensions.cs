namespace Forma.Framework.Localization.Json;

using System.Reflection;

using Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Registers JSON-backed localization in a service collection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers JSON localization with the default options.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services)
        => services.AddJsonLocalization(new LocalizationOptions());

    /// <summary>Registers JSON localization configured by the given options, with the locale packs the application was built against placed before the ones the options name, and the host's own locale files its build embedded in it.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services, LocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAddSingleton<IStringLocaleFactory>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            // The application's build records the packs it references in the entry assembly, and a client's embeds the host's files there.
            var configured = Assembly.GetEntryAssembly() is { } application
                ? options.WithRecordedLocales(application, loggerFactory.CreateLogger(typeof(LocalePackLoader)))
                : options;

            return new JsonStringLocaleFactory(configured, loggerFactory);
        });
        services.TryAddSingleton(typeof(IStringLocale<>), typeof(JsonStringLocale<>));

        return services;
    }
}
