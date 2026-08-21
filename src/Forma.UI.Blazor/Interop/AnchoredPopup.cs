namespace Forma.UI.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A popup kept positioned against its anchor for as long as it is open.</summary>
internal sealed class AnchoredPopup(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.UI/scripts/AnchoredPopup.js";

    private bool _isAttached;
    private readonly JsModule _module = new(js, ModulePath);
    private IJSObjectReference? _placement;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            await ReleaseAsync();
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected — safe to ignore.
        }
        catch (JSException)
        {
            // Module never loaded — nothing to release.
        }

        await _module.DisposeAsync();
    }

    /// <summary>Brings the placement into line with whether the popup is open, running <paramref name="onOpened"/> once it is positioned.</summary>
    public async ValueTask SyncAsync(bool isOpen, ElementReference popup, ElementReference anchor, Func<ValueTask>? onOpened = null)
    {
        if (isOpen == _isAttached)
        {
            return;
        }

        try
        {
            // Cleared before releasing rather than after: the release disposes the placement
            // whether or not closing it succeeds, and a stale flag would skip the next sync.
            _isAttached = false;
            await ReleaseAsync();

            if (isOpen)
            {
                // The popup element is rebuilt each time it opens, so each open gets its own
                // placement bound to that element rather than reusing a stale one.
                _placement = await _module.InvokeAsync<IJSObjectReference>("AnchoredPopup.create", popup, anchor);
                await _placement.InvokeVoidAsync("open");
                _isAttached = true;

                if (onOpened is not null)
                {
                    await onOpened();
                }
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected — safe to ignore.
        }
        catch (JSException)
        {
            // Interop failed — safe to ignore.
        }
    }

    private async ValueTask ReleaseAsync()
    {
        if (_placement is null)
        {
            return;
        }

        var placement = _placement;
        _placement = null;

        try
        {
            await placement.InvokeVoidAsync("close");
        }
        finally
        {
            await placement.DisposeAsync();
        }
    }
}
