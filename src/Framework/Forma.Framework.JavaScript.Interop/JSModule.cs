namespace Forma.Framework.JavaScript.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Microsoft.JSInterop;

/// <summary>A lazily imported JS module.</summary>
/// <remarks>
/// The import task is held rather than its result, so concurrent callers share one import. What a script answers a
/// <c>TryInvoke</c> call is what the browser sent, so it's read as JSON through the caller's contract, and an answer that
/// isn't the value asked for is refused as the transfer refuses one.
/// </remarks>
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

    /// <summary>Tries to invoke an export and reads what it answers as a <typeparamref name="TValue"/>, where <see langword="default"/> means the call did not succeed or its answer was refused.</summary>
    /// <param name="identifier">The export.</param>
    /// <param name="typeInfo">The JSON contract of <typeparamref name="TValue"/>.</param>
    /// <param name="args">The export's arguments.</param>
    /// <returns>The value, which is <see langword="default"/> where the script answered null.</returns>
    public async ValueTask<TValue?> TryInvokeAsync<TValue>(string identifier, JsonTypeInfo<TValue> typeInfo, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        return ReadAnswer(js, path, identifier, await TryInvokeCoreAsync<JsonElement>(identifier, args), typeInfo);
    }

    /// <summary>Tries to invoke an export answering through the transfer, and reads the text it answers, at most <paramref name="maximumLength"/> bytes of it.</summary>
    /// <param name="identifier">The export.</param>
    /// <param name="maximumLength">The size the caller accepts, in bytes.</param>
    /// <param name="args">The export's arguments.</param>
    /// <returns>The text, where <see langword="null"/> means the call didn't succeed or its answer was lost or refused.</returns>
    public async ValueTask<string?> TryInvokeTextAsync(string identifier, long maximumLength, params object?[] args)
    {
        var transfer = await TryInvokeCoreAsync<JSTransfer>(identifier, args);

        try
        {
            return transfer is null ? null : await transfer.ReadTextAsync(maximumLength);
        }
        catch (JSTransferException ex)
        {
            LogTransferFailure(js, path, identifier, ex);
            return null;
        }
    }

    /// <summary>Tries to invoke an export answering through the transfer, and reads the value it answers as a <typeparamref name="TValue"/>, at most <paramref name="maximumLength"/> bytes of its JSON.</summary>
    /// <param name="identifier">The export.</param>
    /// <param name="typeInfo">The JSON contract of <typeparamref name="TValue"/>.</param>
    /// <param name="maximumLength">The size the caller accepts, in bytes.</param>
    /// <param name="args">The export's arguments.</param>
    /// <returns>The value, where <see langword="default"/> means the call didn't succeed, its answer was lost or refused, or the script answered null.</returns>
    public async ValueTask<TValue?> TryInvokeJsonAsync<TValue>(string identifier, JsonTypeInfo<TValue> typeInfo, long maximumLength, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var transfer = await TryInvokeCoreAsync<JSTransfer>(identifier, args);

        try
        {
            return transfer is null ? default : await transfer.ReadJsonAsync(typeInfo, maximumLength);
        }
        catch (JSTransferException ex)
        {
            LogTransferFailure(js, path, identifier, ex);
            return default;
        }
    }

    /// <summary>Tries to invoke an export answering a JS object, and returns the reference to it, where <see langword="null"/> means the call did not succeed.</summary>
    public ValueTask<IJSObjectReference?> TryInvokeReferenceAsync(string identifier, params object?[] args)
        => TryInvokeCoreAsync<IJSObjectReference>(identifier, args);

    /// <summary>Reads <paramref name="answer"/>, what <paramref name="identifier"/> in <paramref name="module"/> answered, as a <typeparamref name="TValue"/>, refusing what isn't one under <paramref name="js"/>'s owner.</summary>
    /// <returns>The value, where <see langword="default"/> means the call answered nothing, null, or a value that was refused.</returns>
    internal static TValue? ReadAnswer<TValue>(IJSEnvironment js, string module, string identifier, JsonElement answer, JsonTypeInfo<TValue> typeInfo)
    {
        // A call that didn't succeed answers no JSON at all, which a browser's answer always is.
        if (answer.ValueKind is JsonValueKind.Undefined)
        {
            return default;
        }

        try
        {
            return JSTransfer.Deserialize(answer, typeInfo);
        }
        catch (JSTransferException ex)
        {
            InteropLog.AnswerRefused(js.Logger, ex, identifier, module, js.RuntimeId);
            return default;
        }
    }

    /// <summary>Logs a value <paramref name="identifier"/> in <paramref name="module"/> answered through the transfer that its caller applies nothing of, as refused or as lost on its way.</summary>
    internal static void LogTransferFailure(IJSEnvironment js, string module, string identifier, JSTransferException exception)
    {
        if (exception.IsRefused)
        {
            InteropLog.AnswerRefused(js.Logger, exception, identifier, module, js.RuntimeId);
        }
        else
        {
            InteropLog.AnswerLost(js.Logger, exception, identifier, module, js.RuntimeId);
        }
    }

    /// <inheritdoc />
    /// <remarks>An import still under way is released once it completes, which disposing doesn't wait for, so a runtime that never answers it, as a web view being torn down answers none, can't hold disposal open.</remarks>
    public ValueTask DisposeAsync()
    {
        var import = _import;
        _import = null;

        if (import is null || import.IsFaulted || import.IsCanceled)
        {
            return ValueTask.CompletedTask;
        }

        var release = ReleaseAsync(import);

        return import.IsCompleted ? new ValueTask(release) : ValueTask.CompletedTask;
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
        catch (OperationCanceledException)
        {
            InteropLog.ImportCanceled(js.Logger, path, js.RuntimeId);
            return null;
        }
        catch (JSException ex)
        {
            InteropLog.ImportFailed(js.Logger, ex, path, js.RuntimeId);
            return null;
        }
    }

    // What the runtime reads an answer as itself: the JSON of a value, a transfer, or a JS object's reference.
    private async ValueTask<TValue?> TryInvokeCoreAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, object?[] args)
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

    // JSDisconnectedException derives from Exception, so it is named beside JSException; a call the runtime canceled, as its
    // circuit ended or the call ran out of time, didn't run either, nor did one whose module, or an object it passes, was released.
    private static bool IsInteropFailure(Exception ex)
        => ex is JSDisconnectedException or JSException or OperationCanceledException or ObjectDisposedException;

    // The release touches nothing of the owner's, so it needn't return to the owner's context.
    private async Task ReleaseAsync(Task<IJSObjectReference> import)
    {
        try
        {
            var module = await import.ConfigureAwait(false);
            await module.DisposeAsync().ConfigureAwait(false);
        }
        catch (JSDisconnectedException)
        {
            InteropLog.CircuitDroppedWhileReleasing(js.Logger, path, js.RuntimeId);
        }
        catch (OperationCanceledException)
        {
            InteropLog.ReleaseCanceled(js.Logger, path, js.RuntimeId);
        }
        catch (JSException ex)
        {
            InteropLog.ScriptThrewWhileReleasing(js.Logger, ex, path, js.RuntimeId);
        }
    }

    private void LogCallFailure(string identifier, Exception ex)
    {
        if (ex is JSDisconnectedException)
        {
            InteropLog.CircuitDroppedWhileCalling(js.Logger, identifier, path, js.RuntimeId);
        }
        else if (ex is OperationCanceledException)
        {
            InteropLog.CallCanceled(js.Logger, identifier, path, js.RuntimeId);
        }
        else if (ex is ObjectDisposedException)
        {
            InteropLog.ReleasedBeforeCalling(js.Logger, ex, identifier, path, js.RuntimeId);
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
