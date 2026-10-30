namespace Forma.Blazor;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Forma on a WebAssembly client.</summary>
public static class WebAssemblyServiceCollectionExtensions
{
    /// <summary>Registers every default the components rely on in a WebAssembly client, leaving in place whatever the client registered first.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="configure">Sets the WebAssembly registration's options.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddFormaUIWebAssembly(this IServiceCollection services, Action<UIWebAssemblyOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new UIWebAssemblyOptions();
        configure?.Invoke(options);

        return services.AddFormaUI(options.InputRouting);
    }
}
