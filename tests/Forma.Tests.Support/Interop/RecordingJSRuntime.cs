namespace Forma.Tests.Interop;

using Microsoft.JSInterop;

/// <summary>A JS runtime that records every call, answering each through one function or holding it open where the test holds its identifier.</summary>
/// <param name="respond">The function that answers a call from its identifier and arguments.</param>
public sealed class RecordingJSRuntime(Func<string, object?[], object?> respond) : IJSRuntime
{
    /// <summary>The calls the runtime took.</summary>
    public JSCallRecorder Recorder { get; } = new(respond);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => Recorder.TakeAsync<TValue>(identifier, args);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => Recorder.TakeAsync<TValue>(identifier, args);
}
