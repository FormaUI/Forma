namespace Forma.Blazor;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Input;

/// <summary>The settings a host gives Forma's server registration.</summary>
public sealed class UIServerOptions
{
    /// <summary>The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</summary>
    public InputRouterOptions? InputRouting { get; set; }

    /// <summary>How a switch persists the pair the page accepts; <see langword="null"/> takes <see cref="CultureSwitchPolicy.Default"/>.</summary>
    public CultureSwitchPolicy? CultureSwitchPolicy { get; set; }
}
