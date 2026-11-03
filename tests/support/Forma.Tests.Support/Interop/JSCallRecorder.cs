namespace Forma.Tests.Interop;

using System.Diagnostics.CodeAnalysis;

/// <summary>The calls a recording JS fake took, each answered through one function, or held open until the test answers it or the call's token cancels it where its identifier is held.</summary>
/// <param name="respond">The function that answers a call from its identifier and arguments.</param>
public sealed class JSCallRecorder(Func<string, object?[], object?> respond)
{
    private readonly List<RecordedCall> _calls = [];
    private readonly HashSet<string> _heldIdentifiers = [];

    /// <summary>Every call taken, in order.</summary>
    public IReadOnlyList<RecordedCall> Calls
        => _calls;

    /// <summary>The calls taken to <paramref name="identifier"/>, in order.</summary>
    public IReadOnlyList<RecordedCall> GetCalls(string identifier)
        => [.. _calls.Where(call => call.Identifier == identifier)];

    /// <summary>Holds every later call to <paramref name="identifier"/> open until the test answers it or the call's token cancels it.</summary>
    public void Hold(string identifier)
        => _heldIdentifiers.Add(identifier);

    /// <summary>Answers every later call to <paramref name="identifier"/> through the function again.</summary>
    public void Release(string identifier)
        => _heldIdentifiers.Remove(identifier);

    /// <summary>Takes a call, answering it through the function or holding it until it is answered or <paramref name="cancellationToken"/> cancels it.</summary>
    [SuppressMessage("Design", "CA1031", Justification = "The call faults with whatever the function throws, as a runtime call does.")]
    public ValueTask<TValue> TakeAsync<TValue>(string identifier, object?[]? args, CancellationToken cancellationToken = default)
    {
        var call = new RecordedCall(identifier, args ?? []);
        _calls.Add(call);

        if (_heldIdentifiers.Contains(identifier))
        {
            call.IsHeld = true;

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
    // included, where an await leaving the context would queue it to the thread pool instead.
    private static Task<TValue> AwaitAnswerAsync<TValue>(RecordedCall call, CancellationToken cancellationToken)
        => call.Answer.Task.WaitAsync(cancellationToken).ContinueWith(answer => (TValue)answer.GetAwaiter().GetResult()!, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
}
