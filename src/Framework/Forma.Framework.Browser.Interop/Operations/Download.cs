namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>Hands the reader a file to save, through the host's native operations where they take it.</summary>
/// <param name="js">The component's JS environment.</param>
/// <param name="nativeOperations">The host's native operations.</param>
public sealed class Download(IJSEnvironment js, INativeOperations? nativeOperations = null) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/Download.js");

    /// <summary>Saves text as a file of the given name and media type.</summary>
    public async ValueTask<bool> SaveAsync(string name, string content, string type)
        => (nativeOperations is { CanSave: true } && await nativeOperations.TrySaveAsync(name, content, type))
            || await _module.TryInvokeVoidAsync("save", name, content, type);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
