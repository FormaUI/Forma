namespace Forma.UI.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A popup kept positioned against its anchor for as long as it is open.</summary>
internal sealed class AnchoredPopup(IJSRuntime js)
    : JsBehaviour(js, "./_content/Forma.UI/scripts/behaviours/AnchoredPopup.js")
{
    private bool _isOpen;

    /// <summary>Brings the popup into line with whether it is open, running <paramref name="onOpened"/> once it is positioned.</summary>
    public async ValueTask SyncAsync(bool isOpen, ElementReference popup, ElementReference anchor, Func<ValueTask>? onOpened = null)
    {
        if (isOpen == _isOpen)
        {
            return;
        }

        if (!isOpen)
        {
            _isOpen = false;
            await DetachAsync();
            return;
        }

        // The popup element is rebuilt each time it opens, so each open gets its own attachment bound to that element.
        // Taken from the attachment rather than the request, so a failed attach is retried on the next sync.
        _isOpen = await AttachCoreAsync(popup, new { anchor });

        if (!_isOpen)
        {
            return;
        }

        await InvokeAsync("open");

        if (onOpened is not null)
        {
            await onOpened();
        }
    }
}
