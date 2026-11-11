namespace Forma.Blazor;

using Forma.Framework.Culture;
using Forma.Framework.Input;

/// <summary>The settings an application gives Forma's WebView integration.</summary>
public sealed class WebViewIntegrationOptions
{
    /// <summary>The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</summary>
    public InputRouterOptions? InputRouting { get; set; }

    /// <summary>Whether every web view follows the application's culture, or each holds a culture of its own.</summary>
    public CultureArrangement CultureArrangement { get; set; }
}
