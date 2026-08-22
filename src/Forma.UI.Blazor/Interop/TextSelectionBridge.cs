namespace Forma.UI.Blazor.Interop;

using System.Diagnostics.CodeAnalysis;

using Forma.UI.Features.Composer.Editing;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Reads and writes the native selection of a textarea or input, shared by every editing surface built on one.</summary>
internal sealed class TextSelectionBridge(IJSRuntime js) : IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.UI/scripts/TextSelection.js";

    private readonly JsModule _module = new(js, ModulePath);

    /// <summary>Reads the element's current selection, or null when the call could not complete.</summary>
    public async ValueTask<ComposerSelection?> ReadAsync(ElementReference element)
    {
        var offsets = await _module.TryInvokeAsync<Offsets>("readSelection", element);

        return offsets is null ? null : new ComposerSelection { Start = offsets.Start, End = offsets.End };
    }

    /// <summary>Sets the element's selection to the given range.</summary>
    public async ValueTask WriteAsync(ElementReference element, ComposerSelection selection)
        => await _module.TryInvokeVoidAsync("writeSelection", element, selection.Start, selection.End);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _module.DisposeAsync();

    [SuppressMessage("Performance", "CA1812", Justification = "Constructed by JSON deserialization when JS interop returns this shape.")]
    private sealed record Offsets(int Start, int End);
}
