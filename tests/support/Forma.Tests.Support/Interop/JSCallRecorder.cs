namespace Forma.Tests.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>The calls a recording JS fake took, each answered through one function, or held open until the test answers it or the call's token cancels it where its identifier is held.</summary>
/// <param name="respond">The function that answers a call from its identifier and arguments.</param>
/// <remarks>A caller resumes where its context posts it, and a test's context posts to several threads at once, so two calls can arrive together; the records are kept under a lock, and each read is a snapshot of them.</remarks>
public sealed class JSCallRecorder(Func<string, object?[], object?> respond)
{
    private readonly Lock _lock = new();
    private readonly List<RecordedCall> _calls = [];
    private readonly HashSet<string> _heldIdentifiers = [];
    private readonly List<CallWaiter> _waiters = [];

    /// <summary>Every call taken, in order.</summary>
    public IReadOnlyList<RecordedCall> Calls
    {
        get
        {
            lock (_lock)
            {
                return [.. _calls];
            }
        }
    }

    /// <summary>The calls taken to <paramref name="identifier"/>, in order.</summary>
    public IReadOnlyList<RecordedCall> GetCalls(string identifier)
    {
        lock (_lock)
        {
            return [.. _calls.Where(call => call.Identifier == identifier)];
        }
    }

    /// <summary>The first call to <paramref name="identifier"/> that <paramref name="predicate"/> matches, once it is taken.</summary>
    public Task<RecordedCall> WaitForCallAsync(string identifier, Func<RecordedCall, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        lock (_lock)
        {
            if (_calls.FirstOrDefault(call => call.Identifier == identifier && predicate(call)) is { } taken)
            {
                return Task.FromResult(taken);
            }

            var waiter = new CallWaiter(identifier, predicate);
            _waiters.Add(waiter);

            return waiter.Call.Task;
        }
    }

    /// <summary>Holds every later call to <paramref name="identifier"/> open until the test answers it or the call's token cancels it.</summary>
    public void Hold(string identifier)
    {
        lock (_lock)
        {
            _heldIdentifiers.Add(identifier);
        }
    }

    /// <summary>Answers every later call to <paramref name="identifier"/> through the function again.</summary>
    public void Release(string identifier)
    {
        lock (_lock)
        {
            _heldIdentifiers.Remove(identifier);
        }
    }

    /// <summary>Takes a call, answering it through the function or holding it until it is answered or <paramref name="cancellationToken"/> cancels it.</summary>
    [SuppressMessage("Design", "CA1031", Justification = "The call faults with whatever the function throws, as a runtime call does.")]
    public ValueTask<TValue> TakeAsync<TValue>(string identifier, object?[]? args, CancellationToken cancellationToken = default)
    {
        var call = new RecordedCall(identifier, args ?? []);
        List<CallWaiter> matched;

        lock (_lock)
        {
            call.IsHeld = _heldIdentifiers.Contains(identifier);
            _calls.Add(call);
            matched = [.. _waiters.Where(waiter => waiter.Identifier == identifier && waiter.Predicate(call))];
            _waiters.RemoveAll(matched.Contains);
        }

        foreach (var waiter in matched)
        {
            waiter.Call.SetResult(call);
        }

        if (call.IsHeld)
        {
            return new ValueTask<TValue>(AwaitAnswerAsync<TValue>(call, cancellationToken));
        }

        try
        {
            return ValueTask.FromResult((TValue)respond(identifier, args ?? [])!);
        }
        catch (Exception ex)
        {
            return ValueTask.FromException<TValue>(ex);
        }
    }

    // Continued synchronously, so the caller's continuation runs where the test answers the call, a renderer's context
    // included, where an await leaving the context would queue it to the thread pool instead; a call its token cancels ends
    // canceled, as a runtime's call does.
    private static Task<TValue> AwaitAnswerAsync<TValue>(RecordedCall call, CancellationToken cancellationToken)
        => call.Answer.Task.WaitAsync(cancellationToken).ContinueWith(answer => (TValue)answer.GetAwaiter().GetResult()!, cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    // A test waiting for a call, resumed apart from the caller, so the test never runs inside the code that made the call.
    private sealed record CallWaiter(string Identifier, Func<RecordedCall, bool> Predicate)
    {
        public TaskCompletionSource<RecordedCall> Call { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
