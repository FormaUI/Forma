namespace Forma.Framework.Browser.Interop.Scrolling;

using Microsoft.AspNetCore.Components;

/// <summary>Whether an element is in view of its root.</summary>
/// <remarks>Elements observed against the same root and margin share one native observer.</remarks>
public sealed class DomIntersection : IDomIntersectionObservation
{
    private static readonly DomCapability<IntersectionWatchOptions> Watching = new(DomRoot.Watcher, "intersection", Describe);

    private readonly DomWatcher _watcher;
    private readonly ElementReference _element;

    internal DomIntersection(DomWatcher watcher, ElementReference element)
    {
        _watcher = watcher;
        _element = element;
    }

    /// <inheritdoc />
    public async Task<DomObservation<IntersectionWatchOptions, IntersectionState>> WatchAsync(Func<IntersectionState, Task> handler, IntersectionWatchOptions options, bool shouldObserve = true)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegative(options.RootMarginPixels, nameof(options));

        var observation = new DomObservation<IntersectionWatchOptions, IntersectionState>(_watcher.Session, Watching, handler);
        await observation.SyncAsync(_element, options, shouldObserve);

        return observation;
    }

    private static object Describe(IntersectionWatchOptions options)
        => new { root = options.Root, rootMargin = options.RootMarginPixels };
}
