namespace Forma.Framework.Browser.Interop.Components;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The browser side of a window: shown in the top layer and put in front of the other windows when raised, a modal one keeping Tab inside and standing on a scrim; it remembers where focus was inside it and takes focus back there.</summary>
public sealed class Window(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/components/Window.js")
{
    /// <summary>Attaches to the window, shown in the top layer while <paramref name="presented"/>; a <paramref name="modal"/> one keeps Tab inside itself and shows <paramref name="scrim"/> beneath it.</summary>
    public ValueTask<bool> AttachAsync(ElementReference window, bool presented, bool modal, ElementReference? scrim)
        => AttachCoreAsync(window, new { presented, modal, scrim });

    /// <summary>Moves focus into the window.</summary>
    /// <remarks>Focus already on a control inside the window stays there; otherwise it goes to the element last focused inside the window, else its first control, else the window itself.</remarks>
    public ValueTask<bool> FocusAsync()
        => InvokeAsync("focus");

    /// <summary>Puts a presented window on top of every other window.</summary>
    public ValueTask<bool> RaiseAsync()
        => InvokeAsync("raise");

    /// <summary>Shows the window in the top layer or takes it out, to match <paramref name="presented"/>.</summary>
    public ValueTask<bool> PresentAsync(bool presented)
        => InvokeAsync("present", presented);
}
