namespace Forma.Framework.Interop.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Opens and closes a native dialog as a modal.</summary>
public sealed class Modal(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Framework.Interop/scripts/components/Modal.js")
{
    /// <summary>Attaches the modal to the dialog.</summary>
    public ValueTask<bool> AttachAsync(ElementReference dialog)
        => AttachCoreAsync(dialog);

    /// <summary>Opens the dialog as a modal, closing it on a backdrop click when requested.</summary>
    public ValueTask<bool> OpenAsync(bool closeOnBackdropClick)
        => InvokeAsync("open", closeOnBackdropClick);

    /// <summary>Closes the dialog if it is open.</summary>
    public ValueTask<bool> CloseAsync()
        => InvokeAsync("close");
}
