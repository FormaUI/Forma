namespace Forma.Tests.Interop;

using Microsoft.JSInterop;

/// <summary>A JS runtime that records every call, answering each through one function or holding it open where the test holds its identifier.</summary>
/// <param name="respond">The function that answers a call from its identifier and arguments.</param>
public sealed class RecordingJSRuntime(Func<string, object?[], object?> respond) : IJSRuntime
{
    /// <summary>The calls the runtime took.</summary>
    public JSCallRecorder Recorder { get; } = new(respond);

    /// <summary>Creates a runtime whose import of the module at <paramref name="path"/> yields <paramref name="module"/>, and of every other module <paramref name="otherModule"/>.</summary>
    public static RecordingJSRuntime Importing(string path, RecordingJSObjectReference module, RecordingJSObjectReference otherModule)
        => new((identifier, args) => identifier == "import" ? (args[0] is string imported && imported == path ? module : otherModule) : null);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => Recorder.TakeAsync<TValue>(identifier, args);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => Recorder.TakeAsync<TValue>(identifier, args, cancellationToken);
}
