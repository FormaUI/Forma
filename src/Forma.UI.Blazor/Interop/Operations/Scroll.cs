namespace Forma.UI.Blazor.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Moves a scroll container to a position it cannot express declaratively.</summary>
internal sealed class Scroll(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.UI/scripts/operations/Scroll.js");

    /// <summary>Centres the container's scroll position over its content.</summary>
    public ValueTask<bool> ToCenterAsync(ElementReference container)
        => _module.TryInvokeVoidAsync("toCenter", container);

    /// <summary>Scrolls the container by one page in <paramref name="direction"/>, -1 toward the start and 1 toward the end.</summary>
    public ValueTask<bool> ByPageAsync(ElementReference container, int direction)
        => _module.TryInvokeVoidAsync("byPage", container, direction);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
