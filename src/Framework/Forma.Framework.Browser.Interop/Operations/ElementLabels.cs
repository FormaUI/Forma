namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>The labels elements are read by.</summary>
public sealed class ElementLabels(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/ElementLabels.js");

    /// <summary>The label each of <paramref name="elements"/> is read by, in the order given: its own <c>aria-label</c>, or its text without what is hidden from assistive technology, and empty for one no longer in the document.</summary>
    /// <param name="elements">The elements.</param>
    /// <returns>The labels, or <see langword="null"/> where the browser can't be asked or answers labels that don't label each element.</returns>
    public async ValueTask<string[]?> ReadAsync(IReadOnlyList<ElementReference> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        if (await _module.TryInvokeJsonAsync("read", DomJsonContext.Default.StringArray, js.TransferOptions.MaximumLength, elements) is not { } labels)
        {
            return null;
        }

        if (labels.Length != elements.Count)
        {
            DomLog.LabelsRefused(js.Logger, js.RuntimeId);

            return null;
        }

        return [.. labels.Select(label => label ?? string.Empty)];
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
