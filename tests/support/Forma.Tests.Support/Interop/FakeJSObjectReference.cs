namespace Forma.Tests.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A JS object reference that answers every call through one function and faults the call with whatever the function throws.</summary>
/// <param name="respond">The function that answers a call from its identifier and arguments.</param>
/// <param name="releaseException">The exception the first release faults with, where <see langword="null"/> is a clean release.</param>
public sealed class FakeJSObjectReference(Func<string, object?[]?, object?> respond, Exception? releaseException = null) : IJSObjectReference
{
    private readonly TaskCompletionSource _disposalStartedSource = new();

    /// <summary>Whether <see cref="DisposeAsync"/> has run.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>Completes as <see cref="DisposeAsync"/> first runs.</summary>
    public Task DisposalStarted
        => _disposalStartedSource.Task;

    /// <inheritdoc />
    [SuppressMessage("Design", "CA1031", Justification = "The call faults with whatever the function throws, as a runtime call does.")]
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
    {
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
        var isFirstRelease = !IsDisposed;
        IsDisposed = true;
        _disposalStartedSource.TrySetResult();

        return isFirstRelease && releaseException is not null ? ValueTask.FromException(releaseException) : ValueTask.CompletedTask;
    }
}
