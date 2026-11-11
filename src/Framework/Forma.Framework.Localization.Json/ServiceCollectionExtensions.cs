namespace Forma.Framework.Localization.Json;

using System.Reflection;

using Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

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

        services.AddLogging();
        services.TryAddSingleton<IStringLocaleFactory>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            // The application's build records the packs it references in its own assembly, and embeds the host's files there.
            var logger = loggerFactory.CreateLogger(typeof(LocaleAssemblyLoader));
            var configuredOptions = LocaleAssemblyLoader.GetApplicationAssemblies(Assembly.GetEntryAssembly(), AppDomain.CurrentDomain.GetAssemblies())
                .Aggregate(options, (recorded, application) => recorded.WithRecordedLocales(application, logger));

            return new JsonStringLocaleFactory(configuredOptions, loggerFactory);
        });
        services.TryAddSingleton(typeof(IStringLocale<>), typeof(JsonStringLocale<>));

        return services;
    }
}
