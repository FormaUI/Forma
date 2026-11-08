namespace Forma.Blazor.Browser.Components.Checks;

using Microsoft.AspNetCore.Components;

/// <summary>A check page, whose address names the runtime that renders it ahead of the page's own path.</summary>
public abstract class CheckPage : ComponentBase
{
    /// <summary>The runtime the page's address names: <c>server</c>, <c>webassembly</c>, <c>auto</c>, <c>webview</c>, <c>android</c> or <c>static</c>.</summary>
    [Parameter] public string Runtime { get; set; } = default!;
}
