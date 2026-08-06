namespace Forma.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

/// <summary>Registers JSON-backed localization in a service collection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers localizers reading files that sit beside the types they translate.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services)
        => services.AddJsonLocalization(new JsonLocalizationOptions());

    /// <summary>Registers localizers reading files from the folder the options name.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services, JsonLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton<IStringLocalizerFactory>(new JsonStringLocalizerFactory(options));
        services.TryAddSingleton(typeof(IStringLocalizer<>), typeof(JsonStringLocalizer<>));

        return services;
    }
}
