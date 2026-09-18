namespace Forma.Framework.Localization.Json;

using Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Registers JSON-backed localization in a service collection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers locales reading the files embedded in each type's assembly, with the host's own laid over them.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services)
        => services.AddJsonLocalization(new LocalizationOptions());

    /// <summary>Registers locales reading files from the folder the options name.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services, LocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAddSingleton<IStringLocaleFactory>(provider => new JsonStringLocaleFactory(options, provider.GetRequiredService<ILoggerFactory>()));
        services.TryAddSingleton(typeof(IStringLocale<>), typeof(JsonStringLocale<>));

        return services;
    }
}
