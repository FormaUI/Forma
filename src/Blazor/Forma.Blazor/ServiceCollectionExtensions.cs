namespace Forma.Blazor;

using System.Diagnostics.CodeAnalysis;

using Forma.Blazor.Components.Page;
using Forma.Blazor.Features.Emojis;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Culture;
using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Events;
using Forma.Framework.Feedback;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Framework.JavaScript.Interop;
using Forma.Framework.Sanitization;
using Forma.Framework.Windows;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

/// <summary>Registers the services Forma's components need in every render mode.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers every default the components rely on, leaving in place whatever a host registered first.</summary>
    /// <param name="services">The collection to register into.</param>
    /// <param name="inputRouting">The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <remarks>
    /// It registers the platform's logging and no stand-in for a service the platform or a package supplies, so a host registers
    /// Forma, its platform's services, its localization and an artwork pack in any order: a component falls back to the source text
    /// where no localization is registered, to the bundled glyphs and the reader's font where no artwork is, and to a runtime that
    /// throws at its first call where no JS runtime is. A value the browser sends a component through the transfer accepts the
    /// <see cref="JSTransferOptions"/> a host registers, and <see cref="JSTransferOptions.Default"/>'s 16 MiB where it registers none.
    /// </remarks>
    // The head outlet a server's page head starts in the page's runtime, which no client's own code names.
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(FormaHeadOutlet))]
    public static IServiceCollection AddFormaUI(this IServiceCollection services, InputRouterOptions? inputRouting = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();

        services.AddFormaEventServices();
        services.AddFormaCultureCoordination();
        services.AddFormaWindowServices();
        services.AddFormaFeedbackServices();
        services.AddFormaSanitizationServices();

        // Scoped to the circuit, whose own document holds the adopted sheets.
        services.TryAddScoped<IEmojiSpriteLoader>(provider => new EmojiSpriteLoader(
            provider.GetJSRuntime(),
            provider.GetRequiredService<ILogger<EmojiSpriteLoader>>(),
            provider.GetService<NavigationManager>()));

        services.AddFormaInputRouter(inputRouting);
        services.AddFormaBrowserServices();

        return services;
    }
}
