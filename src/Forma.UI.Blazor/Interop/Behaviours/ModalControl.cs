namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Opens and closes a native dialog as a modal.</summary>
internal sealed class ModalControl(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/ModalControl.js")
{
    /// <summary>Attaches control to the dialog.</summary>
    public ValueTask<bool> AttachAsync(ElementReference dialog)
        => AttachCoreAsync(dialog);

    /// <summary>Opens the dialog as a modal, closing it on a backdrop click when requested.</summary>
    public ValueTask<bool> OpenAsync(bool closeOnBackdropClick)
        => InvokeAsync("open", closeOnBackdropClick);

    /// <summary>Closes the dialog if it is open.</summary>
    public ValueTask<bool> CloseAsync()
        => InvokeAsync("close");
}
