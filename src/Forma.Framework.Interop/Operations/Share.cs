namespace Forma.Framework.Interop.Operations;

using Microsoft.JSInterop;

/// <summary>Hands a subject to the platform's own share surface.</summary>
public sealed class Share(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.Framework.Interop/scripts/operations/Share.js");

    /// <summary>Whether the browser offers a native share surface.</summary>
    public async ValueTask<bool> IsAvailableAsync()
        => await _module.TryInvokeAsync<bool>("isAvailable");

    /// <summary>Offers the subject to the platform's share surface.</summary>
    public ValueTask<bool> ShareAsync(string title, string url)
        => _module.TryInvokeVoidAsync("share", title, url);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
