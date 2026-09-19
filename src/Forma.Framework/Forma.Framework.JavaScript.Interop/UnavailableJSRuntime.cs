namespace Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>The runtime a component gets where none is registered: every call throws.</summary>
public sealed class UnavailableJSRuntime : IJSRuntime
{
    private UnavailableJSRuntime()
    {
    }

    /// <summary>The one instance, since the runtime holds nothing.</summary>
    public static UnavailableJSRuntime Instance { get; } = new();

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => throw new JSRuntimeUnavailableException(FormatMessage(identifier));

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => throw new JSRuntimeUnavailableException(FormatMessage(identifier));

    private static string FormatMessage(string identifier)
        => $"'{identifier}' cannot be invoked: no JavaScript runtime is available to this component.";
}
