namespace Forma.Framework.Input.Interop;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>An input scope registered with the router, held active while its surface is open and disposed when its owner goes.</summary>
public sealed class InputScope(IJSObjectReference handle, IJSEnvironment environment) : IAsyncDisposable
{
    private IJSObjectReference? _handle = handle;

    /// <summary>Whether the scope is held active.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Holds the scope active, reporting whether the call ran.</summary>
    public async ValueTask<bool> ActivateAsync()
    {
        var ran = await InvokeAsync("activate");
        IsActive = IsActive || ran;

        return ran;
    }

    /// <summary>Releases the hold, reporting whether the call ran.</summary>
    public async ValueTask<bool> ReleaseAsync()
    {
        var ran = await InvokeAsync("release");
        IsActive = IsActive && !ran;

        return ran;
    }

    /// <summary>Replaces what the scope contributes, reporting whether the call ran.</summary>
    public ValueTask<bool> UpdateAsync(InputProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return InvokeAsync("update", profile);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        var handle = _handle;
        _handle = null;
        IsActive = false;

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
            LogFailure("dispose", ex);
        }
    }

    private async ValueTask<bool> InvokeAsync(string identifier, params object?[] args)
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
            LogFailure(identifier, ex);
            return false;
        }
    }

    private void LogFailure(string identifier, Exception ex)
    {
        if (ex is JSDisconnectedException)
        {
            InputInteropLog.CircuitDroppedWhileCalling(environment.Logger, identifier, environment.RuntimeId);
        }
        else
        {
            InputInteropLog.ScriptThrewWhileCalling(environment.Logger, ex, identifier, environment.RuntimeId);
        }
    }
}
