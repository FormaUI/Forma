namespace Forma.Framework.Browser.Interop.Focus;

/// <summary>An element's focus, as an owner observes it.</summary>
public interface IDomFocusObservation
{
    /// <summary>Observes the element's <see cref="FocusState"/> while <paramref name="shouldObserve"/>, handing each state the browser measures to <paramref name="handler"/>.</summary>
    Task<DomObservation<FocusWatchOptions, FocusState>> WatchAsync(Func<FocusState, Task> handler, bool shouldObserve = true);

    /// <summary>Observes the element's <see cref="FocusState"/> as <paramref name="options"/> sets out while <paramref name="shouldObserve"/>, handing each state the browser measures to <paramref name="handler"/>.</summary>
    /// <exception cref="ArgumentException">The options name an empty item attribute.</exception>
    Task<DomObservation<FocusWatchOptions, FocusState>> WatchAsync(Func<FocusState, Task> handler, FocusWatchOptions options, bool shouldObserve = true);
}
