namespace Forma.Framework.Browser.Interop.Operations;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>The value and selected range of a text entry.</summary>
public sealed class TextEntry(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/TextEntry.js");

    /// <summary>Reads what is currently typed and empties the entry.</summary>
    public ValueTask<string?> ReadAndClearAsync(ElementReference input)
        => _module.TryInvokeAsync<string>("take", input);

    /// <summary>Reads the entry's current selected range.</summary>
    public ValueTask<TextRange?> ReadSelectionAsync(ElementReference input)
        => _module.TryInvokeAsync<TextRange>("readSelection", input);

    /// <summary>Sets the entry's selection to the given range.</summary>
    public ValueTask<bool> TryWriteSelectionAsync(ElementReference input, int start, int end)
        => _module.TryInvokeVoidAsync("writeSelection", input, start, end);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();

    /// <summary>A selected range within a text entry.</summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    [SuppressMessage("Performance", "CA1812", Justification = "Constructed by JSON deserialization when JS interop returns this shape.")]
    public sealed record TextRange
    {
        public int Start { get; init; }

        public int End { get; init; }
    }
}
