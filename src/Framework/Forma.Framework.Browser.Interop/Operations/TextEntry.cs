namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The selected range of a text entry.</summary>
public sealed class TextEntry(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/TextEntry.js");

    /// <summary>Reads the entry's current selected range.</summary>
    public ValueTask<TextRange?> ReadSelectionAsync(ElementReference input)
        => _module.TryInvokeAsync<TextRange>("readSelection", input);

    /// <summary>Sets the entry's selection to the given range.</summary>
    public ValueTask<bool> TryWriteSelectionAsync(ElementReference input, int start, int end)
        => _module.TryInvokeVoidAsync("writeSelection", input, start, end);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
