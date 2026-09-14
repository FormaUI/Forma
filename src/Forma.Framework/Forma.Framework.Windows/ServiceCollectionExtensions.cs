namespace Forma.Framework.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the window-management services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the workspace's window manager, unless a host has already registered its own.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaWindowServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Scoped, because a circuit is one workspace.
        services.TryAddScoped<WindowManager>();

        return services;
    }
}
