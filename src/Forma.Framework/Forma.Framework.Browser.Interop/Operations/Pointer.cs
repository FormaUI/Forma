namespace Forma.Framework.Browser.Interop.Operations;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Routes a pointer to one element for the rest of its gesture.</summary>
[SuppressMessage("Naming", "CA1720", Justification = "Named after the pointer capture module it owns; the pointer is the subject, not a pointer type.")]
public sealed class Pointer(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/Pointer.js");

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
