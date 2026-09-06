namespace Forma.Framework.Common.Interop.Behaviours;

using Forma.Framework.Core.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Keeps the arrow, Home and End keys from scrolling the page under a list that owns them.</summary>
public sealed class NavigationGuard(IJSRuntime js)
    : JsInterop(js, "./_content/forma/common/scripts/behaviours/NavigationGuard.js")
{
    /// <summary>Attaches to the list.</summary>
    public ValueTask<bool> AttachAsync(ElementReference target)
        => AttachCoreAsync(target);
}
