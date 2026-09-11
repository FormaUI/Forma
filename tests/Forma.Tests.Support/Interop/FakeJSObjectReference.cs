namespace Forma.Tests.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A JS object reference that answers every call through one function, faulting the call with whatever the function throws, for tests about how a wrapper treats what a call returns or throws.</summary>
/// <param name="respond">Answers a call from its identifier and arguments, or throws in its place.</param>
/// <param name="releaseException">The exception the first release faults with, or <see langword="null"/> to release cleanly.</param>
public sealed class FakeJSObjectReference(Func<string, object?[]?, object?> respond, Exception? releaseException = null) : IJSObjectReference
{
    /// <summary>Whether <see cref="DisposeAsync"/> has run.</summary>
    public bool IsDisposed { get; private set; }

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

        return isFirstRelease && releaseException is not null ? ValueTask.FromException(releaseException) : ValueTask.CompletedTask;
    }
}
