namespace Forma.Framework.Browser.Interop.Operations;

using Forma.Framework.JavaScript.Interop;

using Microsoft.AspNetCore.Components;

/// <summary>The labels elements are read by.</summary>
public sealed class ElementLabels(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/browser/scripts/operations/ElementLabels.js");

    /// <summary>The label each of <paramref name="elements"/> is read by, in the order given: its own <c>aria-label</c>, or its text without what is hidden from assistive technology, and empty for one no longer in the document.</summary>
    /// <param name="elements">The elements.</param>
    /// <returns>The labels, or <see langword="null"/> where the browser can't be asked.</returns>
    public async ValueTask<string[]?> ReadAsync(IReadOnlyList<ElementReference> elements)
        => await _module.TryInvokeJsonAsync("read", js.TransferOptions.MaximumLength, elements) is { } labels
            ? [.. labels.EnumerateArray().Select(label => label.GetString() ?? string.Empty)]
            : null;

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
