namespace Forma.Framework.Timing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the timing services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the debouncer and the system clock this package ships, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaTimingServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Transient, because each debounced surface waits on its own pending request.
        services.TryAddTransient<IDebounceable, Debouncer>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
