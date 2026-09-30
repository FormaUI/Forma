namespace Forma.Framework.Browser.Interop.Behaviors;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A popover kept positioned against its anchor for as long as it is open.</summary>
public sealed class AnchoredPopover(IJSEnvironment js)
    : JSInterop(js, "./_content/forma/browser/scripts/behaviors/AnchoredPopover.js")
{
    private bool _isOpen;
    private Task? _transition;

    /// <summary>Opens or closes the popover to match <paramref name="isOpen"/>.</summary>
    /// <param name="isOpen">Whether the popover is open.</param>
    /// <param name="popover">The popover element.</param>
    /// <param name="anchor">The element the popover is positioned against.</param>
    /// <param name="trigger">The element focus returns to on close.</param>
    /// <param name="onOpened">The callback run once the popover is positioned.</param>
    /// <param name="keepsFocus">Whether Tab moves between the popover's own controls.</param>
    /// <param name="alignment">The inline edge of the anchor the popover lines up with.</param>
    public async ValueTask SyncAsync(bool isOpen, ElementReference popover, ElementReference anchor, ElementReference trigger, Func<ValueTask>? onOpened = null, bool keepsFocus = false, PopoverAlignment alignment = PopoverAlignment.Auto)
    {
        // Waiting for a sync still attaching keeps one open from attaching twice.
        if (_transition is { } transition)
        {
            await transition;
        }

        if (isOpen == _isOpen)
        {
            return;
        }

        var next = isOpen ? OpenAsync(popover, anchor, trigger, onOpened, keepsFocus, alignment) : DetachAsync().AsTask();
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

    /// <summary>Hides the popover.</summary>
    /// <param name="returnsFocus">Whether focus inside the popover returns to the trigger.</param>
    public ValueTask<bool> CloseAsync(bool returnsFocus)
        => InvokeAsync("close", returnsFocus);

    private async Task OpenAsync(ElementReference popover, ElementReference anchor, ElementReference trigger, Func<ValueTask>? onOpened, bool keepsFocus, PopoverAlignment alignment)
    {
        // A failed attach leaves the popover closed, so the next sync attaches again.
        _isOpen = await AttachCoreAsync(popover, new { anchor, keepsFocus, align = alignment switch { PopoverAlignment.Start => "start", PopoverAlignment.End => "end", _ => "auto" } });

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
