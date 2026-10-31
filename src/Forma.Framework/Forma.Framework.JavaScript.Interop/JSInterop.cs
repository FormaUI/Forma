namespace Forma.Framework.JavaScript.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A JS module attached to an element.</summary>
/// <remarks>
/// Every attach and detach starts a new generation, so an attach answering after a later attach, a detach or the
/// disposal has superseded it releases the instance it got rather than holding it.
/// </remarks>
public abstract class JSInterop(IJSEnvironment js, string path) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, path);
    private IJSObjectReference? _handle;
    private int _generation;

    /// <summary>Whether the module is currently attached.</summary>
    public bool IsAttached
        => _handle is not null;

    /// <summary>Attaches the module and reports whether it attached.</summary>
    protected async ValueTask<bool> AttachCoreAsync(params object?[] args)
    {
        var generation = ++_generation;
        await ReleaseAsync(TakeHandle());
        var handle = await _module.TryInvokeAsync<IJSObjectReference>("attach", args);

        if (generation != _generation)
        {
            await ReleaseAsync(handle);

            return false;
        }

        _handle = handle;

        return handle is not null;
    }

    /// <summary>Releases the current attachment, and the one an attach still in flight gets.</summary>
    public ValueTask DetachAsync()
    {
        _generation++;

        return ReleaseAsync(TakeHandle());
    }

    /// <summary>Invokes a function the module exports and reports whether the call ran.</summary>
    protected ValueTask<bool> InvokeModuleAsync(string identifier, params object?[] args)
        => _module.TryInvokeVoidAsync(identifier, args);

    /// <summary>Invokes a void-returning method on the attached instance and reports whether the call ran.</summary>
    protected async ValueTask<bool> InvokeAsync(string identifier, params object?[] args)
    {
        if (_handle is null)
        {
            return false;
        }

        try
        {
            await _handle.InvokeVoidAsync(identifier, args);
            return true;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException)
        {
            LogCallFailure(identifier, ex);
            return false;
        }
    }

    /// <summary>Invokes a method on the attached instance and returns its result, where <see langword="default"/> means the call did not run.</summary>
    protected async ValueTask<TValue?> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, params object?[] args)
    {
        if (_handle is null)
        {
            return default;
        }

        try
        {
            return await _handle.InvokeAsync<TValue>(identifier, args);
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException)
        {
            LogCallFailure(identifier, ex);
            return default;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DetachAsync();
        await _module.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private IJSObjectReference? TakeHandle()
    {
        var handle = _handle;
        _handle = null;

        return handle;
    }

    private async ValueTask ReleaseAsync(IJSObjectReference? handle)
    {
        if (handle is null)
        {
            return;
        }

        try
        {
            await handle.InvokeVoidAsync("dispose");
            await handle.DisposeAsync();
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException)
        {
            LogCallFailure("dispose", ex);
        }
    }

    private void LogCallFailure(string identifier, Exception ex)
    {
        if (ex is JSDisconnectedException)
        {
            InteropLog.CircuitDroppedWhileCalling(js.Logger, identifier, path, js.RuntimeId);
        }
        else
        {
            InteropLog.ScriptThrewWhileCalling(js.Logger, ex, identifier, path, js.RuntimeId);
        }
    }
}
