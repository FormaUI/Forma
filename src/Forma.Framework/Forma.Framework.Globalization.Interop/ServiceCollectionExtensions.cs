namespace Forma.Framework.Globalization.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Registers the document culture synchronizer this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state and the document culture synchronizer that keeps the document on it, one per user scope.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaGlobalizationDocumentCultureSynchronizer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaGlobalizationServices();
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
        services.TryAddScoped<DocumentCultureSynchronizer>();

        return services;
    }
}
