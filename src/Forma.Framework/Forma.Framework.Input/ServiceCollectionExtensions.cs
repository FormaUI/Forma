namespace Forma.Framework.Input;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the input policy this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the application-wide input policy.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="options">The policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaInputServices(this IServiceCollection services, InputRouterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(options ?? InputRouterOptions.Default);

        return services;
    }
}
