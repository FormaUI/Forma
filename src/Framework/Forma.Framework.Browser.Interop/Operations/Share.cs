namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>Hands a subject to the platform's own share surface, the host's native one where it shares.</summary>
/// <param name="js">The component's JS environment.</param>
/// <param name="nativeOperations">The host's native operations.</param>
public sealed class Share(IJSEnvironment js, INativeOperations? nativeOperations = null) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/Share.js");

    /// <summary>Whether the host or the browser offers a native share surface.</summary>
    public async ValueTask<bool> IsAvailableAsync()
        => nativeOperations is { CanShare: true } || await _module.TryInvokeAsync<bool>("isAvailable");

    /// <summary>Offers the subject to the platform's share surface.</summary>
    public async ValueTask<bool> ShareAsync(string title, string url)
        => (nativeOperations is { CanShare: true } && await nativeOperations.TryShareAsync(title, url))
            || await _module.TryInvokeVoidAsync("share", title, url);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
