namespace Forma.Framework.Browser.Interop.Components;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Opens and closes a native dialog as a modal.</summary>
public sealed class Dialog(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/components/Dialog.js")
{
    /// <summary>Attaches to the dialog.</summary>
    public ValueTask<bool> AttachAsync(ElementReference dialog)
        => AttachCoreAsync(dialog);

    /// <summary>Opens the dialog as a modal, closing it on a scrim click when requested.</summary>
    public ValueTask<bool> OpenAsync(bool closeOnScrimClick)
        => InvokeAsync("open", closeOnScrimClick);

    /// <summary>Closes the dialog if it is open.</summary>
    public ValueTask<bool> CloseAsync()
        => InvokeAsync("close");
}
