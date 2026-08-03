using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

namespace Forma.Blazor.Interop;

/// <summary>A lazily-imported JS module, importing it once even under concurrent callers.</summary>
internal sealed class JsModule(IJSRuntime js, string path) : IAsyncDisposable
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

    public async ValueTask DisposeAsync()
    {
        var import = _import;
        _import = null;

        if (import is null)
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
            // Circuit disconnected — safe to ignore.
        }
        catch (JSException)
        {
            // Import never completed — nothing to dispose.
        }
    }

    private Task<IJSObjectReference> ImportAsync()
        => _import ??= js.InvokeAsync<IJSObjectReference>("import", path).AsTask();

    // JSDisconnectedException exposes only a message-only constructor, so the original
    // cannot be chained as an InnerException — the module path is folded into the message instead.
    [DoesNotReturn]
    private void ThrowEnriched(JSDisconnectedException ex)
        => throw new JSDisconnectedException($"{ex.Message} (module: {path})");
}

