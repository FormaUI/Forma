namespace Forma.Blazor;

using Forma.Framework.Browser.Blazor.Server;
using Forma.Framework.Culture.Blazor.Server;

using Microsoft.Extensions.Hosting;

/// <summary>Registers Forma on a server host.</summary>
public static class HostApplicationBuilderExtensions
{
    /// <summary>Registers every default the components rely on and the circuit halves an interactive server needs, leaving in place whatever the host registered first.</summary>
    /// <param name="builder">The host's builder.</param>
    /// <param name="configure">Sets the server registration's options.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IHostApplicationBuilder AddFormaUIServer(this IHostApplicationBuilder builder, Action<UIServerOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var options = new UIServerOptions();
        configure?.Invoke(options);

        // Ahead of the neutral registration, so the circuit's switch requests through the page's coordinator.
        builder.Services.AddFormaCultureServer(options.CultureSwitchPolicy);
        builder.Services.AddFormaUI(options.InputRouting);
        builder.Services.AddFormaBrowserServer();

        return builder;
    }
}
