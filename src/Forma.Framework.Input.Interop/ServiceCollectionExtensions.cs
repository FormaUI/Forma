namespace Forma.Framework.Input.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the input router this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the input policy and the router that carries it to each circuit's document.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="options">The policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaInputRouter(this IServiceCollection services, InputRouterOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaInputServices(options);

        // Scoped, because the policy it pushes lands in this circuit's own document.
        services.TryAddScoped<InputRouter>();

        return services;
    }
}
