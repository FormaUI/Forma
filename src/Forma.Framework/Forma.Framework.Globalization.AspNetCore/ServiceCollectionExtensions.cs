namespace Forma.Framework.Globalization.AspNetCore;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the culture services an Interactive Server host needs.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state and the circuit handler that applies it before every inbound activity.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaGlobalizationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaGlobalizationCoreServices();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, CultureCircuitHandler>(provider => new CultureCircuitHandler(provider.GetRequiredService<ICultureState>())));

        return services;
    }
}
