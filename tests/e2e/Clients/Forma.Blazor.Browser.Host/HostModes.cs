namespace Forma.Blazor.Browser.Host;

using System.Globalization;

using Forma.Framework.Culture;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>How the host routes its pages and switches culture: <c>FORMA_BROWSER_ROUTES</c> <c>interactive</c> makes its router interactive on the Server, <c>navigation-server</c> and <c>navigation-webassembly</c> render the recording router over the navigation pages in that runtime, and anything else renders statically with islands; <c>FORMA_BROWSER_CULTURE</c> <c>state</c> switches the culture state alone and <c>cookie</c> persists a switch in the culture cookie request localization reads, where anything else switches through culture addresses; <c>FORMA_BROWSER_REQUEST_CULTURE</c> names the culture every request runs in, <c>invariant</c> for the invariant culture; and <c>FORMA_BROWSER_ARRANGEMENT</c> <c>independent</c> declares the client's culture its own, with culture capture off, where anything else coordinates it.</summary>
public static class HostModes
{
    /// <summary>Whether the culture is switched through culture addresses.</summary>
    public static bool UsesCultureAddresses { get; } = Environment.GetEnvironmentVariable("FORMA_BROWSER_CULTURE") is not ("state" or "cookie");

    /// <summary>Whether a switch is persisted in the culture cookie, which request localization reads.</summary>
    public static bool UsesCultureCookie { get; } = Environment.GetEnvironmentVariable("FORMA_BROWSER_CULTURE") == "cookie";

    /// <summary>The arrangement's variable as the host's environment names it, which the host hands each WebAssembly runtime.</summary>
    public static string? ArrangementVariable { get; } = Environment.GetEnvironmentVariable("FORMA_BROWSER_ARRANGEMENT");

    /// <summary>How the pages treat the client's culture.</summary>
    public static CultureArrangement Arrangement { get; } = ArrangementVariable == "independent"
        ? CultureArrangement.Independent
        : CultureArrangement.Coordinated;

    /// <summary>The culture every request runs in, whatever the machine's, where the host names one.</summary>
    public static CultureInfo? RequestCulture { get; } = Environment.GetEnvironmentVariable("FORMA_BROWSER_REQUEST_CULTURE") switch
    {
        null or "" => null,
        "invariant" => CultureInfo.InvariantCulture,
        var name => CultureInfo.GetCultureInfo(name),
    };

    /// <summary>Whether the router is interactive.</summary>
    public static bool IsInteractiveRouting { get; } = Environment.GetEnvironmentVariable("FORMA_BROWSER_ROUTES") == "interactive";

    /// <summary>The render mode of the router, none where the pages are islands.</summary>
    public static IComponentRenderMode? Routes { get; } = IsInteractiveRouting ? RenderMode.InteractiveServer : null;

    /// <summary>The render mode of the recording router, where the host renders it in place of its own.</summary>
    public static IComponentRenderMode? NavigationRoutes { get; } = Environment.GetEnvironmentVariable("FORMA_BROWSER_ROUTES") switch
    {
        "navigation-server" => RenderMode.InteractiveServer,
        "navigation-webassembly" => RenderMode.InteractiveWebAssembly,
        _ => null,
    };

    /// <summary>The render mode of an island, none where the whole tree is interactive already.</summary>
    public static IComponentRenderMode? Island { get; } = IsInteractiveRouting ? null : RenderMode.InteractiveServer;
}
