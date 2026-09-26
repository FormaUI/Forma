namespace Forma.Blazor.Components.Interaction;

using Forma.Framework.Input;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Registers a component's element with the router for as long as the component lives.</summary>
internal static class InputRouterExtensions
{
    /// <summary>Registers <paramref name="element"/> as a scope owned by <paramref name="owner"/>, yielding <see langword="null"/> when the circuit cannot register it or the owner is disposed before the registration lands, in which case the registration is released at once.</summary>
    public static async Task<InputScope?> RegisterUnlessDisposedAsync<TOwner>(this InputRouter router, ElementReference element, InputProfile profile, DotNetObjectReference<TOwner> owner, Func<bool> isDisposed)
        where TOwner : class
    {
        if (isDisposed())
        {
            return null;
        }

        var inputScope = await router.RegisterAsync(element, profile, owner);

        if (inputScope is not null && isDisposed())
        {
            await inputScope.DisposeAsync();
            return null;
        }

        return inputScope;
    }
}
