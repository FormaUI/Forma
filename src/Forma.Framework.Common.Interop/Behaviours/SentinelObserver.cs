namespace Forma.Framework.Common.Interop.Behaviours;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>
/// Watches a sentinel element only while it is wanted, so a caller states what it currently wants watched rather
/// than tracking whether that is already the case.
/// </summary>
public sealed class SentinelObserver<TOwner>(IJSEnvironment js) : IAsyncDisposable
    where TOwner : class
{
    private readonly Scroll _scroll = new(js);
    private DotNetObjectReference<TOwner>? _ownerReference;
    private bool _isStale;

    /// <summary>Attaches or detaches the sentinel to match whether it is currently wanted, and re-attaches it when stale.</summary>
    public async Task SyncAsync(TOwner owner, bool shouldObserve, ElementReference sentinel, ElementReference? root = null)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (shouldObserve == _scroll.IsAttached && !_isStale)
        {
            return;
        }

        if (shouldObserve)
        {
            _ownerReference ??= DotNetObjectReference.Create(owner);
            await _scroll.AttachAsync(sentinel, new ScrollBehaviours { Sentinel = new ScrollSentinel(root) }, _ownerReference);
        }
        else
        {
            await _scroll.DetachAsync();
        }

        _isStale = false;
    }

    /// <summary>Marks the current watch stale, so the next sync re-attaches even though nothing else about it changed.</summary>
    public void Invalidate()
        => _isStale = true;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _scroll.DisposeAsync();
        _ownerReference?.Dispose();
    }
}
