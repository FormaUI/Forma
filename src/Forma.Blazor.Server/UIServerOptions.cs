namespace Forma.Blazor;

using Forma.Framework.Culture;
using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Input;

/// <summary>The settings a host gives Forma's server registration.</summary>
public sealed class UIServerOptions
{
    /// <summary>The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</summary>
    public InputRouterOptions? InputRouting { get; set; }

    /// <summary>How a switch persists the pair the page accepts; <see langword="null"/> takes <see cref="CultureSwitchPolicy.Default"/>.</summary>
    public CultureSwitchPolicy? CultureSwitchPolicy { get; set; }

    /// <summary>How the pages treat the culture of the WebAssembly components the server renders: following the page's culture coordinator, the default, or each client's own.</summary>
    public CultureArrangement CultureArrangement { get; set; } = CultureArrangement.Coordinated;
}
