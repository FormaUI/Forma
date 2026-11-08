namespace Forma.Framework.JavaScript.Interop;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;

/// <summary>A JS module attached to an element.</summary>
/// <remarks>
/// Every attach and detach starts a new generation, so an attach answering after a later attach, a detach or the
/// disposal has superseded it releases the instance it got rather than holding it. A call on the attached instance
/// ends as its attachment is released, reporting that it didn't run, so a release never waits behind a call a
/// runtime leaves unanswered, as Blazor Hybrid leaves a reloaded page's.
/// </remarks>
public abstract class JSInterop(IJSEnvironment js, string path) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, path);
    private IJSObjectReference? _handle;
    private CancellationTokenSource? _attachment;
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
        _attachment = handle is null ? null : new CancellationTokenSource();

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

        var released = _attachment!.Token;

        try
        {
            await AwaitWhileAttachedAsync(_handle.InvokeAsync<IJSVoidResult>(identifier, args), released);
            return true;
        }
        catch (OperationCanceledException) when (released.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException)
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

        var released = _attachment!.Token;

        try
        {
            return await AwaitWhileAttachedAsync(_handle.InvokeAsync<TValue>(identifier, args), released);
        }
        catch (OperationCanceledException) when (released.IsCancellationRequested)
        {
            return default;
        }
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException)
        {
            LogCallFailure(identifier, ex);
            return default;
        }
    }

    /// <summary>Invokes a method on the attached instance answering through the transfer, and reads the value it answers as JSON, at most the size the environment's transfer accepts.</summary>
    /// <returns>The value, where <see langword="null"/> means the call did not run or its answer was lost.</returns>
    protected async ValueTask<JsonElement?> InvokeJsonAsync(string identifier, params object?[] args)
    {
        var transfer = await InvokeAsync<JSTransfer>(identifier, args);

        try
        {
            return transfer is null ? null : await transfer.ReadJsonAsync(js.TransferOptions.MaximumLength);
        }
        catch (JSTransferException ex)
        {
            InteropLog.AnswerLost(js.Logger, ex, identifier, path, js.RuntimeId);
            return null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DetachAsync();
        await _module.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // The wait ends with the attachment while the call keeps the runtime's own timeout, and a fault arriving after the release is observed.
    private static async ValueTask<TValue> AwaitWhileAttachedAsync<TValue>(ValueTask<TValue> call, CancellationToken released)
    {
        var answer = call.AsTask();

        try
        {
            return await answer.WaitAsync(released);
        }
        catch (OperationCanceledException) when (released.IsCancellationRequested && !answer.IsCompleted)
        {
            _ = answer.ContinueWith(static late => late.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            throw;
        }
    }

    // The calls still in flight on the attachment end with it.
    private IJSObjectReference? TakeHandle()
    {
        var handle = _handle;
        _handle = null;

        if (_attachment is { } attachment)
        {
            _attachment = null;
            attachment.Cancel();
            attachment.Dispose();
        }

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
        catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException)
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
        else if (ex is OperationCanceledException)
        {
            InteropLog.CallCanceled(js.Logger, identifier, path, js.RuntimeId);
        }
        else
        {
            InteropLog.ScriptThrewWhileCalling(js.Logger, ex, identifier, path, js.RuntimeId);
        }
    }
}
