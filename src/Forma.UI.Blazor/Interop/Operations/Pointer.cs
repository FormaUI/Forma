namespace Forma.UI.Blazor.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Routes a pointer to one element for the rest of its gesture.</summary>
internal sealed class Pointer(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.UI/scripts/operations/Pointer.js");

    /// <summary>Routes the pointer with the given id to <paramref name="target"/> until released.</summary>
    public ValueTask<bool> CaptureAsync(ElementReference target, long pointerId)
        => _module.TryInvokeVoidAsync("capture", target, pointerId);

    /// <summary>Releases capture of the pointer with the given id from <paramref name="target"/>, if held.</summary>
    public ValueTask<bool> ReleaseAsync(ElementReference target, long pointerId)
        => _module.TryInvokeVoidAsync("release", target, pointerId);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
