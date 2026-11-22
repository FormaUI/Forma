namespace Forma.Framework.Browser.Interop.Geometry;

using System.Diagnostics.CodeAnalysis;

using Microsoft.AspNetCore.Components;

/// <summary>An element's geometry: its size along its writing mode's axes, as the layout changes it.</summary>
/// <remarks>Every element observed shares the document's one resize observer.</remarks>
public sealed class DomGeometry : IDomGeometryObservation
{
    private static readonly DomCapability<SizeWatchOptions> SizeCapability = new(DomRoots.Watcher, "size", static _ => new SizeDescription());

    private readonly DomWatcher _watcher;
    private readonly ElementReference _element;

    internal DomGeometry(DomWatcher watcher, ElementReference element)
    {
        _watcher = watcher;
        _element = element;
    }

    /// <inheritdoc />
    public async Task<IDomObservation<SizeWatchOptions, SizeState>> WatchSizeAsync(Func<SizeState, Task> handler, bool shouldObserve = true)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var observation = new DomObservation<SizeWatchOptions, SizeState>(_watcher.Session, SizeCapability, DomJsonContext.Default.SizeState, handler);
        await observation.SyncAsync(_element, new SizeWatchOptions(), shouldObserve);

        return observation;
    }

    /// <summary>The size watch as the browser reads it, which takes no options.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private sealed record SizeDescription;
}
