namespace Forma.Framework.JavaScript.Interop;

using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

/// <summary>A JS environment over a JS runtime.</summary>
public sealed class JSEnvironment : IJSEnvironment
{
    private readonly IJSRuntime _runtime;

    /// <summary>Creates an environment over <paramref name="runtime"/> for the owner with the given logger, runtime identity and transfer options.</summary>
    public JSEnvironment(IJSRuntime runtime, ILogger logger, Guid runtimeId, JSTransferOptions? transferOptions = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(logger);

        _runtime = runtime;
        Logger = logger;
        RuntimeId = runtimeId;
        TransferOptions = transferOptions ?? JSTransferOptions.Default;
    }

    /// <inheritdoc />
    public Guid RuntimeId { get; }

    /// <inheritdoc />
    public ILogger Logger { get; }

    /// <inheritdoc />
    public JSTransferOptions TransferOptions { get; }

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, object?[]? args)
        => _runtime.InvokeAsync<TValue>(identifier, args);

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(LinkerFlags.JsonSerialized)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        => _runtime.InvokeAsync<TValue>(identifier, cancellationToken, args);
}
