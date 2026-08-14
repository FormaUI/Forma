namespace Forma.Framework.Localization;

using Forma.Framework.Localization.Adapter;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

/// <summary>Registers the adapters presenting a locale as the framework's localizer.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IStringLocalizer"/> and <see cref="IStringLocalizerFactory"/> over the registered locale factory.</summary>
    /// <remarks>A backend registers <see cref="IStringLocaleFactory"/>; these adapters present it on the framework's contract.</remarks>
    public static IServiceCollection AddStringLocalizerAdapter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IStringLocalizerFactory>(
            static provider => new StringLocalizerFactoryAdapter(provider.GetRequiredService<IStringLocaleFactory>()));
        services.TryAddSingleton(typeof(IStringLocalizer<>), typeof(StringLocalizerAdapter<>));

        return services;
    }
}
