namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>The order elements stand in within the document.</summary>
public sealed class DocumentOrder(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/DocumentOrder.js");

    /// <summary>The places of <paramref name="elements"/> in the order they stand in the document, those no longer in it last.</summary>
    /// <param name="elements">The elements.</param>
    /// <returns>The places, or <see langword="null"/> where the browser can't be asked.</returns>
    public async ValueTask<int[]?> SortAsync(IReadOnlyList<ElementReference> elements)
        => await _module.TryInvokeJsonAsync("sort", js.TransferOptions.MaximumLength, elements) is { } places
            ? [.. places.EnumerateArray().Select(place => place.GetInt32())]
            : null;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
