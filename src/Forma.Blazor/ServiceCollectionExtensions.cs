namespace Forma.Blazor;

using Forma.Blazor.Features.Emojis;
using Forma.Framework.Core.Interop;
using Forma.Framework.Emojis.Unicode;
using Forma.Framework.Feedback;
using Forma.Framework.Icons.Glyphs;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.Localization;
using Forma.Framework.Sanitization;
using Forma.Framework.Timing;
using Forma.Framework.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

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

        // What every component injects resolves even where the host registered none of it: the logger
        // discards, the locale speaks the source language, and the runtime refuses at the first call.
        services.TryAddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));
        services.TryAddSingleton<IStringLocaleFactory>(NullStringLocaleFactory.Instance);
        services.TryAddSingleton<IJSRuntime>(UnavailableJSRuntime.Instance);

        services.AddFormaTimingServices();
        services.AddFormaWindowServices();
        services.AddFormaFeedbackServices();
        services.AddFormaSanitizationServices();

        services.AddFormaEmojiServices();
        // Scoped, because the sheets it has adopted are the ones in this circuit's own document.
        services.TryAddScoped<EmojiSpriteLoader>();
        services.AddFormaGlyphServices();

        services.AddFormaInputRouter(inputRouting);

        return services;
    }
}
