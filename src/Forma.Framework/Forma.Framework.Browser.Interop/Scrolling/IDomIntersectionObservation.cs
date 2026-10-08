namespace Forma.Framework.Browser.Interop.Scrolling;

/// <summary>Whether an element is in view of its root, as an owner observes it.</summary>
public interface IDomIntersectionObservation
{
    /// <summary>Observes the element's <see cref="IntersectionState"/> with <paramref name="options"/> while <paramref name="shouldObserve"/>, handing each state the browser measures to <paramref name="handler"/>.</summary>
    Task<DomObservation<IntersectionWatchOptions, IntersectionState>> WatchAsync(Func<IntersectionState, Task> handler, IntersectionWatchOptions options, bool shouldObserve = true);
}
