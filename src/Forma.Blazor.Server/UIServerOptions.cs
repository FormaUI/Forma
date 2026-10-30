namespace Forma.Blazor;

using Forma.Framework.Input;

/// <summary>The settings a host gives Forma's server registration.</summary>
public sealed class UIServerOptions
{
    /// <summary>The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</summary>
    public InputRouterOptions? InputRouting { get; set; }
}
