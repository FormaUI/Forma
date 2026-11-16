namespace Forma.Framework.Browser;

using Microsoft.AspNetCore.Components;

/// <summary>An observation of an element's state, kept in step with the target and options its owner last gave it, which hands each state the browser measures to its handler.</summary>
/// <typeparam name="TOptions">The options the observation takes.</typeparam>
/// <typeparam name="TState">The state the observation measures.</typeparam>
public interface IDomObservation<TOptions, TState> : IAsyncDisposable
    where TOptions : notnull
    where TState : class
{
    /// <summary>The state the browser last measured.</summary>
    TState? Current { get; }

    /// <summary>Whether the observation is established and its first state taken.</summary>
    bool IsActive { get; }

    /// <summary>Whether the observation has been disposed.</summary>
    bool IsDisposed { get; }

    /// <summary>Brings the observation in step with what its owner wants: <paramref name="element"/> observed with <paramref name="options"/> while <paramref name="shouldObserve"/>.</summary>
    /// <param name="element">The element observed.</param>
    /// <param name="options">The options it is observed with.</param>
    /// <param name="shouldObserve">Whether it is observed.</param>
    Task SyncAsync(ElementReference element, TOptions options, bool shouldObserve = true);

    /// <summary>Measures the observation afresh and resumes its delivery.</summary>
    Task RefreshAsync();
}
