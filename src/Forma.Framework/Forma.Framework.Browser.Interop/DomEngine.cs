namespace Forma.Framework.Browser.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>What an owner does to its elements.</summary>
/// <remarks>Disposing the engine releases the interactions made through it, in one call to the browser.</remarks>
public sealed class DomEngine : IAsyncDisposable
{
    private readonly DomWatcher _watcher;
    private bool _isDisposed;

    /// <summary>Creates the engine that acts through <paramref name="watcher"/>.</summary>
    public DomEngine(DomWatcher watcher)
    {
        ArgumentNullException.ThrowIfNull(watcher);

        _watcher = watcher;
    }

    /// <summary>Whether the engine has been disposed, or the watcher it borrows.</summary>
    public bool IsDisposed
        => _isDisposed || _watcher.IsDisposed;

    /// <summary>Gets <paramref name="element"/> as the engine acts on it and observes it.</summary>
    public DomElement GetElement(ElementReference element)
        => new(_watcher, this, element);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        _isDisposed = true;
        await _watcher.Session.ReleaseEngineAsync();
    }
}
