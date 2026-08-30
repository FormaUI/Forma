namespace Forma.UI.Blazor.Interop.Operations;

using Microsoft.JSInterop;

/// <summary>What the reader carries between applications.</summary>
internal sealed class Clipboard(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.UI/scripts/operations/Clipboard.js");

    /// <summary>Places text on the clipboard.</summary>
    public ValueTask<bool> WriteAsync(string text)
        => _module.TryInvokeVoidAsync("write", text);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
