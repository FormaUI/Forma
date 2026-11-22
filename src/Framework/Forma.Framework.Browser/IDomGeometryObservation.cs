namespace Forma.Framework.Browser;

/// <summary>An element's geometry, as an owner observes it.</summary>
public interface IDomGeometryObservation
{
    /// <summary>Observes the element's <see cref="SizeState"/> while <paramref name="shouldObserve"/>, handing each size the browser measures to <paramref name="handler"/>.</summary>
    Task<IDomObservation<SizeWatchOptions, SizeState>> WatchSizeAsync(Func<SizeState, Task> handler, bool shouldObserve = true);
}
