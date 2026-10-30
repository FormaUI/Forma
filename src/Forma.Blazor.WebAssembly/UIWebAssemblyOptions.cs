namespace Forma.Blazor;

using Forma.Framework.Input;

/// <summary>The settings a client gives Forma's WebAssembly registration.</summary>
public sealed class UIWebAssemblyOptions
{
    /// <summary>The application-wide input policy; <see langword="null"/> takes <see cref="InputRouterOptions.Default"/>.</summary>
    public InputRouterOptions? InputRouting { get; set; }
}
