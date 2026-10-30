namespace Forma.Framework.Browser.Blazor.Server;

using Forma.Framework.Browser.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the browser services an Interactive Server host needs.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the browser services and the circuit handler that refreshes a circuit's observations.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaBrowserServer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaBrowserServices();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, BrowserCircuitHandler>(provider => new BrowserCircuitHandler(provider.GetRequiredService<DomBridge>())));

        return services;
    }
}
