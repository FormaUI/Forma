namespace Forma.Framework.Browser.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>What an owner observes of its elements.</summary>
/// <remarks>Disposing the watcher releases every observation and interaction made through it or an engine borrowing it, in one call to the browser.</remarks>
public sealed class DomWatcher : IAsyncDisposable
{
    /// <summary>Creates the watcher of the owner whose environment is <paramref name="js"/>, importing through <paramref name="bridge"/>.</summary>
    public DomWatcher(IJSEnvironment js, DomBridge? bridge = null)
    {
        ArgumentNullException.ThrowIfNull(js);

        Session = new DomSession(js, bridge);
    }

    /// <summary>Whether the watcher has been disposed.</summary>
    public bool IsDisposed
        => Session.IsReleased;

    internal DomSession Session { get; }

    /// <summary>Gets <paramref name="element"/> as the watcher observes it.</summary>
    public IDomElementObservation GetElement(ElementReference element)
        => new DomElement(this, engine: null, element);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => Session.ReleaseAsync();
}
