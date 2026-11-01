namespace Forma.Blazor;

using Forma.Framework.Input;

/// <summary>The settings an application gives Forma's WebView registration.</summary>
public sealed class UIWebViewOptions
{
    /// <summary>The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</summary>
    public InputRouterOptions? InputRouting { get; set; }
}
