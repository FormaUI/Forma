namespace Forma.Framework.Culture.Blazor.Server;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the culture services an Interactive Server host needs.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state, the document culture synchronizer that keeps the document on it, and the circuit handler that applies the state before every inbound activity and starts the document culture synchronizer.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaCultureServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaCultureBrowserSynchronization();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, CultureCircuitHandler>(provider => new CultureCircuitHandler(provider.GetRequiredService<ICultureState>(), provider.GetRequiredService<DocumentCultureSynchronizer>())));

        return services;
    }
}
