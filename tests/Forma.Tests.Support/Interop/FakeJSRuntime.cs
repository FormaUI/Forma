namespace Forma.Tests.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A JS runtime that answers every call through one function, faulting the call with whatever the function throws, for tests about how a wrapper treats what a call returns or throws.</summary>
/// <param name="respond">Answers a call from its identifier and arguments, or throws in its place.</param>
public sealed class FakeJSRuntime(Func<string, object?[]?, object?> respond) : IJSRuntime
{
    /// <summary>Creates a runtime whose module import yields <paramref name="moduleReference"/>.</summary>
    public static FakeJSRuntime Importing(FakeJSObjectReference moduleReference)
        => new((identifier, _) => identifier == "import" ? moduleReference : throw new JSException($"'{identifier}' is not an import."));

    /// <summary>Creates a runtime whose every call, the module import included, throws <paramref name="exception"/>.</summary>
    public static FakeJSRuntime Throwing(Exception exception)
        => new((_, _) => throw exception);

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
}
