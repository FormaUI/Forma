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

    /// <summary>Opens the dialog as a modal.</summary>
    /// <param name="closeOnScrimClick">Whether a click landing beside the dialog's content closes it.</param>
    /// <param name="focusWithin">The selector of the part whose first control takes focus, or null for the browser's own placement.</param>
    public ValueTask<bool> OpenAsync(bool closeOnScrimClick, string? focusWithin = null)
        => InvokeAsync("open", new { closeOnScrimClick, focusWithin });

    /// <summary>Closes the dialog.</summary>
    public ValueTask<bool> CloseAsync()
        => InvokeAsync("close");
}
