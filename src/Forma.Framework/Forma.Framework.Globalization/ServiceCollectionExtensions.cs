namespace Forma.Framework.Globalization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the culture services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the culture state each user scope holds.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaGlobalizationCoreServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<ICultureState>(_ => new DefaultCultureState());

        return services;
    }
}
