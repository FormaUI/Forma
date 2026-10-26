namespace Forma.Framework.Globalization.Interop.Server;

using Forma.Framework.Globalization.Interop;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the culture services an Interactive Server host needs.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state, the document language that keeps the document on it, and the circuit handler that applies the state before every inbound activity and starts the document language.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaGlobalizationCircuitIntegration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaDocumentLanguage();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, CultureCircuitHandler>(provider => new CultureCircuitHandler(provider.GetRequiredService<ICultureState>(), provider.GetRequiredService<DocumentLanguage>())));

        return services;
    }
}
