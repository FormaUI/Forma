namespace Forma.UI.Blazor.Interop.Behaviours;

using Forma.UI.Features.Composer.Plugins;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Dispatches a command when its key combination is pressed.</summary>
internal sealed class KeyBindings(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/KeyBindings.js")
{
    /// <summary>Attaches the bindings, invoking <c>HandleCommandAsync</c> on <paramref name="owner"/> when one matches.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference target, DotNetObjectReference<TOwner> owner, IReadOnlyList<ComposerKeyBinding> bindings)
        where TOwner : class
        => AttachCoreAsync(target, new { owner, bindings });
}
