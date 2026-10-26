namespace Forma.Framework.Globalization.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Registers the document language this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state and the document language that keeps the document on it, one per user scope.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaDocumentLanguage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaGlobalizationCoreServices();
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
        services.TryAddScoped<DocumentLanguage>();

        return services;
    }
}
