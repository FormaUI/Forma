namespace Forma.Tests.Interop;

/// <summary>A call a recording JS fake took: its identifier, its arguments, and, while held, the answer it waits on.</summary>
public sealed class RecordedCall
{
    internal RecordedCall(string identifier, IReadOnlyList<object?> arguments)
    {
        Identifier = identifier;
        Arguments = arguments;
    }

    /// <summary>The identifier called.</summary>
    public string Identifier { get; }

    /// <summary>The arguments of the call.</summary>
    public IReadOnlyList<object?> Arguments { get; }

    /// <summary>Whether the call was held open for the test to answer.</summary>
    public bool IsHeld { get; internal set; }

    /// <summary>Whether a held call has been answered.</summary>
    public bool IsAnswered
        => Answer.Task.IsCompleted;

    // Plain, so the caller's continuation runs inline where the test answers it.
    internal TaskCompletionSource<object?> Answer { get; } = new();

    /// <summary>Answers a held call with <paramref name="result"/>.</summary>
    public void Complete(object? result)
        => Answer.SetResult(result);

    /// <summary>Faults a held call with <paramref name="exception"/>.</summary>
    public void Fail(Exception exception)
        => Answer.SetException(exception);
}
