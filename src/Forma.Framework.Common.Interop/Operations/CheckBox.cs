namespace Forma.Framework.Common.Interop.Operations;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>State a checkbox exposes only as a property.</summary>
public sealed class CheckBox(IJSEnvironment js) : IAsyncDisposable
{
    private readonly JSModule _module = new(js, "./_content/forma/common/scripts/operations/CheckBox.js");

    /// <summary>Sets whether the checkbox presents as neither checked nor unchecked.</summary>
    public ValueTask<bool> SetIndeterminateAsync(ElementReference checkbox, bool isIndeterminate)
        => _module.TryInvokeVoidAsync("setIndeterminate", checkbox, isIndeterminate);

    /// <inheritdoc />
    public ValueTask DisposeAsync()
        => _module.DisposeAsync();
}
