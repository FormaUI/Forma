namespace Forma.Framework.Culture.Blazor.WebAssembly;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

/// <summary>Registers the culture services a WebAssembly client needs.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the runtime's culture state and the integration that applies each accepted pair to it and to the process's defaults, with, under coordinated pages, its registration with the page's culture coordinator at startup, the switch that requests through it and the switch's policy, or, under an independent client culture, the local switch.</summary>
    /// <remarks>
    /// A WebAssembly runtime is one user scope in one process, so each of these is one per runtime: the startup, which the runtime runs
    /// outside the scope its components resolve from, registers the state they read. A client that registers its own culture state
    /// registers it once per runtime too.
    /// </remarks>
    /// <param name="services">The collection to register into.</param>
    /// <param name="arrangement">How the page treats the client's culture.</param>
    /// <param name="policy">How a switch persists the pair the page accepts, which a standalone app's page publishes; <see langword="null"/> takes <see cref="CultureSwitchPolicy.Default"/>.</param>
    /// <returns>The same collection.</returns>
    /// <exception cref="InvalidOperationException">A policy is given for an independent client culture, which persists nothing.</exception>
    public static IServiceCollection AddFormaCultureWebAssemblyIntegration(this IServiceCollection services, CultureArrangement arrangement = CultureArrangement.Coordinated, CultureSwitchPolicy? policy = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (!Enum.IsDefined(arrangement))
        {
            throw new ArgumentOutOfRangeException(nameof(arrangement), arrangement, "The arrangement is neither coordinated nor independent.");
        }

        if (arrangement is CultureArrangement.Independent && policy is not null)
        {
            throw new InvalidOperationException("An independent client culture changes its own runtime alone and persists nothing, so it takes no switch policy; leave the policy unset, or coordinate the client's culture with the page.");
        }

        // Ahead of the neutral registrations, which then keep these.
        services.TryAddSingleton<ICultureState>(provider => new DefaultCultureState(provider.GetService<ICultureProvider>()));
        services.TryAddSingleton<ICultureStateIntegration>(provider => new ProcessCultureIntegration(provider.GetRequiredService<ICultureState>()));
        services.AddFormaCultureServices();

        if (arrangement is CultureArrangement.Independent)
        {
            services.TryAddSingleton<ICultureSwitch, LocalCultureSwitch>();

            return services;
        }

        services.AddFormaCultureSwitchPolicy(policy);
        services.TryAddSingleton(CultureCoordinatorRegistration.Create);
        services.AddFormaCultureCoordination();
        services.TryAddSingleton<ICultureSwitch>(provider => new CoordinatedCultureSwitch(provider.GetRequiredService<CultureCoordinatorRegistration>(), provider.GetService<ICultureAddressing>()));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CultureCoordinatorStartup>(provider => new CultureCoordinatorStartup(
            provider.GetRequiredService<CultureCoordinatorRegistration>(),
            provider.GetRequiredService<ICultureState>(),
            provider.GetRequiredService<CultureSwitchPolicy>(),
            provider.GetRequiredService<NavigationManager>(),
            provider.GetService<ICultureAddressing>())));

        return services;
    }
}
