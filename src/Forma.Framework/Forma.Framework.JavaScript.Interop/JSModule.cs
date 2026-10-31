namespace Forma.Framework.JavaScript.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A lazily imported JS module.</summary>
/// <remarks>The import task is held rather than its result, so concurrent callers share one import.</remarks>
public sealed class JSModule(IJSEnvironment js, string path) : IAsyncDisposable
{
    private Task<IJSObjectReference>? _import;

    /// <summary>Invokes a void-returning export of the module.</summary>
    public async ValueTask InvokeVoidAsync(string identifier, params object?[] args)
    {
        var module = await ImportAsync();

        try
        {
            await module.InvokeVoidAsync(identifier, args);
        }
        catch (JSDisconnectedException ex)
        {
            ThrowEnriched(ex);
        }
    }

    /// <summary>Invokes an export of the module returning <typeparamref name="TValue"/>.</summary>
    public async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, params object?[] args)
    {
        var module = await ImportAsync();

        try
        {
            return await module.InvokeAsync<TValue>(identifier, args);
        }
        catch (JSDisconnectedException ex)
        {
            ThrowEnriched(ex);
            throw; // Unreachable — ThrowEnriched always throws.
        }
    }

    /// <summary>Tries to invoke a void-returning export and reports whether the call succeeded.</summary>
    public async ValueTask<bool> TryInvokeVoidAsync(string identifier, params object?[] args)
    {
        var module = await TryImportAsync();

        if (module is null)
        {
            return false;
        }

        try
        {
            await module.InvokeVoidAsync(identifier, args);
            return true;
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            LogCallFailure(identifier, ex);
            return false;
        }
    }

    /// <summary>Tries to invoke an export and returns its result, where <see langword="default"/> means the call did not succeed.</summary>
    public async ValueTask<TValue?> TryInvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, params object?[] args)
    {
        var module = await TryImportAsync();

        if (module is null)
        {
            return default;
        }

        try
        {
            return await module.InvokeAsync<TValue>(identifier, args);
        }
        catch (Exception ex) when (IsInteropFailure(ex))
        {
            LogCallFailure(identifier, ex);
            return default;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var import = _import;
        _import = null;

        if (import is null || import.IsFaulted)
        {
            return;
        }

        try
        {
            var module = await import;
            await module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            InteropLog.CircuitDroppedWhileReleasing(js.Logger, path, js.RuntimeId);
        }
        catch (JSException ex)
        {
            InteropLog.ScriptThrewWhileReleasing(js.Logger, ex, path, js.RuntimeId);
        }
    }

    /// <summary>Imports the module and returns its reference.</summary>
    /// <remarks>
    /// The first call starts the import and every later call shares it while it runs and once it succeeded. An import
    /// that failed fails every caller sharing it and is then dropped, so the next call imports again.
    /// </remarks>
    public Task<IJSObjectReference> ImportAsync()
    {
        if (_import is { IsFaulted: true } or { IsCanceled: true })
        {
            _import = null;
        }

        return _import ??= js.InvokeAsync<IJSObjectReference>("import", path).AsTask();
    }

    /// <summary>Tries to import the module.</summary>
    private async ValueTask<IJSObjectReference?> TryImportAsync()
    {
        try
        {
            return await ImportAsync();
        }
        catch (JSDisconnectedException)
        {
            InteropLog.CircuitDroppedWhileImporting(js.Logger, path, js.RuntimeId);
            return null;
        }
        catch (JSException ex)
        {
            InteropLog.ImportFailed(js.Logger, ex, path, js.RuntimeId);
            return null;
        }
    }

    // JSDisconnectedException derives from Exception, so it is named beside JSException.
    private static bool IsInteropFailure(Exception ex)
        => ex is JSDisconnectedException or JSException;

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

    // JSDisconnectedException takes only a message, so the module path is folded into it.
    [DoesNotReturn]
    private void ThrowEnriched(JSDisconnectedException ex)
        => throw new JSDisconnectedException($"{ex.Message} (module: {path})");
}
