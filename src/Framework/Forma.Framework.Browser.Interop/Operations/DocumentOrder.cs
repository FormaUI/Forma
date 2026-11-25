namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>The order elements stand in within the document.</summary>
public sealed class DocumentOrder(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/DocumentOrder.js");

    /// <summary>The places of <paramref name="elements"/> in the order they stand in the document, those no longer in it last.</summary>
    /// <param name="elements">The elements.</param>
    /// <returns>The places, or <see langword="null"/> where the browser can't be asked or answers an order that doesn't place each element once.</returns>
    public async ValueTask<int[]?> SortAsync(IReadOnlyList<ElementReference> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        if (await _module.TryInvokeJsonAsync("sort", DomJsonContext.Default.Int32Array, js.TransferOptions.MaximumLength, elements) is not { } places)
        {
            return null;
        }

        if (!IsOrderOf(places, elements.Count))
        {
            DomLog.OrderRefused(js.Logger, js.RuntimeId);

            return null;
        }

        return places;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();

    // An order of count elements names each of their places once.
    private static bool IsOrderOf(int[] places, int count)
    {
        if (places.Length != count)
        {
            return false;
        }

        var isPlaced = new bool[count];

        foreach (var place in places)
        {
            if (place < 0 || place >= count || isPlaced[place])
            {
                return false;
            }

            isPlaced[place] = true;
        }

        return true;
    }
}
