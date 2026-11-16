namespace Forma.Framework.Localization.StringLocalizerAdapter;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

/// <summary>Registers Forma's localization behind Microsoft's string localizer contracts.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers an <see cref="IStringLocalizerFactory"/> over the registered <see cref="IStringLocaleFactory"/>, a singleton, and <see cref="IStringLocalizer{T}"/>, each only where nothing is registered for it yet.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaStringLocalizerAdapter(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IStringLocalizerFactory>(provider => new StringLocaleLocalizerFactory(provider.GetStringLocaleFactory()));
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

        return services;
    }
}
