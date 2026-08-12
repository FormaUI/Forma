namespace Forma.Framework.Localization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

/// <summary>Registers JSON-backed localization in a service collection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers locales reading files that sit beside the types they translate.</summary>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services)
        => services.AddJsonLocalization(new JsonLocalizationOptions());

    /// <summary>Registers locales reading files from the folder the options name.</summary>
    /// <remarks>
    /// Also registers the framework's <see cref="IStringLocalizer"/> and <see cref="IStringLocalizerFactory"/>
    /// through the adapters, so a consumer on either contract resolves.
    /// </remarks>
    public static IServiceCollection AddJsonLocalization(this IServiceCollection services, JsonLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        services.TryAddSingleton<IStringLocaleFactory>(new JsonStringLocaleFactory(options));
        services.TryAddSingleton(typeof(IStringLocale<>), typeof(JsonStringLocale<>));

        services.TryAddSingleton<IStringLocalizerFactory>(
            static provider => new StringLocalizerFactoryAdapter(provider.GetRequiredService<IStringLocaleFactory>()));
        services.TryAddSingleton(typeof(IStringLocalizer<>), typeof(StringLocalizerAdapter<>));

        return services;
    }
}
