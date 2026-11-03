namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

/// <summary>What the reader carries between applications.</summary>
public sealed class Clipboard(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/Clipboard.js");

    /// <summary>Places text on the clipboard.</summary>
    public ValueTask<bool> WriteAsync(string text)
        => _module.TryInvokeVoidAsync("write", text);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
