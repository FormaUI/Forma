namespace Forma.Tests.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A JS object reference that holds one call open until the test answers it and answers every other call through a function.</summary>
/// <param name="heldIdentifier">The identifier of the call held open.</param>
/// <param name="respond">The function that answers every other call from its identifier and arguments.</param>
public sealed class HeldJSObjectReference(string heldIdentifier, Func<string, object?[]?, object?> respond) : IJSObjectReference
{
    // Plain, so the wrapper's continuation runs inline where Answer completes it.
    private readonly TaskCompletionSource<object?> _held = new();

    /// <summary>Whether the held call has been made and not yet answered.</summary>
    public bool IsHolding { get; private set; }

    /// <summary>Whether <see cref="DisposeAsync"/> has run.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Answers the held call with <paramref name="result"/>.</summary>
    public void Answer(object? result)
        => _held.SetResult(result);

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031", Justification = "The call faults with whatever the function throws, as a runtime call does.")]
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
        if (identifier == heldIdentifier)
        {
            IsHolding = true;

            return new ValueTask<TValue>(AwaitHeldAsync<TValue>());
        }

        try
        {
            return ValueTask.FromResult((TValue)respond(identifier, args)!);
        }
        catch (Exception ex)
        {
            return ValueTask.FromException<TValue>(ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => InvokeAsync<TValue>(identifier, args);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        IsDisposed = true;

        return ValueTask.CompletedTask;
    }

    private async Task<TValue> AwaitHeldAsync<TValue>()
    {
        var result = await _held.Task.ConfigureAwait(false);
        IsHolding = false;

        return (TValue)result!;
    }
}
