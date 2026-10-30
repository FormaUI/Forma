namespace Forma.Framework.Culture.Blazor.Server;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the culture services an Interactive Server host needs.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state, its registration with the page's culture coordinator, the switch that requests through the coordinator, the policy the page publishes for it, and the circuit handler that applies the state before every inbound activity and registers it with each connection.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="policy">How a switch persists the pair it accepts; <see langword="null"/> takes <see cref="CultureSwitchPolicy.Default"/>.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaCultureServer(this IServiceCollection services, CultureSwitchPolicy? policy = null)
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

        services.AddFormaCultureCoordination();
        services.TryAddSingleton(policy ?? CultureSwitchPolicy.Default);
        services.TryAddScoped<ICultureSwitch>(provider => new CoordinatedCultureSwitch(provider.GetRequiredService<CultureCoordinatorRegistration>(), provider.GetService<ICultureAddressing>()));
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, CultureCircuitHandler>(provider => new CultureCircuitHandler(provider.GetRequiredService<ICultureState>(), provider.GetRequiredService<CultureCoordinatorRegistration>())));

        return services;
    }
}
