namespace Forma.Framework.Common.Interop.Operations;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The value and selected range of a text entry.</summary>
public sealed class TextEntry(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/forma/common/scripts/operations/Text.js");

    /// <summary>Reads what is currently typed and empties the entry.</summary>
    public ValueTask<string?> ReadAndClearAsync(ElementReference input)
        => _module.TryInvokeAsync<string>("take", input);

    /// <summary>Reads the entry's current selected range, or null when the call could not complete.</summary>
    public ValueTask<TextRange?> ReadSelectionAsync(ElementReference input)
        => _module.TryInvokeAsync<TextRange>("readSelection", input);

    /// <summary>Sets the entry's selection to the given range.</summary>
    public ValueTask<bool> TryWriteSelectionAsync(ElementReference input, int start, int end)
        => _module.TryInvokeVoidAsync("writeSelection", input, start, end);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();

    /// <summary>A selected range within a text entry.</summary>
    [SuppressMessage("Performance", "CA1812", Justification = "Constructed by JSON deserialization when JS interop returns this shape.")]
    public sealed record TextRange(int Start, int End);
}
