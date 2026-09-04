namespace Forma.Framework.Input.Interop;

using Forma.Framework.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Reports every key, press, and wheel turn on the page to its owner as the input router resolves it.</summary>
public sealed class InputInspector(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Input.Interop/scripts/InputInspector.js")
{
    /// <summary>Starts reporting to <paramref name="owner"/> through <c>ReportAsync</c>.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference element, DotNetObjectReference<TOwner> owner)
        where TOwner : class
        => AttachCoreAsync(element, new { owner });
}
