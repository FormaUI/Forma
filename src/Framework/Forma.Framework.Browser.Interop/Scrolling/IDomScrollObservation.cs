namespace Forma.Framework.Browser.Interop.Scrolling;

/// <summary>An element's scrolling, as an owner observes it.</summary>
public interface IDomScrollObservation
{
    /// <summary>Observes the element's <see cref="ScrollState"/> with <paramref name="options"/> while <paramref name="shouldObserve"/>, handing each state the browser measures to <paramref name="handler"/>.</summary>
    Task<DomObservation<ScrollWatchOptions, ScrollState>> WatchAsync(Func<ScrollState, Task> handler, ScrollWatchOptions options, bool shouldObserve = true);
}
