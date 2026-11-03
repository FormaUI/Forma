namespace Forma.Framework.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the window-management services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the workspace's window manager.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaWindowServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<WindowManager>();

        return services;
    }
}
