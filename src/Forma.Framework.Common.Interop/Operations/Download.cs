namespace Forma.Framework.Common.Interop.Operations;

using Forma.Framework.Interop;

using Microsoft.JSInterop;

/// <summary>Hands the reader a file to save.</summary>
public sealed class Download(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.Framework.Common.Interop/scripts/operations/Download.js");

    /// <summary>Saves text as a file of the given name and media type.</summary>
    public ValueTask<bool> SaveAsync(string name, string content, string type)
        => _module.TryInvokeVoidAsync("save", name, content, type);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
