namespace Forma.Framework.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Dispatches a command when its key combination is pressed.</summary>
public sealed class KeyBindings(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Interop/scripts/behaviours/KeyBindings.js")
{
    /// <summary>Attaches the bindings, invoking <c>HandleCommandAsync</c> on <paramref name="owner"/> when one matches.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, DotNetObjectReference<TOwner> owner, IReadOnlyList<KeyBinding> bindings)
        where TOwner : class
        => AttachCoreAsync(target, new { owner, bindings });
}
