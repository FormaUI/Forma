namespace Forma.Framework.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>State a checkbox exposes only as a property.</summary>
public sealed class CheckBox(IJSRuntime js) : IAsyncDisposable
{
    private readonly JsModule _module = new(js, "./_content/Forma.Framework.Interop/scripts/operations/CheckBox.js");

    /// <summary>Sets whether the checkbox presents as neither checked nor unchecked.</summary>
    public ValueTask<bool> SetIndeterminateAsync(ElementReference checkbox, bool isIndeterminate)
        => _module.TryInvokeVoidAsync("setIndeterminate", checkbox, isIndeterminate);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
