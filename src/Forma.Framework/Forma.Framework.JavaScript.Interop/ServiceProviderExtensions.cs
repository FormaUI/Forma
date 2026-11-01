namespace Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>Resolves the JS runtime a host's platform registers.</summary>
public static class ServiceProviderExtensions
{
    /// <summary>Gets the JS runtime registered in <paramref name="services"/>, or <see cref="UnavailableJSRuntime.Instance"/> in a container with none, as a framework test's or <c>HtmlRenderer</c>'s is.</summary>
    /// <param name="services">The services to resolve from.</param>
    /// <returns>The JS runtime.</returns>
    public static IJSRuntime GetJSRuntime(this IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.GetService(typeof(IJSRuntime)) as IJSRuntime ?? UnavailableJSRuntime.Instance;
    }
}
