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
}
