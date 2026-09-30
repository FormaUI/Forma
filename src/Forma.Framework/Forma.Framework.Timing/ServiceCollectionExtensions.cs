namespace Forma.Framework.Timing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the timing services.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the debouncer and the system clock.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaTimingServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Each debounced surface holds its own pending request.
        services.TryAddTransient<IDebounceable, Debouncer>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
