namespace Forma.Framework.JavaScript.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A JS module attached to an element.</summary>
public abstract class JSInterop(IJSEnvironment js, string path) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, path);
    private IJSObjectReference? _handle;

    /// <summary>Whether the module is currently attached.</summary>
    public bool IsAttached
        => _handle is not null;

    /// <summary>Attaches the module and reports whether it attached.</summary>
    protected async ValueTask<bool> AttachCoreAsync(params object?[] args)
    {
        await DetachAsync();
        _handle = await _module.TryInvokeAsync<IJSObjectReference>("attach", args);

        return _handle is not null;
    }

    /// <summary>Releases the current attachment.</summary>
    public async ValueTask DetachAsync()
    {
        var handle = _handle;
        _handle = null;

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
