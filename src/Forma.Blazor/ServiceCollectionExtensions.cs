namespace Forma.Blazor;

using Forma.Framework.Emojis.Unicode;
using Forma.Framework.Feedback;
using Forma.Framework.Icons.Glyphs;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Sanitization;
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
    /// <remarks>The bundled glyphs and the reader's font are the icon and emoji providers unless the host registered its own or added an artwork pack first.</remarks>
    public static IServiceCollection AddFormaUI(this IServiceCollection services, InputRouterOptions? inputRouting = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddFormaTimingServices();
        services.AddFormaFeedbackServices();
        services.AddFormaSanitizationServices();

        services.AddFormaEmojiServices();
        services.AddFormaGlyphServices();

        services.AddFormaInputRouter(inputRouting);

        return services;
    }
}
