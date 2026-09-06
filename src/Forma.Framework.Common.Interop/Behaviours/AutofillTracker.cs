namespace Forma.Framework.Common.Interop.Behaviours;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Marks a field the browser filled in without the user typing.</summary>
public sealed class AutofillTracker(IJSRuntime js)
    : JsInterop(js, "./_content/forma/common/scripts/behaviours/AutofillTracker.js")
{
    /// <summary>Attaches tracking to the field.</summary>
    public ValueTask<bool> AttachAsync(ElementReference input)
        => AttachCoreAsync(input);
}
