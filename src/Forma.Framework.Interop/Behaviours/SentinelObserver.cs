namespace Forma.Framework.Interop.Behaviours;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// Attaches a sentinel element to an <see cref="InfiniteScroll"/> observer only while it is wanted, so a caller
/// states what it currently wants watched rather than tracking whether that is already the case.
/// </summary>
public sealed class SentinelObserver<TOwner>(IJSRuntime js) : IAsyncDisposable
    where TOwner : class
{
    private readonly InfiniteScroll _infiniteScroll = new(js);
    private DotNetObjectReference<TOwner>? _ownerReference;
    private bool _isStale;

    /// <summary>Attaches or detaches the sentinel to match whether it is currently wanted, and re-attaches it when stale.</summary>
    public async Task SyncAsync(TOwner owner, bool shouldObserve, ElementReference sentinel, ElementReference? root = null)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (shouldObserve == _infiniteScroll.IsAttached && !_isStale)
        {
            return;
        }

        if (shouldObserve)
        {
            _ownerReference ??= DotNetObjectReference.Create(owner);
            await _infiniteScroll.AttachAsync(sentinel, _ownerReference, root);
        }
        else
        {
            await _infiniteScroll.DetachAsync();
        }

        _isStale = false;
    }

    /// <summary>Marks the current watch stale, so the next sync re-attaches even though nothing else about it changed.</summary>
    public void Invalidate()
        => _isStale = true;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _infiniteScroll.DisposeAsync();
        _ownerReference?.Dispose();
    }
}
