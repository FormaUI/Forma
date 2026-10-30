namespace Forma.Blazor.Browser.Host;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>How the host routes its pages and switches culture: <c>FORMA_BROWSER_ROUTES</c> <c>interactive</c> makes its router interactive on the Server, where anything else renders statically with islands, and <c>FORMA_BROWSER_CULTURE</c> <c>state</c> switches the culture state alone, where anything else switches through culture addresses.</summary>
public static class HostModes
{
    /// <summary>Whether the culture is switched through culture addresses.</summary>
    public static bool UsesCultureAddresses { get; } = Environment.GetEnvironmentVariable("FORMA_BROWSER_CULTURE") != "state";

    /// <summary>Whether the router is interactive.</summary>
    public static bool IsInteractiveRouting { get; } = Environment.GetEnvironmentVariable("FORMA_BROWSER_ROUTES") == "interactive";

    /// <summary>The render mode of the router, none where the pages are islands.</summary>
    public static IComponentRenderMode? Routes { get; } = IsInteractiveRouting ? RenderMode.InteractiveServer : null;

    /// <summary>The render mode of an island, none where the whole tree is interactive already.</summary>
    public static IComponentRenderMode? Island { get; } = IsInteractiveRouting ? null : RenderMode.InteractiveServer;
}
