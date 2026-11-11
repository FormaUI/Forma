namespace Forma.Tests.Interop;

using Microsoft.JSInterop;

/// <summary>A JS object reference that records every call, answering each through one function or holding it open where the test holds its identifier, until the test answers it or the call's token cancels it.</summary>
/// <param name="respond">The function that answers a call from its identifier and arguments.</param>
public sealed class RecordingJSObjectReference(Func<string, object?[], object?> respond) : IJSObjectReference
{
    /// <summary>The calls the reference took.</summary>
    public JSCallRecorder Recorder { get; } = new(respond);

    private readonly TaskCompletionSource _disposalStartedSource = new();
    private int _disposalCount;

    /// <summary>How many times the reference has been released.</summary>
    public int DisposalCount
        => Volatile.Read(ref _disposalCount);

    /// <summary>Completes as the reference is first released.</summary>
    public Task DisposalStarted
        => _disposalStartedSource.Task;

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => Recorder.TakeAsync<TValue>(identifier, args);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => Recorder.TakeAsync<TValue>(identifier, args, cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposalCount);
        _disposalStartedSource.TrySetResult();

        return ValueTask.CompletedTask;
    }
}
