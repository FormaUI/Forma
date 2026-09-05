namespace Forma.Framework.Common.Interop.Operations;

using Forma.Framework.Interop;

using Microsoft.JSInterop;

/// <summary>Activates a third-party embed script and rescans the page for markup it should process.</summary>
public sealed class Embedding(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.Framework.Common.Interop/scripts/operations/Embedding.js");

    /// <summary>Loads the embed script if needed and asks it to process markup rendered since it last ran.</summary>
    public ValueTask<bool> ActivateAsync(string scriptUrl)
        => _module.TryInvokeAsync<bool>("activate", scriptUrl);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
