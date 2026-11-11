namespace Forma.Framework.Browser.Interop.Components;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The browser side of a window: shown in the top layer and put in front of the other windows when raised, a modal one keeping Tab inside and standing on a scrim; it remembers where focus was inside it and takes focus back there, and tells its owner of each press inside it.</summary>
public sealed class Window(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/components/Window.js")
{
    /// <summary>Attaches to the window, shown in the top layer while <paramref name="isPresented"/>; a <paramref name="isModal"/> one keeps Tab inside itself and shows <paramref name="scrim"/> beneath it; <paramref name="owner"/> hears of each press inside it.</summary>
    public ValueTask<bool> AttachAsync<TOwner>(ElementReference window, bool isPresented, bool isModal, ElementReference? scrim, DotNetObjectReference<TOwner> owner)
        where TOwner : class
        => AttachCoreAsync(window, new AttachOptions<TOwner> { IsPresented = isPresented, IsModal = isModal, Scrim = scrim, Owner = owner });

    /// <summary>Moves focus into the window.</summary>
    /// <remarks>Focus already on a control inside the window stays there; otherwise it goes to the element last focused inside the window, else its first control, else the window itself.</remarks>
    public ValueTask<bool> FocusAsync()
        => InvokeAsync("focus");

    /// <summary>Puts a presented window on top of every other window.</summary>
    public ValueTask<bool> RaiseAsync()
        => InvokeAsync("raise");

    /// <summary>Shows the window in the top layer or takes it out, to match <paramref name="isPresented"/>.</summary>
    public ValueTask<bool> PresentAsync(bool isPresented)
        => InvokeAsync("present", isPresented);

    /// <summary>The options the script attaches with.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record AttachOptions<TOwner>
        where TOwner : class
    {
        public required bool IsPresented { get; init; }

        public required bool IsModal { get; init; }

        public required ElementReference? Scrim { get; init; }

        public required DotNetObjectReference<TOwner> Owner { get; init; }
    }
}
