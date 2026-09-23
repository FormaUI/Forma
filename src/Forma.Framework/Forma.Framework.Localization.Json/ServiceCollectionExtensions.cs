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
    /// <summary>Registers locales reading the files embedded in each type's assembly, with the locale packs the application was built against and then the host's own files laid over them.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services)
        => services.AddJsonLocalization(new LocalizationOptions());

    /// <summary>Registers locales configured by the options, reading the locale packs the application was built against before any the options name.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services, LocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAddSingleton<IStringLocaleFactory>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            // The application's build recorded the packs it references in its own assembly, the entry assembly,
            // which is the composition root's to read rather than a service's; an application the runtime entered
            // without one has none recorded.
            var configured = Assembly.GetEntryAssembly() is { } application
                ? options.WithRecordedPacks(application, loggerFactory.CreateLogger(typeof(LocalePackLoader)))
                : options;

            return new JsonStringLocaleFactory(configured, loggerFactory);
        });
        services.TryAddSingleton(typeof(IStringLocale<>), typeof(JsonStringLocale<>));

        return services;
    }
}
