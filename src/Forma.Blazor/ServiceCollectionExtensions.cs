namespace Forma.Blazor;

using Forma.Blazor.Features.Emojis;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Culture;
using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Emojis.Unicode;
using Forma.Framework.Events;
using Forma.Framework.Feedback;
using Forma.Framework.Icons.Glyphs;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Localization;
using Forma.Framework.Sanitization;
using Forma.Framework.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

/// <summary>Registers the services Forma's components need in every render mode.</summary>
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

        // Fallbacks for what every component injects: a discarding logger, the source-language locale, and a runtime that throws at the first call.
        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
        services.TryAddSingleton<IStringLocaleFactory>(NullStringLocaleFactory.Instance);
        services.TryAddSingleton<IJSRuntime>(UnavailableJSRuntime.Instance);

        services.AddFormaEventServices();
        services.AddFormaCultureCoordination();
        // Where no render-mode half registered a switch, a switch applies to the user scope's own state alone.
        services.TryAddScoped<ICultureSwitch, LocalCultureSwitch>();
        services.AddFormaWindowServices();
        services.AddFormaFeedbackServices();
        services.AddFormaSanitizationServices();

        services.AddFormaEmojiServices();
        // Scoped to the circuit, whose own document holds the adopted sheets.
        services.TryAddScoped<IEmojiSpriteLoader, EmojiSpriteLoader>();
        services.AddFormaGlyphServices();

        services.AddFormaInputRouter(inputRouting);
        services.AddFormaBrowserServices();

        return services;
    }
}
