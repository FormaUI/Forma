namespace Forma.Framework.Localization.Json;

using Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers JSON-backed localization in a service collection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers locales reading files that sit beside the types they translate.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services)
        => services.AddJsonLocalization(new JsonLocalizationOptions());

    /// <summary>Registers locales reading files from the folder the options name.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services, JsonLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton<IStringLocaleFactory>(new JsonStringLocaleFactory(options));
        services.TryAddSingleton(typeof(IStringLocale<>), typeof(JsonStringLocale<>));

        return services;
    }
}
