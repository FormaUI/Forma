namespace Forma.Blazor.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A popup kept positioned against its anchor for as long as it is open, telling its owner when Escape is pressed anywhere while it is.</summary>
internal sealed class AnchoredPopup(IJSRuntime js)
    : JsInterop(js, "./_content/Forma.Blazor/scripts/behaviours/AnchoredPopup.js")
{
    private bool _isOpen;
    private Task? _transition;

    /// <summary>Brings the popup into line with whether it is open, running <paramref name="onOpened"/> once it is positioned; a popup that <paramref name="keepsFocus"/> moves Tab between its own controls, the <paramref name="trigger"/> is where focus returns on close, and the <paramref name="owner"/> is told through <c>HandleEscapeAsync</c> when Escape is pressed while it is open.</summary>
    public async ValueTask SyncAsync<TOwner>(DotNetObjectReference<TOwner> owner, bool isOpen, ElementReference popup, ElementReference anchor, ElementReference trigger, Func<ValueTask>? onOpened = null, bool keepsFocus = false)
        where TOwner : class
    {
        ArgumentNullException.ThrowIfNull(owner);

        // A sync that arrives while an earlier one is still attaching waits for it, so one open
        // cannot attach twice and strand the first attachment's listeners.
        if (_transition is { } transition)
        {
            await transition;
        }

        if (isOpen == _isOpen)
        {
            return;
        }

        var next = isOpen ? OpenAsync(owner, popup, anchor, trigger, onOpened, keepsFocus) : DetachAsync().AsTask();
        _isOpen = isOpen;
        _transition = next;

        try
        {
            await next;
        }
        finally
        {
            _transition = null;
        }
    }

    /// <summary>Hides the popup while its content is still in the document, returning focus to the trigger when asked and focus is inside it.</summary>
    public ValueTask<bool> CloseAsync(bool returnsFocus)
        => InvokeAsync("close", returnsFocus);

    private async Task OpenAsync<TOwner>(DotNetObjectReference<TOwner> owner, ElementReference popup, ElementReference anchor, ElementReference trigger, Func<ValueTask>? onOpened, bool keepsFocus)
        where TOwner : class
    {
        // Each open gets its own attachment, so a failed attach is retried on the next sync.
        _isOpen = await AttachCoreAsync(popup, new { anchor, keepsFocus, owner });

        if (!_isOpen)
        {
            return;
        }

        await InvokeAsync("open", trigger);

        if (onOpened is not null)
        {
            await onOpened();
        }
    }
}
