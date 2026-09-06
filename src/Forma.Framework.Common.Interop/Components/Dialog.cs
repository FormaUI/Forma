namespace Forma.Framework.Common.Interop.Components;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Opens and closes a native dialog as a modal.</summary>
public sealed class Dialog(IJSRuntime js)
    : JsInterop(js, "./_content/forma/common/scripts/components/Dialog.js")
{
    /// <summary>Attaches to the dialog.</summary>
    public ValueTask<bool> AttachAsync(ElementReference dialog)
        => AttachCoreAsync(dialog);

    /// <summary>Opens the dialog as a modal, closing it on a backdrop click when requested.</summary>
    public ValueTask<bool> OpenAsync(bool closeOnBackdropClick)
        => InvokeAsync("open", closeOnBackdropClick);

    /// <summary>Closes the dialog if it is open.</summary>
    public ValueTask<bool> CloseAsync()
        => InvokeAsync("close");
}
