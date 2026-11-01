namespace Forma.Framework.Browser.Interop;

using Forma.Framework.Input.Interop;
using Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

/// <summary>Registers the browser services this package ships.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers the bridge of each JS runtime scope.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddFormaBrowserServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();

        services.TryAddScoped(provider => new DomBridge(provider.GetJSRuntime(), provider.GetRequiredService<ILogger<DomBridge>>(), provider.GetService<InputRouter>()));

        return services;
    }
}
