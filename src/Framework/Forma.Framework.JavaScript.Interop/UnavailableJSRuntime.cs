namespace Forma.Framework.JavaScript.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.JSInterop;

/// <summary>A JS runtime whose every call throws <see cref="JSRuntimeUnavailableException"/>.</summary>
public sealed class UnavailableJSRuntime : IJSRuntime
{
    private UnavailableJSRuntime()
    {
    }

    /// <summary>The single instance.</summary>
    public static UnavailableJSRuntime Instance { get; } = new();

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, object?[]? args)
        => throw new JSRuntimeUnavailableException(FormatMessage(identifier));

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => throw new JSRuntimeUnavailableException(FormatMessage(identifier));

    private static string FormatMessage(string identifier)
        => $"'{identifier}' cannot be invoked: no JavaScript runtime is available to this component.";
}
