namespace Forma.Blazor;

using Forma.Framework.Emojis.Unicode;
using Forma.Framework.Feedback;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Sanitization.Allowlist;
using Forma.Framework.Timing;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>Registers the services Forma's components need to function.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers every default the components rely on, leaving in place whatever a host registered first.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="inputRouting">The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>Artwork is not registered here; an icon or emoji pack is a package a host adds and calls for itself.</remarks>
    public static IServiceCollection AddFormaUI(this IServiceCollection services, InputRouterOptions? inputRouting = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaTimingServices();
        services.AddFormaFeedbackServices();
        services.AddFormaSanitizationServices();

        services.AddFormaEmojiServices();

        services.AddFormaInputRouter(inputRouting);

        return services;
    }
}
