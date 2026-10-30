namespace Forma.Framework.Culture.Browser.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Registers the culture coordination this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state and the registration that keeps it on the pair the page's culture coordinator accepts, one per user scope.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaCultureCoordination(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaCultureServices();
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
        services.TryAddScoped<CultureCoordinatorRegistration>();

        return services;
    }

    /// <summary>Registers how a switch persists the pair the page accepts, which the page publishes for its coordinator.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="policy">How a switch persists the pair it accepts; <see langword="null"/> takes <see cref="CultureSwitchPolicy.Default"/>.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="ArgumentException">The cookie policy names no cookie or path, or sends its cookie from every site without securing it.</exception>
    public static IServiceCollection AddFormaCultureSwitchPolicy(this IServiceCollection services, CultureSwitchPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (policy is { Persistence: CultureSwitchPersistence.Cookie })
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(policy.CookieName, nameof(policy));
            ArgumentException.ThrowIfNullOrWhiteSpace(policy.CookiePath, nameof(policy));

            if (policy is { CookieSameSite: CultureCookieSameSite.None, IsCookieSecure: false })
            {
                throw new ArgumentException("A culture cookie sent from every site is a secure one, which a browser requires of it.", nameof(policy));
            }
        }

        services.TryAddSingleton(policy ?? CultureSwitchPolicy.Default);

        return services;
    }
}
