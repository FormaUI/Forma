namespace Forma.Framework.Core.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A lazily-imported JS module, importing it once even under concurrent callers.</summary>
public sealed class JSModule(IJSEnvironment js, string path) : IAsyncDisposable
{
    private Task<IJSObjectReference>? _import;

    /// <summary>Imports the module if needed, then invokes a void-returning export.</summary>
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

    /// <summary>Imports the module if needed, then invokes an export returning <typeparamref name="TValue"/>.</summary>
    public async ValueTask<TValue> InvokeAsync<TValue>(string identifier, params object?[] args)
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

    /// <summary>Invokes a void-returning export, reporting whether the call succeeded.</summary>
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

    /// <summary>Invokes an export, yielding <see langword="default"/> when the call does not succeed.</summary>
    public async ValueTask<TValue?> TryInvokeAsync<TValue>(string identifier, params object?[] args)
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

        // A module that never arrived leaves nothing to release.
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

    private Task<IJSObjectReference> ImportAsync()
        => _import ??= js.InvokeAsync<IJSObjectReference>("import", path).AsTask();

    /// <summary>The module, or <see langword="null"/> when it could not be imported.</summary>
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

    // JSDisconnectedException derives from Exception rather than JSException, so a dropped
    // circuit and a failed call have to be named separately.
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

    // JSDisconnectedException exposes only a message-only constructor, so the original
    // cannot be chained as an InnerException — the module path is folded into the message instead.
    [DoesNotReturn]
    private void ThrowEnriched(JSDisconnectedException ex)
        => throw new JSDisconnectedException($"{ex.Message} (module: {path})");
}
