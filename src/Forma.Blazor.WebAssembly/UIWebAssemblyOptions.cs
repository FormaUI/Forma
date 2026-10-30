namespace Forma.Blazor;

using Forma.Framework.Culture;
using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Input;

/// <summary>The settings a client gives Forma's WebAssembly registration.</summary>
public sealed class UIWebAssemblyOptions
{
    /// <summary>The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</summary>
    public InputRouterOptions? InputRouting { get; set; }

    /// <summary>How the page treats the client's culture: following the page's culture coordinator, the default, or switched alone.</summary>
    public CultureArrangement CultureArrangement { get; set; } = CultureArrangement.Coordinated;

    /// <summary>How a switch persists the pair the page accepts, which a standalone app's page publishes, a page its server renders publishing the server's; <see langword="null"/> takes <see cref="CultureSwitchPolicy.Default"/>.</summary>
    public CultureSwitchPolicy? CultureSwitchPolicy { get; set; }
}
